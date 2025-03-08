using Microsoft.Extensions.DependencyInjection;
using Notions;
using SICore.Clients.Viewer;
using SICore.Contracts;
using SIData;
using SIGame.ViewModel;
using SIGame.ViewModel.Contracts;
using SIGame.ViewModel.Models;
using SIGame.ViewModel.PlatformSpecific;
using SIGame.ViewModel.Properties;
using SIPackages.Core;
using SIUI.ViewModel;
using SIUI.ViewModel.Core;
using System.Diagnostics;
using System.Text;
using Utils;
using Utils.Timers;
using R = SICore.Properties.Resources;

namespace SICore;

/// <summary>
/// Defines a human viewer logic.
/// </summary>
public sealed class ViewerHumanLogic : Logic<ViewerData>, IViewerLogic, IAsyncDisposable
{
    private record struct ContentInfo(string Type, string Uri, string OriginalUri);

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

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private bool _disposed = false;

    private readonly ILocalFileManager _localFileManager = 
        PlatformManager.Instance.ServiceProvider!.GetRequiredService<ILocalFileManager>();

    private readonly ViewerActions _viewerActions;

    private readonly ILocalizer _localizer;

    public TableInfoViewModel TInfo { get; }

    public bool CanSwitchType => true;

    private string? _prependTableText;
    private string? _appendTableText;

    private readonly AppSettingsCore _appSettings = new();
    private readonly IAnimatableTimer _timer = PlatformManager.Instance.GetAnimatableTimer();
    private bool _runTimer = false;
    private double? _initialTime = null;

    private readonly GameViewModel _gameViewModel;

    private readonly string _serverHostUri = "";
    private readonly string? _serverAddress;
    private readonly string? _serverPublicUrl;
    private readonly string[]? _contentPublicUrls;

    private StreamWriter? _gameLogger = null;
    private string? _logFilePath = null;

    private readonly List<string> _chatTable = new();

    public ViewerHumanLogic(
        GameViewModel gameViewModel,
        ViewerData data,
        ViewerActions viewerActions,
        ILocalizer localizer,
        string serverAddress,
        string? serverPublicUrl = null,
        string[]? contentPublicUrls = null)
        : base(data)
    {
        _gameViewModel = gameViewModel;
        _viewerActions = viewerActions;
        _localizer = localizer;

        _gameViewModel.Logic = this;
        _gameViewModel.Disposed += GameViewModel_Disposed;

        TInfo = _gameViewModel.TInfo;

        TInfo.PropertyChanged += TInfo_PropertyChanged;
        TInfo.MediaLoad += TInfo_MediaLoad;
        TInfo.MediaLoadError += TInfo_MediaLoadError;

        TInfo.QuestionSelected += TInfo_QuestionSelected;
        TInfo.ThemeSelected += TInfo_ThemeSelected;
        TInfo.AnswerSelected += TInfo_AnswerSelected;

        TInfo.QuestionToggled += TInfo_QuestionToggled;

        _localFileManager.Error += LocalFileManager_Error;

        _timer.KeepFinalValue = true;
        _timer.TimeChanged += Timer_TimeChanged;
        _serverAddress = serverAddress;
        _serverPublicUrl = serverPublicUrl ?? serverAddress;

        if (!string.IsNullOrWhiteSpace(serverAddress))
        {
            if (Uri.TryCreate(serverAddress, UriKind.Absolute, out var hostUri))
            {
                _serverHostUri = "http://" + hostUri.Host;
            }
        }

        _contentPublicUrls = contentPublicUrls;
    }

    private void TInfo_QuestionToggled(QuestionInfoViewModel question)
    {
        var found = false;

        for (var i = 0; i < TInfo.RoundInfo.Count; i++)
        {
            for (var j = 0; j < TInfo.RoundInfo[i].Questions.Count; j++)
            {
                if (TInfo.RoundInfo[i].Questions[j] == question)
                {
                    found = true;
                    _viewerActions.SendMessageWithArgs(Messages.Toggle, i, j);
                    break;
                }
            }

            if (found)
            {
                break;
            }
        }
    }

    private async void GameViewModel_Disposed() => await DisposeAsync();

    private void Timer_TimeChanged(IAnimatableTimer timer)
    {
        TInfo.PartialImageVisibility = timer.Time / 100;
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

    public void ClearSelections(bool full = false) => _gameViewModel.ClearSelections(full);

    public void OnSelectPlayer(Models.SelectPlayerReason reason)
    {
        _gameViewModel.ClearReplic();
        _gameViewModel.Hint = GetSelectHint(reason);
        _gameViewModel.SelectionMode = SelectionMode.SelectPlayer;
    }

    private static string GetSelectHint(Models.SelectPlayerReason selectionMode) => selectionMode switch
    {
        Models.SelectPlayerReason.Answerer => Resources.HintSelectCatPlayer,
        Models.SelectPlayerReason.Chooser => Resources.HintSelectStarter,
        Models.SelectPlayerReason.Deleter => Resources.HintThemeDeleter,
        Models.SelectPlayerReason.Staker => Resources.HintSelectStaker,
        _ => "",
    };

    private void TInfo_AnswerSelected(ItemViewModel item)
    {
        _gameViewModel.SendAnswer.Execute(item.Label);
        ClearSelections(true);
    }

    private void LocalFileManager_Error(Uri mediaUri, Exception e) =>
        _gameViewModel.OnAddString(
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

        _gameViewModel.OnAddString(null, $"{_localizer[nameof(R.MediaLoadError)]} {exc.MediaUri}: {error}{Environment.NewLine}", LogMode.Log);
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

    /// <summary>
    /// Adds mesage to the game chat.
    /// </summary>
    /// <param name="message">Message to add.</param>
    private void AddToChat(Message message)
    {
        var index = _chatTable.IndexOf(message.Sender);

        // if user is not present in user list, add him
        if (index == -1)
        {
            _chatTable.Add(message.Sender);
            index = _chatTable.Count - 1;
        }

        _gameViewModel.OnAddString(message.Sender, message.Text, LogMode.Chat + index);
    }

    public void ReceiveText(Message m)
    {
        AddToChat(m);

        if (_data.Host.MakeLogs)
        {
            AddToFileLog(m);
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

            _gameViewModel.ClearReplic();
            _gameViewModel.Speaker = _data.ShowMan;
            _gameViewModel.Speaker.Replic = TrimReplic(text);

            logString = $"<span class=\"sh\">{_gameViewModel.Speaker.Name}: </span><span class=\"r\">{text}</span>";

            if (_data.Host.TranslateGameToChat)
            {
                AddToChat(new Message(text, _gameViewModel.Speaker.Name));
            }
        }
        else if (replicCode.StartsWith(ReplicCodes.Player.ToString()) && replicCode.Length > 1)
        {
            var indexString = replicCode[1..];

            if (int.TryParse(indexString, out var index) && index >= 0 && index < _data.Players.Count)
            {
                _gameViewModel.ClearReplic();
                _gameViewModel.Speaker = _data.Players[index];
                _gameViewModel.Speaker.Replic = TrimReplic(text);

                logString = $"<span class=\"sr n{index}\">{_gameViewModel.Speaker.Name}: </span><span class=\"r\">{text}</span>";

                if (_data.Host.TranslateGameToChat)
                {
                    AddToChat(new Message(text, _gameViewModel.Speaker.Name));
                }
            }
        }
        else if (replicCode == ReplicCodes.Special.ToString())
        {
            logString = $"<span class=\"sp\">{text}</span>";
            _gameViewModel.OnAddString("* ", text, LogMode.Protocol);
        }
        else
        {
            if (_data.Host.TranslateGameToChat)
            {
                _gameViewModel.OnAddString(null, text, LogMode.Protocol);
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
        if (_gameLogger == null)
        {
            if (_logFilePath != null)
            {
                try
                {
                    var stream = _data.Host.CreateLog(_viewerActions.Client.Name, out var path);
                    _logFilePath = path;
                    _gameLogger = new StreamWriter(stream);
                    _gameLogger.Write(text);
                }
                catch (IOException exc)
                {
                    _gameViewModel.OnAddString(null, $"{Resources.ErrorWritingLogToDisc}: {exc.Message}", LogMode.Log);
                }
            }
        }
        else
        {
            try
            {
                _gameLogger.Write(text);
            }
            catch (IOException exc)
            {
                _gameViewModel.OnAddString(null, $"{Resources.ErrorWritingLogToDisc}: {exc.Message}", LogMode.Log);
                
                try
                {
                    _gameLogger.Dispose();
                }
                catch
                {
                    // Can be problems when there is not enough space on disk
                }

                _logFilePath = null;
                _gameLogger = null;
            }
            catch (EncoderFallbackException exc)
            {
                _gameViewModel.OnAddString(null, $"{Resources.ErrorWritingLogToDisc}: {exc.Message}", LogMode.Log);
            }
        }
    }

    public void OnAd(string? text = null)
    {
        _gameViewModel.Ad = text;

        if (!string.IsNullOrEmpty(text))
        {
            TInfo.Text = "";
            TInfo.QuestionContentType = QuestionContentType.Text;
            TInfo.Sound = false;
            TInfo.TStage = TableStage.Question;
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

                if (_data.Host.MakeLogs && _gameLogger == null)
                {
                    try
                    {
                        var stream = _data.Host.CreateLog(_viewerActions.Client.Name, out string path);
                        _logFilePath = path;
                        _gameLogger = new StreamWriter(stream);
                        _gameLogger.Write("<!DOCTYPE html><html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"/><title>" + _localizer[nameof(R.LogTitle)] + "</title>");
                        _gameLogger.Write("<style>.sr { font-weight:bold; color: #00FFFF; } .n0 { color: #EF21A9; } .n1 { color: #0BE6CF; } .n2 { color: #EF9F21; } .n3 { color: #FF0000; } .n4 { color: #00FF00; } .n5 { color: #0000FF; } .sp, .sl { font-style: italic; font-weight: bold; } .sh { color: #0AEA2A; font-weight: bold; } .l { color: #646464; font-weight: bold; } .r { font-weight: bold; } .s { font-style: italic; } </style>");
                        _gameLogger.Write("</head><body>");
                    }
                    catch (IOException exc)
                    {
                        _gameViewModel.OnAddString(null, $"{Resources.ErrorWritingLogToDisc}: {exc.Message}", LogMode.Log);
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

                OnReplic(ReplicCodes.Special.ToString(), $"{Resources.GameStarted} {DateTime.Now}");

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

                foreach (var player in _data.Players)
                {
                    player.ClearState();
                }

                _gameViewModel.Apellate.CanBeExecuted = false;
                break;

            case GameStage.After:
                _gameLogger?.Write("</body></html>");
                break;

            default:
                break;
        }

        _gameViewModel.Hint = "";
        _gameViewModel.UpdateCommands();
        _gameViewModel.ClearValidation();
        OnAd();
    }

    public void GameThemes()
    {
        TInfo.TStage = TableStage.GameThemes;
        _data.EnableMediaLoadButton = false;
    }

    public void RoundThemes(Models.ThemesPlayMode playMode)
    {
        UI.Execute(() => RoundThemesUI(playMode), exc => _data.Host.SendError(exc));
        OnAd();
    }

    private void RoundThemesUI(Models.ThemesPlayMode playMode)
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

        switch (playMode)
        {
            case Models.ThemesPlayMode.OneByOne:
                TInfo.TStage = TableStage.RoundThemes;
                _data.Host.PlaySound(Sounds.RoundThemes);
                break;
            
            case Models.ThemesPlayMode.AllTogether:
                TInfo.TStage = TableStage.Final;
                break;
        }

        ClearQuestionState();
    }

    public void ClearQuestionState()
    {
        _prependTableText = null;
        _appendTableText = null;
    }

    public async void Choice()
    {
        TInfo.Text = "";
        TInfo.MediaSource = null;
        TInfo.QuestionContentType = QuestionContentType.Text;
        TInfo.Sound = false;

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
            _viewerActions.Client.CurrentNode.OnError(exc, false);
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

    public void OnContentShape(string shape)
    {
        if (TInfo.TStage == TableStage.Question)
        {
            // Toggle TStage change to reapply QuestionTemplateSelector template
            TInfo.TStage = TableStage.Void;
        }

        TInfo.TextLength = 0;
        TInfo.PartialText = true;
        TInfo.Text = shape;
        TInfo.TStage = TableStage.Question;
        TInfo.QuestionContentType = QuestionContentType.Text;
    }

    public void OnOptions(string[] mparams)
    {
        for (var i = 1; i + 1 < mparams.Length; i += 2)
        {
            var optionName = mparams[i];
            var optionValue = mparams[i + 1];

            switch (optionName)
            {
                case nameof(AppSettingsCore.DisplayAnswerOptionsLabels):
                    if (bool.TryParse(optionValue, out var flag))
                    {
                        TInfo.Settings.Model.DisplayAnswerOptionsLabels = flag;
                    }

                    break;

                case nameof(AppSettingsCore.FalseStart):
                    if (bool.TryParse(optionValue, out var falseStart))
                    {
                        _appSettings.FalseStart = falseStart;
                    }

                    break;

                case nameof(AppSettingsCore.PartialText):
                    if (bool.TryParse(optionValue, out var partialText))
                    {
                        _appSettings.PartialText = partialText;
                    }

                    break;

                case nameof(AppSettingsCore.PartialImages):
                    if (bool.TryParse(optionValue, out var partialImages))
                    {
                        _appSettings.PartialImages = partialImages;
                    }

                    break;

                case nameof(AppSettingsCore.TimeSettings.PartialImageTime):
                    if (int.TryParse(optionValue, out var value))
                    {
                        _appSettings.TimeSettings.PartialImageTime = value;
                    }

                    break;

                default:
                    break;
            }
        }
    }

    public void OnToggle(int themeIndex, int questionIndex, int price)
    {
        if (themeIndex < 0
            || themeIndex >= TInfo.RoundInfo.Count
            || questionIndex < 0
            || questionIndex >= TInfo.RoundInfo[themeIndex].Questions.Count)
        {
            return;
        }

        TInfo.RoundInfo[themeIndex].Questions[questionIndex].Price = price;
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

                var processedContentValue = PreprocessUri(contentType, contentValue);

                content.Add(new ContentInfo(contentType, processedContentValue, contentValue));

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
            return;
        }

        switch (placement)
        {
            case ContentPlacements.Screen:
                OnScreenContent(content);
                break;

            case ContentPlacements.Replic:
                var (contentType, contentValue, _) = content.LastOrDefault();

                if (contentType == ContentTypes.Text)
                {
                    OnReplicText(contentValue);
                }
                break;

            case ContentPlacements.Background:
                var (contentType2, contentValue2, originalValue) = content.LastOrDefault();

                if (contentType2 == ContentTypes.Audio)
                {
                    OnBackgroundAudio(contentValue2, originalValue);
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
            return string.Concat(_serverHostUri, contentValue.AsSpan(Constants.GameHostUri.Length));
        }

        if (contentValue.StartsWith(Constants.ServerHost))
        {
            return string.Concat(_serverPublicUrl, contentValue.AsSpan(Constants.ServerHost.Length));
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

        if (TInfo.TStage != TableStage.Answer && _gameViewModel.Speaker != null && !_gameViewModel.Speaker.IsShowman)
        {
            _gameViewModel.Speaker.Replic = "";
        }

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
        if (TInfo.TStage != TableStage.Answer && _gameViewModel.Speaker != null && !_gameViewModel.Speaker.IsShowman)
        {
            _gameViewModel.Speaker.Replic = "";
        }

        TInfo.TStage = TableStage.Question;

        var groups = new List<ContentGroup>();
        ContentGroup? currentGroup = null;

        foreach (var (contentType, contentValue, originalValue) in contentInfo)
        {
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
                        && !_data.Host.LoadExternalMedia
                        && !ExternalUrlOk(uri))
                    {
                        currentGroup.Content.Add(new ContentViewModel(ContentType.Text, string.Format(_localizer[nameof(R.ExternalLink)], uri)));
                        _data.EnableMediaLoadButton = true;
                        _data.ExternalContent.Add((contentType, uri));
                        return;
                    }

                    if (!Uri.TryCreate(uri, UriKind.Absolute, out var mediaUri))
                    {
                        OnSpecialReplic($"Unparsable uri: {uri}");
                        return;
                    }

                    var localUri = _localFileManager.TryGetFile(mediaUri) ?? uri;
                    _data.Host.Log($"Media uri conversion: {mediaUri} => {localUri}");

                    var tableContentType = contentType == ContentTypes.Image
                        ? ContentType.Image
                        : (contentType == ContentTypes.Video ? ContentType.Video : ContentType.Html);

                    currentGroup.Content.Add(new ContentViewModel(tableContentType, localUri, OriginalValue: originalValue));

                    // TODO: this logic should be moved to server; client should receive just boolean flag
                    if (contentType == ContentTypes.Image
                        && ClientData.QuestionType == QuestionTypes.Simple
                        && !_data.IsAnswer
                        && !_appSettings.FalseStart
                        && _appSettings.PartialImages
                        && _appSettings.TimeSettings.PartialImageTime > 0)
                    {
                        TInfo.PartialImage = true;
                        _runTimer = true;
                        _initialTime = 0;
                    }

                    break;
            }
        }

        if (currentGroup != null)
        {
            currentGroup.Init();
            groups.Add(currentGroup);
        }

        if (_data.Host.AttachContentToTable && groups.Count == 1 && groups[0].Content.Count == 1 && groups[0].Content[0].Type != ContentType.Text)
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

        if (_runTimer)
        {
            _timer.Run(_appSettings.TimeSettings.PartialImageTime * 10, false, _initialTime);
        }
    }

    private void OnReplicText(string text) => OnReplic(ReplicCodes.Showman.ToString(), text.UnescapeNewLines());

    private void OnBackgroundAudio(string uri, string originalUri)
    {
        if (TInfo.TStage != TableStage.Question)
        {
            TInfo.TStage = TableStage.Question;
            TInfo.QuestionContentType = QuestionContentType.Void;
        }

        if (!uri.StartsWith("http://localhost") && !_data.Host.LoadExternalMedia && !ExternalUrlOk(uri))
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

        var localUri = _localFileManager.TryGetFile(mediaUri) ?? uri;

        TInfo.SoundSource = new MediaSource(localUri) { OriginalValue = originalUri };
        TInfo.Sound = true;

        if (TInfo.QuestionContentType == QuestionContentType.Void)
        {
            var additionalText = _appendTableText ?? _prependTableText;

            if (_data.Host.AttachContentToTable && additionalText != null)
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
            case ContentTypes.Image:
                TInfo.QuestionContentType = QuestionContentType.Image;
                TInfo.Sound = false;
                break;

            case ContentTypes.Audio:
                TInfo.QuestionContentType = QuestionContentType.Clef;
                TInfo.Sound = true;
                break;

            case ContentTypes.Video:
                TInfo.QuestionContentType = QuestionContentType.Video;
                TInfo.Sound = false;
                break;
        }
    }

    private bool ExternalUrlOk(string uri) => _contentPublicUrls != null && _contentPublicUrls.Any(publicUrl => uri.StartsWith(publicUrl));

    public void OnSetJoinMode(Models.JoinMode joinMode) => _gameViewModel.JoinMode = joinMode;

    public void OnAtomHint(string hint)
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
        _data.IsAnswer = true;

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
        _data.IsAnswer = true;
        TInfo.AnimateText = false;
        TInfo.PartialText = false;
        TInfo.Content = Array.Empty<ContentGroup>();
        TInfo.QuestionContentType = QuestionContentType.Void;
        TInfo.Sound = false;

        _prependTableText = null;

        if (TInfo.LayoutMode == LayoutMode.Simple)
        {
            _appendTableText = answer.LeaveFirst(MaxAdditionalTableTextLength);
        }
    }

    public void OnThemeComments(string comments)
    {
        _prependTableText = comments.UnescapeNewLines().LeaveFirst(MaxAdditionalTableTextLength);
    }

    public void Try()
    {
        TInfo.QuestionStyle = QuestionStyle.WaitingForPress;

        if (_runTimer)
        {
            _timer.Run(_appSettings.TimeSettings.PartialImageTime * 10, false, _initialTime);
        }
    }

    /// <summary>
    /// Нельзя жать на кнопку
    /// </summary>
    /// <param name="text">Кто уже нажал или время вышло</param>
    public void EndTry(string text)
    {
        TInfo.QuestionStyle = QuestionStyle.Normal;
        TInfo.IsMediaStopped = true;

        if (_runTimer)
        {
            _initialTime = _timer.Time;
            _timer.Pause((int)(_timer.Time * _timer.MaxTime / 100), false);
        }

        if (!int.TryParse(text, out int number))
        {
            _data.Sound = Sounds.QuestionNoAnswers;
            return;
        }

        if (number < 0 || number >= _data.Players.Count)
        {
            return;
        }

        _data.Players[number].State = PlayerState.Answering;
    }

    public void OnStopPlay()
    {
        TInfo.IsMediaStopped = true;
    }

    public void ShowTablo() => TInfo.TStage = TableStage.RoundTable;

    public void OnPersonScoreChanged(int playerIndex, bool isRight, int sum)
    {
        if (isRight)
        {
            _data.Sound = sum >= 2000 ? Sounds.ApplauseBig : Sounds.ApplauseSmall;
            _data.Players[playerIndex].State = PlayerState.Right;
        }
        else
        {
            _data.Sound = Sounds.AnswerWrong;
            _data.Players[playerIndex].Pass = true;
            _data.Players[playerIndex].State = PlayerState.Wrong;
        }

        AddToFileLog($"<span data-tag=\"sumChange\" data-playerIndex=\"{playerIndex}\" data-change=\"{(isRight ? 1 : -1) * sum}\"></span>");
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

    public void OnQuestionStart(bool isDefaultType)
    {
        TInfo.QuestionContentType = QuestionContentType.Text;
        TInfo.Sound = false;
        TInfo.LayoutMode = LayoutMode.Simple;
        _data.IsAnswer = false;
        TInfo.AnimateText = true;
        TInfo.PartialText = false;
        TInfo.PartialImage = false;
        TInfo.IsMediaStopped = false;
        _data.EnableMediaLoadButton = false;
        _data.ExternalContent.Clear();

        _runTimer = false;
        _initialTime = 0;

        switch (_data.QuestionType)
        {
            case QuestionTypes.Stake:
                {
                    TInfo.Text = Resources.Label_Auction;
                    HighlightCurrentTheme();
                    TInfo.TStage = TableStage.Special;
                    _data.Sound = Sounds.QuestionStake;
                    break;
                }

            case QuestionTypes.Secret:
            case QuestionTypes.SecretNoQuestion:
            case QuestionTypes.SecretPublicPrice:
                {
                    TInfo.Text = Resources.Label_CatInBag;
                    HideAllThemes();
                    TInfo.TStage = TableStage.Special;
                    _data.Sound = Sounds.QuestionSecret;
                    _prependTableText = null;
                    break;
                }

            case QuestionTypes.NoRisk:
                {
                    TInfo.Text = Resources.Label_Sponsored;
                    HighlightCurrentTheme();
                    TInfo.TStage = TableStage.Special;
                    _data.Sound = Sounds.QuestionNoRisk;
                    break;
                }

            case QuestionTypes.Simple:
                TInfo.TimeLeft = 1.0;

                //if (!isDefaultType)
                //{
                //    TInfo.Text = Resources.QuestionTypeWithButton.ToUpper();
                //    TInfo.TStage = TableStage.Special;
                //}
                break;

            case QuestionTypes.StakeAll:
                if (!isDefaultType)
                {
                    HighlightCurrentTheme();
                    TInfo.Text = Resources.QuestionTypeStakeForAll.ToUpper();
                    TInfo.TStage = TableStage.Special;
                    _data.Sound = Sounds.QuestionStakeAll;
                }
                break;

            case QuestionTypes.ForAll:
                if (!isDefaultType)
                {
                    HighlightCurrentTheme();
                    TInfo.Text = Resources.QuestionTypeForAll.ToUpper();
                    TInfo.TStage = TableStage.Special;
                    _data.Sound = Sounds.QuestionAll;
                }
                break;

            default:
                foreach (var item in TInfo.RoundInfo)
                {
                    item.Active = false;
                }
                break;
        }
    }

    private void HideAllThemes()
    {
        lock (TInfo.RoundInfoLock)
        {
            foreach (var item in TInfo.RoundInfo)
            {
                item.Active = false;
            }
        }
    }

    private void HighlightCurrentTheme()
    {
        lock (TInfo.RoundInfoLock)
        {
            for (int i = 0; i < TInfo.RoundInfo.Count; i++)
            {
                TInfo.RoundInfo[i].Active = i == _data.ThemeIndex;
            }
        }
    }

    public void StopRound()
    {
        TInfo.TStage = TableStage.Sign;
        OnAd();
    }

    public void Out(int themeIndex)
    {
        TInfo.PlaySelection(themeIndex);
        _data.Sound = Sounds.FinalDelete;
    }

    public void OnWinner(int winnerIndex) => UI.Execute(() => WinnerUI(winnerIndex), exc => _data.Host.SendError(exc));

    private void WinnerUI(int winnerIndex)
    {
        if (winnerIndex > -1)
        {
            _data.Sound = Sounds.ApplauseFinal;
        }

        // Лучшие игроки
        _data.Host.SaveBestPlayers(_data.Players);
    }

    public void TimeOut() => _data.Sound = Sounds.RoundTimeout;

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _timer.Dispose();

        _gameLogger?.Dispose();
        
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();

        await _localFileManager.DisposeAsync();
    }

    public void FinalThink() => _data.Host.PlaySound(Sounds.FinalThink);

    public void UpdatePicture(Account account, string path)
    {
        if (path.Contains(Constants.GameHost))
        {
            if (!string.IsNullOrWhiteSpace(_serverAddress))
            {
                if (Uri.TryCreate(_serverAddress, UriKind.Absolute, out var hostUri))
                {
                    account.Picture = path.Replace(Constants.GameHost, hostUri.Host);
                }
                else
                {
                    _data.Host.OnPictureError(_serverAddress);
                }
            }
        }
        else if (path.Contains(Constants.ServerHost))
        {
            if (!string.IsNullOrWhiteSpace(_serverAddress))
            {
                account.Picture = path.Replace(Constants.ServerHost, _serverPublicUrl);
            }
        }
        else
        {
            account.Picture = path;
        }
    }

    public void UpdateAvatar(PersonAccount account, string contentType, string uri)
    {
        var preprocessedUri = PreprocessUri(uri);

        if (preprocessedUri == null)
        {
            return;
        }

        switch (contentType)
        {
            case ContentTypes.Image:
                account.Picture = preprocessedUri;
                break;

            case ContentTypes.Video:
                account.AvatarVideoUri = string.IsNullOrEmpty(preprocessedUri) ? null : preprocessedUri;
                break;

            default:
                break;
        }
    }

    private string? PreprocessUri(string uri)
    {
        if (uri.Contains(Constants.GameHost))
        {
            if (!string.IsNullOrWhiteSpace(_serverAddress))
            {
                if (Uri.TryCreate(_serverAddress, UriKind.Absolute, out var hostUri))
                {
                    return uri.Replace(Constants.GameHost, hostUri.Host);
                }
                else
                {
                    _data.Host.OnPictureError(_serverAddress);
                }
            }
        }
        else if (uri.Contains(Constants.ServerHost))
        {
            if (!string.IsNullOrWhiteSpace(_serverAddress))
            {
                return uri.Replace(Constants.ServerHost, _serverPublicUrl);
            }
        }
        else
        {
            return uri;
        }

        return null;
    }

    public void OnTextSpeed(double speed) => TInfo.TextSpeed = speed;

    public void SetText(string text, Models.TableStage stage)
    {
        TInfo.Text = text;
        TInfo.TStage = stage == Models.TableStage.Theme ? TableStage.Theme : stage == Models.TableStage.Round ? TableStage.Round : TableStage.QuestionPrice;
        _data.EnableMediaLoadButton = false;

        if (stage == Models.TableStage.Theme || stage == Models.TableStage.QuestionPrice)
        {
            OnAd();
        }
    }

    public void OnPersonStake() => OnAd();

    public void OnPauseChanged(bool isPaused)
    {
        TInfo.Pause = isPaused;

        var manageTableCommand = _gameViewModel.ManageTable;

        if (manageTableCommand != null)
        {
            manageTableCommand.CanBeExecuted = isPaused;
        }

        if (!isPaused)
        {
            TInfo.IsEditable = false;
        }

        _gameViewModel.OnIsPausedChanged(isPaused);
    }

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
            _gameViewModel.OnAddString(null, _localizer[nameof(R.Greeting)] + Environment.NewLine, LogMode.Protocol);
        }
        catch (Exception exc)
        {
            Trace.TraceError("PrintGreeting error: " + exc);
        }
    }

    public void OnTimeChanged()
    {
        
    }

    public void OnTimerChanged(int timerIndex, string timerCommand, string arg, string? person = null)
    {
        _gameViewModel.OnTimerChanged(timerIndex, timerCommand, arg);

        if (timerIndex == 1 && timerCommand == MessageParams.Timer_Resume)
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
                        if (_gameViewModel.DialogMode == DialogModes.ChangeSum || _gameViewModel.DialogMode == DialogModes.None)
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
        // We do not start from 0 and increment it to avoid issues when the value could accidentally reach zero
        // if the work is processed faster than it is queued
        var fileCounter = mparams.Length - 1;

        for (var i = 1; i < mparams.Length; i++)
        {
            var uri = mparams[i];

            if (uri.Contains(Constants.GameHost))
            {
                if (!string.IsNullOrWhiteSpace(_serverAddress))
                {
                    if (Uri.TryCreate(_serverAddress, UriKind.Absolute, out var hostUri))
                    {
                        uri = uri.Replace(Constants.GameHost, hostUri.Host);
                    }
                }
            }
            else if (uri.Contains(Constants.ServerHost))
            {
                uri = uri.Replace(Constants.ServerHost, _serverPublicUrl);
            }
            else if (!uri.StartsWith("http://localhost") && !_data.Host.LoadExternalMedia && !ExternalUrlOk(uri))
            {
                Interlocked.Decrement(ref fileCounter);
                continue;
            }

            if (!Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out var mediaUri))
            {
                Interlocked.Decrement(ref fileCounter);
                continue;
            }
            
            _localFileManager.AddFile(mediaUri, () =>
            {
                if (Interlocked.Decrement(ref fileCounter) == 0)
                {
                    _viewerActions.SendMessage(Messages.MediaPreloaded);
                }
            });
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

    public void OnBanned(Models.BannedInfo bannedInfo) =>
        UI.Execute(
            () =>
            {
                ClientData.Banned.Add(bannedInfo);
            },
            exc => ClientData.Host.OnError(exc));

    public void OnBannedList(IEnumerable<Models.BannedInfo> banned) =>
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

        _gameViewModel.GameMetadata = gameInfo.ToString();

        if (!string.IsNullOrEmpty(voiceChatUri) && Uri.IsWellFormedUriString(voiceChatUri, UriKind.Absolute))
        {
            ClientData.VoiceChatUri = voiceChatUri;
        }
    }

    public void AddPlayer(PlayerAccount player) => UI.Execute(
        () =>
        {
            _gameViewModel.Players.Add(player);
            _gameViewModel.UpdateAddTableCommand();
        },
        ClientData.Host.OnError);

    public void RemovePlayerAt(int index) => UI.Execute(
        () =>
        {
            _gameViewModel.Players.RemoveAt(index);
            _gameViewModel.UpdateAddTableCommand();
        },
        ClientData.Host.OnError);

    public void OnInfo()
    {
        UI.Execute(
            () =>
            {
                _gameViewModel.Players.Clear();

                foreach (var player in ClientData.Players)
                {
                    _gameViewModel.Players.Add(player);
                }

                _gameViewModel.UpdateAddTableCommand();
            },
            ClientData.Host.OnError);

        if (_gameViewModel.Host?.Role == GameRole.Player)
        {
            Greet();
        }

        if (!_data.IsNetworkGame && _gameViewModel.Ready.CanBeExecuted)
        {
            _gameViewModel.Ready.Execute(null);
        }
    }

    private async void Greet()
    {
        try
        {
            await Task.Delay(2000);

            _gameViewModel.OnAddString(null, string.Format(_viewerActions.LO[nameof(R.Hint)], _data.Host.GameButtonKey), LogMode.Log);
            _gameViewModel.OnAddString(null, _viewerActions.LO[nameof(R.PressButton)] + Environment.NewLine, LogMode.Log);
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception exc)
        {
            Trace.TraceError("Greet error: " + exc);
        }
    }

    public void OnAnswerOptions(bool questionHasScreenContent, IEnumerable<string> optionsTypes)
    {
        TInfo.AnswerOptions.Options = optionsTypes.Select(i => new ItemViewModel()).ToArray();
        TInfo.LayoutMode = LayoutMode.AnswerOptions;
        TInfo.TStage = TableStage.Question;
        TInfo.Text = "";
    }

    public void SelectQuestion()
    {
        lock (_data.ChoiceLock)
        {
            _data.ThemeIndex = _data.QuestionIndex = -1;
        }

        _gameViewModel.Hint = Resources.HintSelectQuestion;
        TInfo.Selectable = true;
        TInfo.SelectQuestion.CanBeExecuted = true;
        _data.Host.OnFlash();
    }

    public void OnEnableButton() => _gameViewModel.EnableGameButton(true);

    public void OnDisableButton() => _gameViewModel.DisableGameButton(true);

    public void IsRight(string name, bool voteForRight, string answer)
    {
        _gameViewModel.Hint = Resources.HintCheckAnswer;
        _gameViewModel.NewValidation = false;
        _gameViewModel.DialogMode = DialogModes.AnswerValidation;
        _gameViewModel.AddValidation(name, answer);
        _data.Host.OnFlash();
    }

    public void ValidateAnswer(int playerIndex, string answer)
    {
        if (playerIndex < 0 || playerIndex >= _data.Players.Count)
        {
            return;
        }

        var playerName = _data.Players[playerIndex].Name;
        _gameViewModel.NewValidation = true;
        ClientData.PersonDataExtensions.ShowExtraRightButtons = false;
        _gameViewModel.Hint = Resources.HintCheckAnswer;
        _gameViewModel.AddValidation(playerName, answer);
        _gameViewModel.DialogMode = DialogModes.AnswerValidation;
        _data.Host.OnFlash();
    }

    public void Answer()
    {
        _data.Host.OnFlash();

        if (TInfo.LayoutMode == LayoutMode.Simple)
        {
            _gameViewModel.Answer = "";
            _gameViewModel.DialogMode = DialogModes.Answer;
            ((PlayerAccount)_data.Me).IsDeciding = false;

            StartSendingVersion(_cancellationTokenSource.Token);
        }
        else
        {
            TInfo.Selectable = true;
            TInfo.SelectAnswer.CanBeExecuted = true;
        }
    }

    /// <summary>
    /// Periodically sends player answer to server.
    /// </summary>
    private async void StartSendingVersion(CancellationToken cancellationToken)
    {
        try
        {
            var version = _gameViewModel.Answer;

            do
            {
                await Task.Delay(3000, cancellationToken);

                if (_gameViewModel.Answer != version)
                {
                    _gameViewModel.SendAnswerVersion.Execute(null);
                    version = _gameViewModel.Answer;
                }
            } while (_gameViewModel.DialogMode == DialogModes.Answer && !cancellationToken.IsCancellationRequested);
        }
        catch
        {
            // Ignore
        }
    }

    public void OnPlayerOutcome(int playerIndex, bool isRight)
    {
        if (_data.QuestionType != QuestionTypes.Simple
            && _data.Players[playerIndex].Name == _viewerActions.Client.Name
            || isRight)
        {
            _gameViewModel.Apellate.CanBeExecuted = _gameViewModel.ApellationCount > 0;
        }
    }

    public void OnHint(string hint) => _gameViewModel.Hint = $"{_localizer[nameof(R.RightAnswer)].ToUpperInvariant()} : {hint}";

    public void EndThink()
    {
        _gameViewModel.Apellate.CanBeExecuted = _gameViewModel.ApellationCount > 0;
    }

    public void Report(string report)
    {
        if (!_data.Host.SendReport)
        {
            _gameViewModel.Report.SendNoReport.Execute(null);
            return;
        }

        _gameViewModel.Report.Report = report;
        _gameViewModel.DialogMode = DialogModes.Report;
        _data.Host.OnFlash();
    }

    public void OnGameClosed()
    {
        PlatformManager.Instance.ShowMessage(Resources.GameClosedMessage, MessageType.Warning, true);
        OnReplic(ReplicCodes.Special.ToString(), Resources.GameClosedMessage);
    }

    public void OnCanPressButton()
    {
        _gameViewModel.Apellate.CanBeExecuted = false;
    }

    public void OnQuestionSelected()
    {
        _gameViewModel.Apellate.CanBeExecuted = false;
    }

    public void OnPersonConnected() => _gameViewModel.UpdateCommands();

    public void OnPersonDisconnected() => _gameViewModel.UpdateCommands();

    public void OnHostChanged() => _gameViewModel.UpdateCommands();

    public void OnClientSwitch(IViewerClient viewer)
    {
        _gameViewModel.Host = viewer;
        _gameViewModel.UpdateCommands();
    }

    public void DeleteTheme()
    {
        _data.ThemeIndex = -1;

        TInfo.Selectable = true;
        TInfo.SelectTheme.CanBeExecuted = true;
        _gameViewModel.Hint = Resources.HintSelectTheme;

        _data.Host.OnFlash();
    }

    public void MakeStake()
    {
        _gameViewModel.SendStakeNew.CanBeExecuted = _data.PersonDataExtensions.Var[1];
        _gameViewModel.SendPassNew.CanBeExecuted = _data.PersonDataExtensions.Var[2];
        _gameViewModel.SendAllInNew.CanBeExecuted = _data.PersonDataExtensions.Var[3];

        _gameViewModel.DialogMode = DialogModes.StakeNew;
        _gameViewModel.Hint = _viewerActions.LO[nameof(R.HintMakeAStake)];
        _data.Host.OnFlash();

        foreach (var player in _data.Players)
        {
            player.IsDeciding = false;
        }
    }
}
