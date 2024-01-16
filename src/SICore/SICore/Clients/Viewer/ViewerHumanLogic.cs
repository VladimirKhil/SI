using Notions;
using SICore.BusinessLogic;
using SICore.Clients.Viewer;
using SIData;
using SIPackages.Core;
using SIUI.ViewModel;
using SIUI.ViewModel.Core;
using System.Diagnostics;
using System.Text;
using System.Xml;
using Utils;
using R = SICore.Properties.Resources;

namespace SICore;

/// <summary>
/// Defines a human viewer logic.
/// </summary>
public class ViewerHumanLogic : Logic<ViewerData>, IViewerLogic
{
    private record struct ContentInfo(string Type, string Uri);

    /// <summary>
    /// Maximum length of text that could be automatically added to game table.
    /// </summary>
    private const int MaxAdditionalTableTextLength = 150;

    /// <summary>
    /// Minimum weight for the small content.
    /// </summary>
    private const double SmallContentWeight = 1.0;

    /// <summary>
    /// Maximum weight for the large content.
    /// </summary>
    private const double LargeContentWeight = 5.0;

    /// <summary>
    /// Length of text having weight of 1.
    /// </summary>
    private const int TextLengthWithBasicWeight = 80;

    private static readonly TimeSpan HintLifetime = TimeSpan.FromSeconds(6);

    private bool _disposed = false;

    private readonly CancellationTokenSource _cancellation = new();

    private readonly ILocalFileManager _localFileManager = new LocalFileManager();
    private readonly Task _localFileManagerTask;

    protected readonly ViewerActions _viewerActions;

    protected readonly ILocalizer _localizer;

    public TableInfoViewModel TInfo { get; }

    public bool CanSwitchType => true;

    public IPlayerLogic PlayerLogic { get; }

    public IShowmanLogic ShowmanLogic { get; }

    private string? _prependTableText;
    private string? _appendTableText;

    public ViewerHumanLogic(ViewerData data, ViewerActions viewerActions, ILocalizer localizer)
        : base(data)
    {
        _viewerActions = viewerActions;
        _localizer = localizer;

        TInfo = new TableInfoViewModel(_data.TInfo, _data.Host.GetSettings()) { AnimateText = true, Enabled = true };

        TInfo.PropertyChanged += TInfo_PropertyChanged;
        TInfo.MediaLoad += TInfo_MediaLoad;
        TInfo.MediaLoadError += TInfo_MediaLoadError;

        TInfo.QuestionSelected += TInfo_QuestionSelected;
        TInfo.ThemeSelected += TInfo_ThemeSelected;
        TInfo.AnswerSelected += TInfo_AnswerSelected;

        PlayerLogic = new PlayerHumanLogic(data, TInfo, viewerActions);
        ShowmanLogic = new ShowmanHumanLogic(data, TInfo, viewerActions);

        _localFileManager.Error += LocalFileManager_Error;
        _localFileManagerTask = _localFileManager.StartAsync(_cancellation.Token);
    }

    private void TInfo_QuestionSelected(QuestionInfoViewModel question)
    {
        var found = false;

        for (var i = 0; i < TInfo.RoundInfo.Count; i++)
        {
            for (var j = 0; j < TInfo.RoundInfo[i].Questions.Count; j++)
            {
                if (TInfo.RoundInfo[i].Questions[j] == question)
                {
                    found = true;
                    _viewerActions.SendMessageWithArgs(Messages.Choice, i, j);
                    break;
                }
            }

            if (found)
            {
                break;
            }
        }

        ClearSelections(true);
    }

    private void TInfo_ThemeSelected(ThemeInfoViewModel theme)
    {
        for (int i = 0; i < TInfo.RoundInfo.Count; i++)
        {
            if (TInfo.RoundInfo[i] == theme)
            {
                _viewerActions.SendMessageWithArgs(Messages.Delete, i);
                break;
            }
        }

        ClearSelections(true);
    }

    public void ClearSelections(bool full = false)
    {
        if (full)
        {
            TInfo.Selectable = false;
            TInfo.SelectQuestion.CanBeExecuted = false;
            TInfo.SelectTheme.CanBeExecuted = false;
            TInfo.SelectAnswer.CanBeExecuted = false;
        }

        _data.Hint = "";
        _data.DialogMode = DialogModes.None;

        for (int i = 0; i < _data.Players.Count; i++)
        {
            _data.Players[i].CanBeSelected = false;
        }

        _data.Host.OnFlash(false);
    }

    private void TInfo_AnswerSelected(ItemViewModel item)
    {
        _data.PersonDataExtensions.SendAnswer.Execute(item.Label);
        ClearSelections(true);
    }

    private void LocalFileManager_Error(Uri mediaUri, Exception e) =>
        _data.OnAddString(
            null,
            $"\n{string.Format(R.FileLoadError, Path.GetFileName(mediaUri.ToString()))}: {e.Message}\n",
            LogMode.Log);

    private void TInfo_MediaLoad() => _viewerActions.SendMessage(Messages.MediaLoaded);

    private void TInfo_MediaLoadError(MediaLoadException exc)
    {
        string error;

        if (exc.InnerException is NotSupportedException)
        {
            error = $"{_localizer[nameof(R.MediaFileNotSupported)]}: {exc.InnerException.Message}";
        }
        else
        {
            error = (exc.InnerException ?? exc).ToString();
        }

        _data.OnAddString(null, $"{_localizer[nameof(R.MediaLoadError)]} {exc.MediaUri}: {error}{Environment.NewLine}", LogMode.Log);
    }

    private void TInfo_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TableInfoViewModel.TStage))
        {
            if (TInfo.TStage == TableStage.RoundTable)
            {
                _data.Host.PlaySound();
            }
        }
    }

    public virtual void ReceiveText(Message m)
    {
        _data.AddToChat(m);

        if (_data.Host.MakeLogs)
        {
            AddToFileLog(m);
        }
    }

    private static readonly XmlReaderSettings ReaderSettings = new() { ConformanceLevel = ConformanceLevel.Fragment };

    /// <summary>
    /// Вывод текста в протокол и в форму
    /// </summary>
    /// <param name="text">Выводимый текст</param>
    [Obsolete("Use OnReplic instead")]
    virtual public void Print(string text)
    {
        var chatMessageBuilder = new StringBuilder();
        var logMessageBuilder = new StringBuilder();

        var isPrintable = false;
        var special = false;

        try
        {
            using var reader = new StringReader(text);
            using var xmlReader = XmlReader.Create(reader, ReaderSettings);

            xmlReader.Read();
            while (!xmlReader.EOF)
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    ParseMessageToPrint(xmlReader, chatMessageBuilder, logMessageBuilder, ref isPrintable, ref special);
                }
                else
                {
                    xmlReader.Read();
                }
            }
        }
        catch (XmlException exc)
        {
            throw new Exception($"{_localizer[nameof(R.StringParseError)]} {text}.", exc);
        }

        var toFormStr = chatMessageBuilder.ToString();
        if (isPrintable)
        {
            var pair = toFormStr.Split(':');
            var speech = (pair.Length > 1 && pair[0].Length + 2 < toFormStr.Length) ? toFormStr[(pair[0].Length + 2)..] : toFormStr;

            if (_data.Speaker != null)
            {
                _data.Speaker.Replic = "";
            }

            _data.Speaker = _data.MainPersons.FirstOrDefault(item => item.Name == pair[0]);
            if (_data.Speaker != null)
            {
                _data.Speaker.Replic = speech.Trim();
            }
        }

        if (_data.Host.TranslateGameToChat || special)
        {
            _data.OnAddString(null, toFormStr.Trim(), LogMode.Protocol);
        }

        if (_data.Host.MakeLogs)
        {
            AddToFileLog(logMessageBuilder.ToString());
        }
    }

    /// <summary>
    /// Вывод сообщения в лог файл и в чат игры
    /// </summary>
    /// <param name="replicCode">ReplicCodes код сообщения или игрока</param>
    /// <param name="text">сообщение</param>
    public void OnReplic(string replicCode, string text)
    {
        string? logString = null;

        if (replicCode == ReplicCodes.Showman.ToString())
        {
            if (_data.ShowMan == null)
            {
                return;
            }

            // reset old speaker's replic
            if (_data.Speaker != null)
            {
                _data.Speaker.Replic = "";
            }

            // add new replic to the current speaker
            _data.Speaker = _data.ShowMan;
            _data.Speaker.Replic = TrimReplic(text);

            logString = $"<span class=\"sh\">{_data.Speaker.Name}: </span><span class=\"r\">{text}</span>";

            if (_data.Host.TranslateGameToChat)
            {
                _data.AddToChat(new Message(text, _data.Speaker.Name));
            }
        }
        else if (replicCode.StartsWith(ReplicCodes.Player.ToString()) && replicCode.Length > 1)
        {
            var indexString = replicCode[1..];

            if (int.TryParse(indexString, out var index) && index >= 0 && index < _data.Players.Count)
            {
                if (_data.Speaker != null)
                {
                    _data.Speaker.Replic = "";
                }

                _data.Speaker = _data.Players[index];
                _data.Speaker.Replic = TrimReplic(text);

                logString = $"<span class=\"sr n{index}\">{_data.Speaker.Name}: </span><span class=\"r\">{text}</span>";

                if (_data.Host.TranslateGameToChat)
                {
                    _data.AddToChat(new Message(text, _data.Speaker.Name));
                }
            }
        }
        else if (replicCode == ReplicCodes.Special.ToString())
        {
            logString = $"<span class=\"sp\">{text}</span>";
            _data.OnAddString("* ", text, LogMode.Protocol);
        }
        else
        {
            if (_data.Host.TranslateGameToChat)
            {
                _data.OnAddString(null, text, LogMode.Protocol);
            }

            // all other types of messages are printed only to logs
            logString = $"<span class=\"s\">{text}</span>";
        }

        if (logString != null && _data.Host.MakeLogs)
        {
            logString += "<br/>";
            AddToFileLog(logString);
        }
    }

    private string TrimReplic(string text) => text.Shorten(_data.Host.MaximumReplicTextLength, "…");

    internal void AddToFileLog(Message message) =>
        AddToFileLog(
            $"<span style=\"color: gray; font-weight: bold\">{message.Sender}:</span> " +
            $"<span style=\"font-weight: bold\">{message.Text}</span><br />");

    internal void AddToFileLog(string text)
    {
        if (_data.ProtocolWriter == null)
        {
            if (_data.ProtocolPath != null)
            {
                try
                {
                    var stream = _data.Host.CreateLog(_viewerActions.Client.Name, out var path);
                    _data.ProtocolPath = path;
                    _data.ProtocolWriter = new StreamWriter(stream);
                    _data.ProtocolWriter.Write(text);
                }
                catch (IOException)
                {
                }
            }
        }
        else
        {
            try
            {
                _data.ProtocolWriter.Write(text);
            }
            catch (IOException exc)
            {
                _data.OnAddString(null, $"{_localizer[nameof(R.ErrorWritingLogToDisc)]}: {exc.Message}", LogMode.Log);
                try
                {
                    _data.ProtocolWriter.Dispose();
                }
                catch
                {
                    // Из-за недостатка места на диске плохо закрывается
                }

                _data.ProtocolPath = null;
                _data.ProtocolWriter = null;
            }
            catch (EncoderFallbackException exc)
            {
                _data.OnAddString(null, $"{_localizer[nameof(R.ErrorWritingLogToDisc)]}: {exc.Message}", LogMode.Log);
            }
        }
    }

    [Obsolete]
    private void ParseMessageToPrint(
        XmlReader reader,
        StringBuilder chatMessageBuilder,
        StringBuilder logMessageBuilder,
        ref bool isPrintable,
        ref bool special)
    {
        var name = reader.Name;
        var content = reader.ReadElementContentAsString();

        switch (name)
        {
            case "this.client":
                chatMessageBuilder.AppendFormat("{0}: ", content);
                logMessageBuilder.AppendFormat("<span class=\"l\">{0}: </span>", content);
                break;

            case Constants.Player:
                {
                    isPrintable = true;

                    if (int.TryParse(content, out var playerIndex))
                    {
                        var speaker = $"<span class=\"sr n{playerIndex}\">";

                        var playerName = playerIndex < _data.Players.Count ? _data.Players[playerIndex].Name : "<" + _localizer[nameof(R.UnknownPerson)] + ">";
                        chatMessageBuilder.AppendFormat("{0}: ", playerName);
                        logMessageBuilder.AppendFormat("{0}{1}: </span>", speaker, playerName);
                    }
                }
                break;

            case Constants.Showman:
                isPrintable = true;
                chatMessageBuilder.AppendFormat("{0}: ", content);
                logMessageBuilder.AppendFormat("<span class=\"sh\">{0}: </span>", content);
                break;

            case "replic":
                chatMessageBuilder.Append(content);
                logMessageBuilder.AppendFormat("<span class=\"r\">{0}</span>", content);
                break;

            case "system":
                chatMessageBuilder.Append(content);
                logMessageBuilder.AppendFormat("<span class=\"s\">{0}</span>", content);
                break;

            case "special":
                special = true;
                chatMessageBuilder.AppendFormat("* {0}", content.ToUpper());
                logMessageBuilder.AppendFormat("<span class=\"sl\">{0}</span>", content);
                break;

            case "line":
                chatMessageBuilder.Append('\r');
                logMessageBuilder.Append("<br />");
                break;
        }
    }

    public void Stage()
    {
        switch (_data.Stage)
        {
            case GameStage.Before:
                break;

            case GameStage.Begin:
                TInfo.TStage = TableStage.Sign;

                if (_data.Host.MakeLogs && _data.ProtocolWriter == null)
                {
                    try
                    {
                        var stream = _data.Host.CreateLog(_viewerActions.Client.Name, out string path);
                        _data.ProtocolPath = path;
                        _data.ProtocolWriter = new StreamWriter(stream);
                        _data.ProtocolWriter.Write("<!DOCTYPE html><html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"/><title>" + _localizer[nameof(R.LogTitle)] + "</title>");
                        _data.ProtocolWriter.Write("<style>.sr { font-weight:bold; color: #00FFFF; } .n0 { color: #EF21A9; } .n1 { color: #0BE6CF; } .n2 { color: #EF9F21; } .n3 { color: #FF0000; } .n4 { color: #00FF00; } .n5 { color: #0000FF; } .sp, .sl { font-style: italic; font-weight: bold; } .sh { color: #0AEA2A; font-weight: bold; } .l { color: #646464; font-weight: bold; } .r { font-weight: bold; } .s { font-style: italic; } </style>");
                        _data.ProtocolWriter.Write("</head><body>");
                    }
                    catch (IOException)
                    {

                    }
                    catch (ArgumentException exc)
                    {
                        _data.Host.OnError(exc);
                    }
                    catch (UnauthorizedAccessException exc)
                    {
                        _data.Host.OnError(exc);
                    }
                }

                OnReplic(ReplicCodes.Special.ToString(), $"{_localizer[nameof(R.GameStarted)]} {DateTime.Now}");

                var gameMeta = new StringBuilder($"<span data-tag=\"gameInfo\" data-showman=\"{ClientData.ShowMan?.Name}\"");

                for (var i = 0; i < ClientData.Players.Count; i++)
                {
                    gameMeta.Append($" data-player-{i}=\"{ClientData.Players[i].Name}\"");
                }

                AddToFileLog(gameMeta + "></span>");
                break;

            case GameStage.Round:
            case GameStage.Final:
                TInfo.TStage = TableStage.Round;
                TInfo.Selectable = false;
                _data.Sound = Sounds.RoundBegin;

                foreach (var item in _data.Players)
                {
                    item.State = PlayerState.None;
                    item.Stake = 0;

                    item.SafeStake = false;
                }
                break;

            case GameStage.After:
                if (_data.ProtocolWriter != null)
                {
                    _data.ProtocolWriter.Write("</body></html>");
                }
                else
                {
                    _data.OnAddString(null, _localizer[nameof(R.ErrorWritingLogs)], LogMode.Chat);
                }
                break;

            default:
                break;
        }

    }

    virtual public void GameThemes()
    {
        TInfo.TStage = TableStage.GameThemes;
        _data.EnableMediaLoadButton = false;
    }

    virtual public void RoundThemes(bool print) => UI.Execute(() => RoundThemesUI(print), exc => _data.Host.SendError(exc));

    private void RoundThemesUI(bool print)
    {
        lock (_data.TInfoLock)
        lock (TInfo.RoundInfoLock)
        {
            TInfo.RoundInfo.Clear();

            foreach (var item in _data.TInfo.RoundInfo.Select(themeInfo => new ThemeInfoViewModel(themeInfo)))
            {
                TInfo.RoundInfo.Add(item);
            }
        }

        if (print)
        {
            if (_data.Stage == GameStage.Round)
            {
                TInfo.TStage = TableStage.RoundThemes;
                _data.Host.PlaySound(Sounds.RoundThemes);
            }
            else
            {
                TInfo.TStage = TableStage.Final;
            }
        }

        ClearQuestionState();
    }

    public void ClearQuestionState()
    {
        _prependTableText = null;
        _appendTableText = null;
    }

    virtual public async void Choice()
    {
        TInfo.Text = "";
        TInfo.MediaSource = null;
        TInfo.QuestionContentType = QuestionContentType.Text;
        TInfo.Sound = false;

        foreach (var item in _data.Players)
        {
            item.State = PlayerState.None;
        }

        var select = false;

        lock (_data.ChoiceLock)
        {
            lock (TInfo.RoundInfoLock)
            {
                if (_data.ThemeIndex > -1 &&
                    _data.ThemeIndex < TInfo.RoundInfo.Count &&
                    _data.QuestionIndex > -1 &&
                    _data.QuestionIndex < TInfo.RoundInfo[_data.ThemeIndex].Questions.Count)
                {
                    select = true;
                }
            }
        }

        if (!select)
        {
            return;
        }

        try
        {
            await TInfo.PlaySimpleSelectionAsync(_data.ThemeIndex, _data.QuestionIndex);
        }
        catch (Exception exc)
        {
            _viewerActions.Client.CurrentServer.OnError(exc, false);
        }
    }

    public void OnTextShape(string[] mparams)
    {
        var text = new StringBuilder();

        for (var i = 1; i < mparams.Length; i++)
        {
            text.Append(mparams[i]);

            if (i < mparams.Length - 1)
            {
                text.Append('\n');
            }
        }

        if (TInfo.TStage == TableStage.Question)
        {
            // Toggle TStage change to reapply QuestionTemplateSelector template
            TInfo.TStage = TableStage.Void;
        }

        TInfo.TextLength = 0;
        TInfo.PartialText = true;
        TInfo.Text = text.ToString();
        TInfo.TStage = TableStage.Question;
        TInfo.QuestionContentType = QuestionContentType.Text;
    }

    public void OnContent(string[] mparams)
    {
        if (mparams.Length < 5)
        {
            OnSpecialReplic("Invalid content message");
            return;
        }

        var placement = mparams[1];

        var content = new List<ContentInfo>();

        for (var i = 2; i + 2 < mparams.Length; i++)
        {
            _ = int.TryParse(mparams[i], out var layoutId);

            if (layoutId == 0)
            {
                var contentType = mparams[i + 1];
                var contentValue = mparams[i + 2];

                contentValue = PreprocessUri(contentType, contentValue);

                content.Add(new ContentInfo(contentType, contentValue));

                i += 2;
            }
            else if (TInfo.LayoutMode == LayoutMode.AnswerOptions && i + 3 < mparams.Length && layoutId > 0 && layoutId - 1 < TInfo.AnswerOptions.Options.Length)
            {
                var label = mparams[i + 1];
                var contentType = mparams[i + 2];
                var contentValue = mparams[i + 3];

                contentValue = PreprocessUri(contentType, contentValue);

                var option = TInfo.AnswerOptions.Options[layoutId - 1];

                if (contentType == ContentTypes.Text || contentType == ContentTypes.Image)
                {
                    option.Label = label;
                    option.Content = new ContentViewModel(contentType == ContentTypes.Text ? ContentType.Text : ContentType.Image, contentValue);
                    option.IsVisible = true;
                }

                i += 3;
            }
        }

        _data.ExternalContent.Clear();

        if (content.Count == 0)
        {
            OnSpecialReplic("No content found");
            return;
        }

        switch (placement)
        {
            case ContentPlacements.Screen:
                OnScreenContent(content);
                break;

            case ContentPlacements.Replic:
                var (contentType, contentValue) = content.LastOrDefault();

                if (contentType == ContentTypes.Text)
                {
                    OnReplicText(contentValue);
                }
                break;

            case ContentPlacements.Background:
                var (contentType2, contentValue2) = content.LastOrDefault();

                if (contentType2 == ContentTypes.Audio)
                {
                    OnBackgroundAudio(contentValue2);
                }
                break;

            default:
                break;
        }
    }

    private string PreprocessUri(string contentType, string contentValue)
    {
        if (contentType == ContentTypes.Text)
        {
            return contentValue;
        }

        if (contentValue.StartsWith(Constants.GameHostUri))
        {
            return string.Concat(ClientData.ServerHostUri, contentValue.AsSpan(Constants.GameHostUri.Length));
        }

        return contentValue;
    }

    public void OnContentAppend(string[] mparams)
    {
        if (mparams.Length < 5)
        {
            return;
        }

        var placement = mparams[1]; // Screen only
        var layoutId = mparams[2]; // 0 only
        var contentType = mparams[3];
        var contentValue = mparams[4]; // Text only

        if (placement != ContentPlacements.Screen || layoutId != "0" || contentType != ContentTypes.Text)
        {
            return;
        }

        if (TInfo.TStage != TableStage.Answer && _data.Speaker != null && !_data.Speaker.IsShowman)
        {
            _data.Speaker.Replic = "";
        }

        _data.AtomType = contentType;

        var text = contentValue.UnescapeNewLines();

        var currentText = TInfo.Text ?? "";
        var newTextLength = text.Length;

        var tailIndex = TInfo.TextLength + newTextLength;

        TInfo.Text = currentText[..TInfo.TextLength]
            + text
            + (currentText.Length > tailIndex ? currentText[tailIndex..] : "");

        TInfo.TextLength += newTextLength;
    }

    public void OnContentState(string[] mparams)
    {
        if (mparams.Length < 4)
        {
            return;
        }

        var placement = mparams[1];

        if (TInfo.LayoutMode == LayoutMode.AnswerOptions
            && placement == ContentPlacements.Screen
            && int.TryParse(mparams[2], out var layoutId)
            && layoutId > 0
            && layoutId <= TInfo.AnswerOptions.Options.Length
            && Enum.TryParse<ItemState>(mparams[3], out var state))
        {
            TInfo.AnswerOptions.Options[layoutId - 1].State = state;
        }
    }

    private void OnScreenContent(IEnumerable<ContentInfo> contentInfo)
    {
        if (TInfo.TStage != TableStage.Answer && _data.Speaker != null && !_data.Speaker.IsShowman)
        {
            _data.Speaker.Replic = "";
        }

        TInfo.TStage = TableStage.Question;

        var groups = new List<ContentGroup>();
        ContentGroup? currentGroup = null;

        foreach (var (contentType, contentValue) in contentInfo)
        {
            _data.AtomType = contentType;

            switch (contentType)
            {
                case ContentTypes.Text:
                    if (currentGroup != null)
                    {
                        currentGroup.Init();
                        groups.Add(currentGroup);
                        currentGroup = null;
                    }

                    var text = contentValue.UnescapeNewLines().Shorten(_data.Host.MaximumTableTextLength, "…");
                    var group = new ContentGroup { Weight = Math.Max(SmallContentWeight, Math.Min(LargeContentWeight, (double)text.Length / TextLengthWithBasicWeight)) };
                    group.Content.Add(new ContentViewModel(ContentType.Text, text, groups.Count == 0 ? TInfo.TextSpeed : 0.0));
                    groups.Add(group);
                    break;

                case ContentTypes.Video:
                case ContentTypes.Image:
                case ContentTypes.Html:
                    currentGroup ??= new ContentGroup { Weight = LargeContentWeight };

                    var uri = contentValue;

                    if (contentType != ContentTypes.Html
                        && !uri.StartsWith("http://localhost")
                        && !Data.Host.LoadExternalMedia
                        && !ExternalUrlOk(uri))
                    {
                        currentGroup.Content.Add(new ContentViewModel(ContentType.Text, string.Format(_localizer[nameof(R.ExternalLink)], uri)));
                        _data.EnableMediaLoadButton = true;
                        _data.ExternalContent.Add((contentType, uri));
                        return;
                    }

                    if (!Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out var mediaUri))
                    {
                        OnSpecialReplic($"Unparsable uri: {uri}");
                        return;
                    }

                    uri = _localFileManager.TryGetFile(mediaUri) ?? uri;
                    Data.Host.Log($"Media uri conversion: {mediaUri} => {uri}");

                    var tableContentType = contentType == ContentTypes.Image
                        ? ContentType.Image
                        : (contentType == ContentTypes.Video ? ContentType.Video : ContentType.Html);

                    currentGroup.Content.Add(new ContentViewModel(tableContentType, uri));
                    break;
            }
        }

        if (currentGroup != null)
        {
            currentGroup.Init();
            groups.Add(currentGroup);
        }

        if (Data.Host.AttachContentToTable && groups.Count == 1 && groups[0].Content.Count == 1 && groups[0].Content[0].Type != ContentType.Text)
        {
            if (_prependTableText != null)
            {
                var group = new ContentGroup();
                group.Content.Add(new ContentViewModel(ContentType.Text, _prependTableText, 0.0));
                groups.Insert(0, group);
            }
            else if (_appendTableText != null)
            {
                var group = new ContentGroup();
                group.Content.Add(new ContentViewModel(ContentType.Text, _appendTableText, 0.0));
                groups.Add(group);
            }
        }

        TInfo.Content = groups;
        TInfo.QuestionContentType = QuestionContentType.Collection;
    }

    private void OnReplicText(string text) => OnReplic(ReplicCodes.Showman.ToString(), text.UnescapeNewLines());

    private void OnBackgroundAudio(string uri)
    {
        if (TInfo.TStage != TableStage.Question)
        {
            TInfo.TStage = TableStage.Question;
            TInfo.QuestionContentType = QuestionContentType.Void;
        }

        if (!uri.StartsWith("http://localhost") && !Data.Host.LoadExternalMedia && !ExternalUrlOk(uri))
        {
            TInfo.Text = string.Format(_localizer[nameof(R.ExternalLink)], uri);
            TInfo.QuestionContentType = QuestionContentType.SpecialText;
            _data.EnableMediaLoadButton = true;
            _data.ExternalContent.Add((ContentTypes.Audio, uri));
            return;
        }

        if (!Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out var mediaUri))
        {
            return;
        }

        uri = _localFileManager.TryGetFile(mediaUri) ?? uri;

        TInfo.SoundSource = new MediaSource(uri);
        TInfo.Sound = true;

        if (TInfo.QuestionContentType == QuestionContentType.Void)
        {
            var additionalText = _appendTableText ?? _prependTableText;

            if (Data.Host.AttachContentToTable && additionalText != null)
            {
                var groups = new List<ContentGroup>();
                var group = new ContentGroup();
                group.Content.Add(new ContentViewModel(ContentType.Text, additionalText, 0.0));
                groups.Add(group);
                TInfo.Content = groups;
                TInfo.QuestionContentType = QuestionContentType.Collection;
            }
            else
            {
                TInfo.QuestionContentType = QuestionContentType.Clef;
            }
        }
    }

    [Obsolete]
    public void OnScreenContent(string[] mparams)
    {
        if (TInfo.TStage != TableStage.Answer && _data.Speaker != null && !_data.Speaker.IsShowman)
        {
            _data.Speaker.Replic = "";
        }

        _data.AtomType = mparams[1];

        var isPartial = _data.AtomType == Constants.PartialText;

        if (!isPartial)
        {
            if (_data.AtomType != AtomTypes.Oral)
            {
                TInfo.Text = "";
            }

            TInfo.PartialText = false;
        }

        TInfo.TStage = TableStage.Question;
        TInfo.IsMediaStopped = false;

        _data.EnableMediaLoadButton = false;

        switch (_data.AtomType)
        {
            case AtomTypes.Text:
            case Constants.PartialText:
                var text = new StringBuilder();

                for (var i = 2; i < mparams.Length; i++)
                {
                    text.Append(mparams[i]);

                    if (i < mparams.Length - 1)
                    {
                        text.Append('\n');
                    }
                }

                if (isPartial)
                {
                    var currentText = TInfo.Text ?? "";
                    var newTextLength = text.Length;

                    var tailIndex = TInfo.TextLength + newTextLength;

                    TInfo.Text = currentText[..TInfo.TextLength]
                        + text
                        + (currentText.Length > tailIndex ? currentText[tailIndex..] : "");

                    TInfo.TextLength += newTextLength;
                }
                else
                {
                    TInfo.Text = text.ToString().Shorten(_data.Host.MaximumTableTextLength, "…");
                }

                TInfo.QuestionContentType = QuestionContentType.Text;
                TInfo.Sound = false;
                _data.EnableMediaLoadButton = false;
                break;

            case AtomTypes.Video:
            case AtomTypes.Audio:
            case AtomTypes.AudioNew:
            case AtomTypes.Image:
            case AtomTypes.Html:
                string uri;

                switch (mparams[2])
                {
                    case MessageParams.Atom_Uri:
                        uri = mparams[3];

                        if (uri.Contains(Constants.GameHost))
                        {
                            var address = ClientData.ServerAddress;

                            if (!string.IsNullOrWhiteSpace(address))
                            {
                                if (Uri.TryCreate(address, UriKind.Absolute, out var hostUri))
                                {
                                    uri = uri.Replace(Constants.GameHost, hostUri.Host);
                                }
                            }
                        }
                        else if (uri.Contains(Constants.ServerHost))
                        {
                            uri = uri.Replace(Constants.ServerHost, ClientData.ServerPublicUrl ?? ClientData.ServerAddress);
                        }
                        else if (_data.AtomType != AtomTypes.Html
                            && !uri.StartsWith("http://localhost")
                            && !Data.Host.LoadExternalMedia
                            && !ExternalUrlOk(uri))
                        {
                            TInfo.Text = string.Format(_localizer[nameof(R.ExternalLink)], uri);
                            TInfo.QuestionContentType = QuestionContentType.SpecialText;
                            TInfo.Sound = false;

                            _data.EnableMediaLoadButton = true;
                            _data.ExternalContent.Add((_data.AtomType, uri));
                            return;
                        }

                        break;

                    default:
                        return;
                }

                if (!Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out var mediaUri))
                {
                    return;
                }

                uri = _localFileManager.TryGetFile(mediaUri) ?? uri;

                if (_data.AtomType == AtomTypes.Image)
                {
                    TInfo.MediaSource = new MediaSource(uri);
                    TInfo.QuestionContentType = QuestionContentType.Image;
                    TInfo.Sound = false;
                }
                else if (_data.AtomType == AtomTypes.Audio || _data.AtomType == AtomTypes.AudioNew)
                {
                    TInfo.SoundSource = new MediaSource(uri);
                    TInfo.QuestionContentType = QuestionContentType.Clef;
                    TInfo.Sound = true;
                }
                else if (_data.AtomType == AtomTypes.Video)
                {
                    TInfo.MediaSource = new MediaSource(uri);
                    TInfo.QuestionContentType = QuestionContentType.Video;
                    TInfo.Sound = false;
                }
                else
                {
                    TInfo.MediaSource = new MediaSource(uri);
                    TInfo.QuestionContentType = QuestionContentType.Html;
                    TInfo.Sound = false;
                }

                _data.EnableMediaLoadButton = false;
                break;
        }
    }

    public void ReloadMedia()
    {
        _data.EnableMediaLoadButton = false;

        var externalContent = _data.ExternalContent.FirstOrDefault();

        if (externalContent == default)
        {
            return;
        }

        TInfo.MediaSource = new MediaSource(externalContent.Uri);

        switch (externalContent.ContentType)
        {
            case AtomTypes.Image:
                TInfo.QuestionContentType = QuestionContentType.Image;
                TInfo.Sound = false;
                break;

            case AtomTypes.Audio:
            case AtomTypes.AudioNew:
                TInfo.QuestionContentType = QuestionContentType.Clef;
                TInfo.Sound = true;
                break;

            case AtomTypes.Video:
                TInfo.QuestionContentType = QuestionContentType.Video;
                TInfo.Sound = false;
                break;
        }
    }

    private bool ExternalUrlOk(string uri) =>
        ClientData.ContentPublicUrls != null && ClientData.ContentPublicUrls.Any(publicUrl => uri.StartsWith(publicUrl));

    public void OnBackgroundContent(string[] mparams)
    {
        if (TInfo.TStage != TableStage.Question)
        {
            TInfo.TStage = TableStage.Question;
            TInfo.QuestionContentType = QuestionContentType.Clef;
        }

        var atomType = mparams[1];

        switch (atomType)
        {
            case AtomTypes.Audio:
            case AtomTypes.AudioNew:
                string uri;

                switch (mparams[2])
                {
                    case MessageParams.Atom_Uri:
                        uri = mparams[3];

                        if (uri.Contains(Constants.GameHost))
                        {
                            var address = ClientData.ServerAddress;

                            if (!string.IsNullOrWhiteSpace(address))
                            {
                                if (Uri.TryCreate(address, UriKind.Absolute, out var hostUri))
                                {
                                    uri = uri.Replace(Constants.GameHost, hostUri.Host);
                                }
                            }
                        }
                        else if (uri.Contains(Constants.ServerHost))
                        {
                            uri = uri.Replace(Constants.ServerHost, ClientData.ServerPublicUrl ?? ClientData.ServerAddress);
                        }
                        else if (!uri.StartsWith("http://localhost") && !Data.Host.LoadExternalMedia && !ExternalUrlOk(uri))
                        {
                            TInfo.Text = string.Format(_localizer[nameof(R.ExternalLink)], uri);
                            TInfo.QuestionContentType = QuestionContentType.SpecialText;
                            TInfo.Sound = false;

                            _data.EnableMediaLoadButton = true;
                            _data.ExternalContent.Add((atomType, uri));
                        }

                        break;

                    default:
                        return;
                }

                Uri? mediaUri;

                if (!Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out mediaUri))
                {
                    return;
                }

                uri = _localFileManager.TryGetFile(mediaUri) ?? uri;

                TInfo.SoundSource = new MediaSource(uri);
                TInfo.Sound = true;

                if (TInfo.QuestionContentType == QuestionContentType.Void)
                {
                    TInfo.QuestionContentType = QuestionContentType.Clef;
                }

                break;
        }
    }

    virtual public void OnAtomHint(string hint)
    {
        TInfo.Hint = hint;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(HintLifetime);
                TInfo.Hint = "";
            }
            catch (Exception exc)
            {
                _data.Host.SendError(exc);
            }
        });
    }

    public void OnRightAnswer(string answer)
    {
        if (TInfo.LayoutMode == LayoutMode.Simple)
        {
            try
            {
                TInfo.TStage = TableStage.Answer;
                TInfo.Text = answer;
                _data.EnableMediaLoadButton = false;
            }
            catch (NullReferenceException exc)
            {
                // Strange error happened periodically in WPF bindings
                _data.Host.SendError(exc);
            }
        }
        else
        {
            var options = TInfo.AnswerOptions.Options;

            if (options == null)
            {
                return;
            }

            var rightIndex = Array.FindIndex(options, o => o.Label == answer);

            if (rightIndex == -1)
            {
                OnReplic(ReplicCodes.Showman.ToString(), $"{_localizer[nameof(R.RightAnswer)]}: {answer}");
                return;
            }

            for (int i = 0; i < options.Length; i++)
            {
                if (i == rightIndex)
                {
                    options[i].State = ItemState.Right;
                }
                else if (options[i].State == ItemState.Active)
                {
                    options[i].State = ItemState.Normal;
                }
            }
        }
    }

    public void OnRightAnswerStart(string answer)
    {
        TInfo.AnimateText = false;
        TInfo.PartialText = false;
        TInfo.Content = Array.Empty<ContentGroup>();
        TInfo.QuestionContentType = QuestionContentType.Void;
        TInfo.Sound = false;

        _prependTableText = null;
        _appendTableText = answer.LeaveFirst(MaxAdditionalTableTextLength);
    }

    public void OnThemeComments(string comments)
    {
        _prependTableText = comments.UnescapeNewLines().LeaveFirst(MaxAdditionalTableTextLength);
    }

    public void Try() => TInfo.QuestionStyle = QuestionStyle.WaitingForPress;

    /// <summary>
    /// Нельзя жать на кнопку
    /// </summary>
    /// <param name="text">Кто уже нажал или время вышло</param>
    virtual public void EndTry(string text)
    {
        TInfo.QuestionStyle = QuestionStyle.Normal;
        TInfo.IsMediaStopped = true;

        if (!int.TryParse(text, out int number))
        {
            _data.Sound = Sounds.QuestionNoAnswers;
            return;
        }

        if (number < 0 || number >= _data.Players.Count)
        {
            return;
        }

        _data.Players[number].State = PlayerState.Press;
    }

    virtual public void ShowTablo() => TInfo.TStage = TableStage.RoundTable;

    /// <summary>
    /// Игрок получил или потерял деньги
    /// </summary>
    virtual public void Person(int playerIndex, bool isRight)
    {
        if (isRight)
        {
            _data.Sound = _data.CurPriceRight >= 2000 ? Sounds.ApplauseBig : Sounds.ApplauseSmall;
            _data.Players[playerIndex].State = PlayerState.Right;
        }
        else
        {
            _data.Sound = Sounds.AnswerWrong;
            _data.Players[playerIndex].Pass = true;
            _data.Players[playerIndex].State = PlayerState.Wrong;
        }

        AddToFileLog($"<span data-tag=\"sumChange\" data-playerIndex=\"{playerIndex}\" data-change=\"{(isRight ? 1 : -1) * ClientData.CurPriceRight}\"></span>");
    }

    public void OnPersonFinalStake(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= _data.Players.Count)
        {
            return;
        }

        _data.Players[playerIndex].Stake = -4;
    }

    public void OnPersonFinalAnswer(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= _data.Players.Count)
        {
            return;
        }

        _data.Players[playerIndex].State = PlayerState.HasAnswered;
    }

    public void OnPersonApellated(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= _data.Players.Count)
        {
            return;
        }

        _data.Players[playerIndex].State = PlayerState.HasAnswered;
    }

    public void OnQuestionStart()
    {
        TInfo.QuestionContentType = QuestionContentType.Text;
        TInfo.Sound = false;
        TInfo.LayoutMode = LayoutMode.Simple;
        TInfo.PartialText = false;
        TInfo.IsMediaStopped = false;
        _data.EnableMediaLoadButton = false;
        _data.ExternalContent.Clear();

        switch (_data.QuestionType)
        {
            case QuestionTypes.Auction: // deprecated
            case QuestionTypes.Stake:
                {
                    TInfo.Text = _localizer[nameof(R.Label_Auction)];

                    lock (TInfo.RoundInfoLock)
                    {
                        for (int i = 0; i < TInfo.RoundInfo.Count; i++)
                        {
                            TInfo.RoundInfo[i].Active = i == _data.ThemeIndex;
                        }
                    }

                    TInfo.TStage = TableStage.Special;
                    _data.Sound = Sounds.QuestionStake;
                    break;
                }

            case QuestionTypes.Cat: // deprecated
            case QuestionTypes.BagCat: // deprecated
            case QuestionTypes.Secret:
            case QuestionTypes.SecretNoQuestion:
            case QuestionTypes.SecretPublicPrice:
                {
                    TInfo.Text = _localizer[nameof(R.Label_CatInBag)];

                    lock (TInfo.RoundInfoLock)
                    {
                        foreach (var item in TInfo.RoundInfo)
                        {
                            item.Active = false;
                        }
                    }

                    TInfo.TStage = TableStage.Special;
                    _data.Sound = Sounds.QuestionSecret;
                    break;
                }

            case QuestionTypes.Sponsored: // deprecated
            case QuestionTypes.NoRisk:
                {
                    TInfo.Text = _localizer[nameof(R.Label_Sponsored)];

                    lock (TInfo.RoundInfoLock)
                    {
                        foreach (var item in TInfo.RoundInfo)
                        {
                            item.Active = false;
                        }
                    }

                    TInfo.TStage = TableStage.Special;
                    _data.Sound = Sounds.QuestionNoRisk;
                    break;
                }

            case QuestionTypes.Simple:
                TInfo.TimeLeft = 1.0;
                break;

            default:
                foreach (var item in TInfo.RoundInfo)
                {
                    item.Active = false;
                }
                break;
        }
    }

    public void StopRound() => TInfo.TStage = TableStage.Sign;

    virtual public void Out(int themeIndex)
    {
        TInfo.PlaySelection(themeIndex);
        _data.Sound = Sounds.FinalDelete;
    }

    public void Winner() => UI.Execute(WinnerUI, exc => _data.Host.SendError(exc));

    private void WinnerUI()
    {
        if (_data.Winner > -1)
        {
            _data.Sound = Sounds.ApplauseFinal;
        }

        // Лучшие игроки
        _data.Host.SaveBestPlayers(_data.Players);
    }

    public void TimeOut() => _data.Sound = Sounds.RoundTimeout;

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_data.ProtocolWriter != null)
        {
            _data.ProtocolWriter.Dispose();
            _data.ProtocolWriter = null;
        }

        _cancellation.Cancel();

        await _localFileManagerTask;

        _localFileManager.Dispose();
        _cancellation.Dispose();

        await base.DisposeAsync(disposing);
    }

    public void FinalThink() => _data.Host.PlaySound(Sounds.FinalThink);

    public void UpdatePicture(Account account, string path)
    {
        if (path.Contains(Constants.GameHost))
        {
            if (!string.IsNullOrWhiteSpace(ClientData.ServerAddress))
            {
                var remoteUri = ClientData.ServerAddress;

                if (Uri.TryCreate(remoteUri, UriKind.Absolute, out var hostUri))
                {
                    account.Picture = path.Replace(Constants.GameHost, hostUri.Host);
                }
                else
                {
                    // Блок для отлавливания специфической ошибки
                    _data.Host.OnPictureError(remoteUri);
                }
            }
        }
        else if (path.Contains(Constants.ServerHost))
        {
            if (!string.IsNullOrWhiteSpace(ClientData.ServerAddress))
            {
                account.Picture = path.Replace(Constants.ServerHost, ClientData.ServerPublicUrl ?? ClientData.ServerAddress);
            }
        }
        else
        {
            account.Picture = path;
        }
    }

    /// <summary>
    /// Попытка осуществить повторное подключение к серверу
    /// </summary>
    public async void TryConnect(IConnector connector)
    {
        try
        {
            OnReplic(ReplicCodes.Special.ToString(), _localizer[nameof(R.TryReconnect)]);

            var result = await connector.ReconnectToServer();

            if (!result)
            {
                AnotherTry(connector);
                return;
            }

            OnReplic(ReplicCodes.Special.ToString(), _localizer[nameof(R.ReconnectOK)]);
            await connector.RejoinGame();

            if (!string.IsNullOrEmpty(connector.Error))
            {
                if (connector.CanRetry)
                {
                    AnotherTry(connector);
                }
                else
                {
                    OnReplic(ReplicCodes.Special.ToString(), connector.Error);
                }
            }
            else
            {
                OnReplic(ReplicCodes.Special.ToString(), _localizer[nameof(R.ReconnectEntered)]);
            }
        }
        catch (Exception exc)
        {
            try { _data.Host.OnError(exc); }
            catch { }
        }
    }

    private async void AnotherTry(IConnector connector)
    {
        try
        {
            OnReplic(ReplicCodes.Special.ToString(), connector.Error);

            if (!_disposed)
            {
                await Task.Delay(10000);
                TryConnect(connector);
            }
        }
        catch (Exception exc)
        {
            Trace.TraceError("AnotherTry error: " + exc);
        }
    }

    public void OnTextSpeed(double speed) => TInfo.TextSpeed = speed;

    public void SetText(string text, TableStage stage = TableStage.Round)
    {
        TInfo.Text = text;
        TInfo.TStage = stage;
        _data.EnableMediaLoadButton = false;
    }

    public void OnPauseChanged(bool isPaused) => TInfo.Pause = isPaused;

    public void TableLoaded() => UI.Execute(TableLoadedUI, exc => _data.Host.SendError(exc));

    private void TableLoadedUI()
    {
        lock (TInfo.RoundInfoLock)
        {
            for (int i = 0; i < _data.TInfo.RoundInfo.Count; i++)
            {
                if (TInfo.RoundInfo.Count <= i)
                    break;

                TInfo.RoundInfo[i].Questions.Clear();

                foreach (var item in _data.TInfo.RoundInfo[i].Questions.Select(questionInfo => new QuestionInfoViewModel(questionInfo)).ToArray())
                {
                    TInfo.RoundInfo[i].Questions.Add(item);
                }
            }
        }
    }

    public void Resume() => TInfo.IsMediaStopped = false;

    public async void PrintGreeting()
    {
        try
        {
            await Task.Delay(1000);
            _data.OnAddString(null, _localizer[nameof(R.Greeting)] + Environment.NewLine, LogMode.Protocol);
        }
        catch (Exception exc)
        {
            Trace.TraceError("PrintGreeting error: " + exc);
        }
    }

    public void OnTimeChanged()
    {
        
    }

    public void OnTimerChanged(int timerIndex, string timerCommand, string arg, string person)
    {
        if (timerIndex == 1 && timerCommand == "RESUME")
        {
            TInfo.QuestionStyle = QuestionStyle.WaitingForPress;
        }

        if (timerIndex != 2)
        {
            return;
        }

        switch (timerCommand)
        {
            case MessageParams.Timer_Go:
                {
                    if (person != null && int.TryParse(person, out int personIndex))
                    {
                        if (_data.DialogMode == DialogModes.ChangeSum
                            || _data.DialogMode == DialogModes.Manage
                            || _data.DialogMode == DialogModes.None)
                        {
                            if (personIndex == -1)
                            {
                                if (_data.ShowMan != null)
                                {
                                    _data.ShowMan.IsDeciding = true;
                                }
                            }
                            else if (personIndex > -1 && personIndex < _data.Players.Count)
                            {
                                _data.Players[personIndex].IsDeciding = true;
                            }
                        }

                        if (personIndex == -2)
                        {
                            _data.ShowMainTimer = true;
                        }
                    }

                    break;
                }

            case MessageParams.Timer_Stop:
                {
                    if (_data.ShowMan != null)
                    {
                        _data.ShowMan.IsDeciding = false;
                    }

                    foreach (var player in _data.Players)
                    {
                        player.IsDeciding = false;
                    }

                    _data.ShowMainTimer = false;
                    break;
                }
        }
    }

    public void OnPackageLogo(string uri)
    {
        TInfo.TStage = TableStage.Question;

        if (uri.Contains(Constants.GameHost))
        {
            var address = ClientData.ServerAddress;

            if (!string.IsNullOrWhiteSpace(address))
            {
                if (Uri.TryCreate(address, UriKind.Absolute, out var hostUri))
                {
                    uri = uri.Replace(Constants.GameHost, hostUri.Host);
                }
            }
        }
        else if (uri.Contains(Constants.ServerHost))
        {
            uri = uri.Replace(Constants.ServerHost, ClientData.ServerPublicUrl ?? ClientData.ServerAddress);
        }

        if (!Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out _))
        {
            return;
        }

        TInfo.MediaSource = new MediaSource(uri);
        TInfo.QuestionContentType = QuestionContentType.Image;
        TInfo.Sound = false;
    }

    public void OnPersonPass(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= _data.Players.Count)
        {
            return;
        }

        _data.Players[playerIndex].State = PlayerState.Pass;
    }

    public void OnRoundContent(string[] mparams)
    {
        for (var i = 1; i < mparams.Length; i++)
        {
            var uri = mparams[i];

            if (uri.Contains(Constants.GameHost))
            {
                var address = ClientData.ServerAddress;

                if (!string.IsNullOrWhiteSpace(address))
                {
                    if (Uri.TryCreate(address, UriKind.Absolute, out var hostUri))
                    {
                        uri = uri.Replace(Constants.GameHost, hostUri.Host);
                    }
                }
            }
            else if (uri.Contains(Constants.ServerHost))
            {
                uri = uri.Replace(Constants.ServerHost, ClientData.ServerPublicUrl ?? ClientData.ServerAddress);
            }
            else if (!uri.StartsWith("http://localhost") && !Data.Host.LoadExternalMedia && !ExternalUrlOk(uri))
            {
                continue;
            }

            if (!Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out var mediaUri))
            {
                continue;
            }

            _localFileManager.AddFile(mediaUri);
        }
    }

    private void OnSpecialReplic(string message) => OnReplic(ReplicCodes.Special.ToString(), message);

    public void OnUnbanned(string ip) =>
        UI.Execute(
            () =>
            {
                var banned = ClientData.Banned.FirstOrDefault(p => p.Ip == ip);

                if (banned != null)
                {
                    ClientData.Banned.Remove(banned);
                    OnSpecialReplic(string.Format(_localizer[nameof(R.UserUnbanned)], banned.UserName));
                }
            },
            exc => ClientData.Host.OnError(exc));

    public void OnBanned(BannedInfo bannedInfo) =>
        UI.Execute(
            () =>
            {
                ClientData.Banned.Add(bannedInfo);
            },
            exc => ClientData.Host.OnError(exc));

    public void OnBannedList(IEnumerable<BannedInfo> banned) =>
        UI.Execute(() =>
        {
            ClientData.Banned.Clear();

            foreach (var item in banned)
            {
                ClientData.Banned.Add(item);
            }
        },
        exc => ClientData.Host.OnError(exc));

    public void SetCaption(string caption) => TInfo.Caption = caption;

    public void OnGameMetadata(string gameName, string packageName, string contactUri, string voiceChatUri)
    {
        var gameInfo = new StringBuilder();

        var coersedGameName = gameName.Length > 0 ? gameName : R.LocalGame;

        gameInfo.AppendFormat(R.GameName).Append(": ").Append(coersedGameName).AppendLine();
        gameInfo.AppendFormat(R.PackageName).Append(": ").Append(packageName).AppendLine();
        gameInfo.AppendFormat(R.ContactUri).Append(": ").Append(contactUri).AppendLine();
        gameInfo.AppendFormat(R.VoiceChatLink).Append(": ").Append(voiceChatUri).AppendLine();

        ClientData.GameMetadata = gameInfo.ToString();

        if (!string.IsNullOrEmpty(voiceChatUri) && Uri.IsWellFormedUriString(voiceChatUri, UriKind.Absolute))
        {
            ClientData.VoiceChatUri = voiceChatUri;
        }
    }

    public void AddPlayer(PlayerAccount account) => UI.Execute(
        () =>
        {
            ClientData.PlayersObservable.Add(account);
        },
        ClientData.Host.OnError);

    public void RemovePlayerAt(int index) => UI.Execute(
        () =>
        {
            ClientData.PlayersObservable.RemoveAt(index);
        },
        ClientData.Host.OnError);

    public void ResetPlayers() => UI.Execute(
        () =>
        {
            ClientData.PlayersObservable.Clear();

            foreach (var player in ClientData.Players)
            {
                ClientData.PlayersObservable.Add(player);
            }
        },
        ClientData.Host.OnError);

    public void OnAnswerOptions(bool questionHasScreenContent, IEnumerable<string> optionsTypes)
    {
        TInfo.AnswerOptions.Options = optionsTypes.Select(i => new ItemViewModel()).ToArray();
        TInfo.LayoutMode = LayoutMode.AnswerOptions;
        TInfo.TStage = TableStage.Question;
    }
}
