using SIEngine.Rules;
using SImulator.ViewModel.Contracts;
using SImulator.ViewModel.Core;
using SImulator.ViewModel.Model;
using SImulator.ViewModel.PlatformSpecific;
using SImulator.ViewModel.Properties;
using SIPackages;
using SIPackages.Core;
using SIUI.ViewModel;
using SIUI.ViewModel.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Utils;
using Utils.Commands;
using Utils.Timers;

namespace SImulator.ViewModel.Controllers;

/// <inheritdoc cref="IPresentationController" />
public sealed class PresentationController : IPresentationController, INotifyPropertyChanged
{
    /// <summary>
    /// Minimum weight for the small content.
    /// </summary>
    private const double SmallContentWeight = 1.0;

    /// <summary>
    /// Length of text having weight of 1.
    /// </summary>
    private const int TextLengthWithBasicWeight = 80;
    
    /// <summary>
    /// Relative media content group weight on screen.
    /// </summary>
    private const double MediaContentGroupWeight = 5.0;

    private int _previousCode = -1;

    private readonly IAnimatableTimer _animatableTimer = PlatformManager.Instance.CreateAnimatableTimer();

    public bool CanControlMedia => true;

    private IPresentationListener? _listener;

    internal IPresentationListener? Listener
    {
        get => _listener;
        set
        {
            _listener = value;
        }
    }

    public TableInfoViewModel TInfo { get; private set; }

    /// <summary>
    /// Game players.
    /// </summary>
    public IList<SimplePlayerInfo> Players { get; private set; } = new ObservableCollection<SimplePlayerInfo>();

    public ICommand Next { get; private set; }

    public ICommand Back { get; private set; }

    public ICommand NextRound { get; private set; }

    public ICommand BackRound { get; private set; }

    public ICommand Stop { get; private set; }

    private bool _stageCallbackBlock = false;

    public IDisplayDescriptor Screen { get; }

    public event Action<Exception>? Error;

    private bool _showPlayers = false;

    /// <summary>
    /// Show players and scores.
    /// </summary>
    [DefaultValue(false)]
    public bool ShowPlayers
    {
        get => _showPlayers;
        set { if (_showPlayers != value) { _showPlayers = value; OnPropertyChanged(); } }
    }

    public Action<int, int>? SelectionCallback { get; set; }

    public Action<int>? DeletionCallback { get; set; }

    private bool _isSoundEnabled;

    private readonly SoundsSettings _soundsSettings;

    public PresentationController(IDisplayDescriptor screen, SoundsSettings soundSettings)
    {
        _soundsSettings = soundSettings;
        Screen = screen;

        TInfo = new TableInfoViewModel
        {
            Enabled = true,
            TStage = TableStage.Sign
        };

        TInfo.PropertyChanged += TInfo_PropertyChanged;
        TInfo.QuestionSelected += QuestionInfo_Selected;
        TInfo.ThemeSelected += ThemeInfo_Selected;
        TInfo.AnswerSelected += TInfo_AnswerSelected;

        TInfo.MediaStart += () =>
        {
            _listener?.OnMediaStart();
        };

        TInfo.MediaEnd += () =>
        {
            try
            {
                _listener?.OnMediaEnd();
            }
            catch (ObjectDisposedException)
            {

            }
        };

        TInfo.MediaProgress += progress =>
        {
            _listener?.OnMediaProgress(progress);
        };

        Next = new SimpleCommand(arg => _listener?.AskNext());
        Back = new SimpleCommand(arg => _listener?.AskBack());
        NextRound = new SimpleCommand(arg => _listener?.AskNextRound());
        BackRound = new SimpleCommand(arg => _listener?.AskBackRound());

        Stop = new SimpleCommand(arg =>
        {
            if (_listener != null)
            {
                UI.Execute(_listener.AskStop, OnError);
            }
        });

        _animatableTimer.TimeChanged += AnimatableTimer_TimeChanged;
    }

    public void SetAppSound(bool isEnabled) => _isSoundEnabled = isEnabled;

    private void AnimatableTimer_TimeChanged(IAnimatableTimer timer) =>
        TInfo.TimeLeft = timer.Time < 0.001 ? 0.0 : 1.0 - timer.Time / 100;

    private void TInfo_AnswerSelected(ItemViewModel answer)
    {
        int answerIndex;

        for (answerIndex = 0; answerIndex < TInfo.AnswerOptions.Options.Length; answerIndex++)
        {
            if (TInfo.AnswerOptions.Options[answerIndex] == answer)
            {
                break;
            }
        }

        Listener?.OnAnswerSelected(answerIndex);
    }

    private void TInfo_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TableInfoViewModel.TStage))
        {
            if (TInfo.TStage == TableStage.RoundTable)
            {
                SetSound();

                if (Listener != null && !_stageCallbackBlock)
                {
                    Listener.OnRoundThemesFinished();
                }
            }
        }
    }

    public void SetSound(string sound = "")
    {
        if (!_isSoundEnabled)
        {
            return;
        }

        UI.Execute(() => PlatformManager.Instance.PlaySound(sound), OnError);
    }

    public async Task StartAsync(Action onLoad)
    {
        await PlatformManager.Instance.CreateMainViewAsync(this, Screen);
        TInfo.TStage = TableStage.Sign;
        onLoad();
    }

    public async Task StopAsync()
    {
        await PlatformManager.Instance.CloseMainViewAsync();

        lock (TInfo.TStageLock)
        {
            TInfo.TStage = TableStage.Void;
        }

        SetSound();
        Listener = null;
    }

    private void SetMedia(MediaSource media, bool background)
    {
        if (background)
        {
            TInfo.SoundSource = media;
        }
        else
        {
            TInfo.MediaSource = media;
        }
    }

    public void SetGameThemes(IEnumerable<string> themes)
    {
        TInfo.GameThemes.Clear();
        TInfo.GameThemes.AddRange(themes);

        lock (TInfo.TStageLock)
        {
            TInfo.TStage = TableStage.GameThemes;
        }

        SetSound(_soundsSettings.GameThemes);
    }

    public void SetRoundTable()
    {
        _stageCallbackBlock = true;
        
        SetStage(TableStage.RoundTable);
        
        _stageCallbackBlock = false;
        _previousCode = -1;
        TInfo.QuestionStyle = QuestionStyle.Normal;
    }

    public void SetStage(TableStage stage)
    {
        lock (TInfo.TStageLock)
        {
            TInfo.TStage = stage;
        }
    }

    public void SetText(string text = "") => TInfo.Text = text;

    private void SetScreenContent(IReadOnlyCollection<ContentGroup> content)
    {
        TInfo.TStage = TableStage.Question;
        TInfo.Content = content;
        SetQuestionContentType(QuestionContentType.Collection);
    }

    public void SetQuestionContentType(QuestionContentType questionContentType)
    {
        try
        {
            TInfo.QuestionContentType = questionContentType;
        }
        catch (NotImplementedException exc) when (exc.Message.Contains("The Source property cannot be set to null"))
        {
            // https://github.com/MicrosoftEdge/WebView2Feedback/issues/1136
            return;
        }
    }

    public void SetQuestionStyle(QuestionStyle questionStyle) => TInfo.QuestionStyle = questionStyle;

    public void OnContentStart()
    {
        SetQuestionSound(false);
        SetQuestionContentType(QuestionContentType.Void);
        SetStage(TableStage.Question);
        SetSound();
    }

    public void SetQuestionSound(bool sound)
    {
        TInfo.Sound = sound;

        TInfo.QuestionContentType = TInfo.QuestionContentType == QuestionContentType.Void
            ? QuestionContentType.Clef
            : TInfo.QuestionContentType;
    }

    public void AddPlayer(string playerName) => Players.Add(new SimplePlayerInfo { Name = playerName });

    public void RemovePlayer(int playerIndex)
    {
        var player = Players[playerIndex];

        if (player != null)
        {
            Players.Remove(player);
        }
    }

    public void ClearPlayers()
    {
        Players.Clear();
    }

    public void UpdatePlayerInfo(int index, PlayerInfo player, string? propertyName = null)
    {
        if (index <= -1 || index >= Players.Count)
        {
            return;
        }

        var p = Players[index];
        
        p.Sum = player.Sum;
        p.Name = player.Name;
        p.State = player.State;
    }

    public void SetRoundThemes(string[] themes, bool isFinal)
    {
        TInfo.RoundInfo.Clear();

        foreach (var theme in themes)
        {
            TInfo.RoundInfo.Add(new ThemeInfoViewModel { Name = theme });
        }

        lock (TInfo.TStageLock)
        {
            TInfo.TStage = isFinal ? TableStage.Final : TableStage.RoundThemes;
        }

        if (!isFinal)
        {
            SetSound(_soundsSettings.RoundThemes);
        }
    }

    public void SetTable(ThemeInfoViewModel[] table)
    {
        TInfo.RoundInfo.Clear();

        foreach (var theme in table)
        {
            TInfo.RoundInfo.Add(theme);
        }
    }

    private void ThemeInfo_Selected(ThemeInfoViewModel theme)
    {
        int themeIndex;

        for (themeIndex = 0; themeIndex < TInfo.RoundInfo.Count; themeIndex++)
        {
            if (TInfo.RoundInfo[themeIndex] == theme)
            {
                break;
            }
        }

        DeletionCallback?.Invoke(themeIndex);
    }

    private void QuestionInfo_Selected(QuestionInfoViewModel question)
    {
        lock (TInfo.TStageLock)
        {
            if (TInfo.TStage != TableStage.RoundTable)
            {
                return;
            }
        }

        int questionIndex = -1;
        int themeIndex;

        for (themeIndex = 0; themeIndex < TInfo.RoundInfo.Count; themeIndex++)
        {
            var found = false;
            var theme = TInfo.RoundInfo[themeIndex];

            for (questionIndex = 0; questionIndex < theme.Questions.Count; questionIndex++)
            {
                if (theme.Questions[questionIndex] == question)
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                break;
            }
        }

        SelectionCallback?.Invoke(themeIndex, questionIndex);
    }

    public void PlaySimpleSelection(int theme, int quest) => TInfo.PlaySimpleSelectionAsync(theme, quest);

    public void PlaySelection(int theme)
    {
        TInfo.PlaySelection(theme);
        SetSound(_soundsSettings.FinalDelete);
    }

    public void UpdateSettings(Settings settings) => TInfo.Settings.Initialize(settings);

    public void UpdateShowPlayers(bool showPlayers) => ShowPlayers = showPlayers;

    public bool OnKeyPressed(GameKey key)
    {
        lock (TInfo.TStageLock)
        {
            switch (TInfo.TStage)
            {
                case TableStage.RoundTable:
                    if (TInfo.Settings.Model.KeyboardControl)
                    {
                        if (_previousCode > -1 && PlatformManager.Instance.IsEscapeKey(key))
                        {
                            _previousCode = -1;
                            return true;
                        }

                        var code = PlatformManager.Instance.GetKeyNumber(key);

                        if (code == -1)
                        {
                            _previousCode = -1;
                            return false;
                        }

                        if (_previousCode == -1)
                        {
                            _previousCode = code;
                            return true;
                        }
                        else
                        {
                            if (_previousCode < TInfo.RoundInfo.Count
                                && code < TInfo.RoundInfo[_previousCode].Questions.Count
                                && TInfo.RoundInfo[_previousCode].Questions[code].Price > -1)
                            {
                                SelectionCallback?.Invoke(_previousCode, code);
                                _previousCode = -1;
                                return true;
                            }

                            _previousCode = -1;
                        }

                    }

                    break;

                case TableStage.Final:
                    if (TInfo.Settings.Model.KeyboardControl)
                    {
                        var code = PlatformManager.Instance.GetKeyNumber(key);
                        if (code == -1)
                        {
                            return false;
                        }

                        if (code < TInfo.RoundInfo.Count && TInfo.RoundInfo[code].Name != null)
                        {
                            DeletionCallback?.Invoke(code);
                            return true;
                        }
                    }

                    break;
            }
        }
        return false;
    }

    public void SetActivePlayerIndex(int playerIndex)
    {
        for (var i = 0; i < Players.Count; i++)
        {
            Players[i].State = i == playerIndex ? PlayerState.Active : PlayerState.None;
        }
    }

    public void AddLostButtonPlayerIndex(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= Players.Count)
        {
            return;
        }

        if (!ShowPlayers)
        {
            var playerName = Players[playerIndex].Name;

            lock (TInfo.LostButtonPlayers)
            {
                if (!TInfo.LostButtonPlayers.Contains(playerName))
                {
                    TInfo.LostButtonPlayers.Add(playerName);
                }
            }
        }

        var currentCountedPlayers = Players.Where(p => p.State == PlayerState.Active || p.LostButtonIndex > -1).Count();

        if (Players[playerIndex].LostButtonIndex == -1)
        {
            Players[playerIndex].LostButtonIndex = currentCountedPlayers + 1;
            Players[playerIndex].State = PlayerState.LostButton;
        }
    }

    public void ClearPlayersState()
    {
        if (!ShowPlayers)
        {
            lock (TInfo.LostButtonPlayers)
            {
                TInfo.LostButtonPlayers.Clear();
            }
        }

        for (var i = 0; i < Players.Count; i++)
        {
            Players[i].LostButtonIndex = -1;
            Players[i].State = PlayerState.None;
        }
    }

    public void SeekMedia(int position) => UI.Execute(() => TInfo.OnMediaSeek(position), OnError);

    private void OnError(Exception exc) => Error?.Invoke(exc);

    public void ResumeMedia() => TInfo.OnMediaResume();

    public void StopMedia() => TInfo.OnMediaPause();

    public void RestoreQuestion(int themeIndex, int questionIndex, int price) => TInfo.RoundInfo[themeIndex].Questions[questionIndex].Price = price;

    public void SetCaption(string caption) => TInfo.Caption = caption;

    public void SetTimerMaxTime(int maxTime) => _animatableTimer.MaxTime = maxTime;

    public void RunTimer() => _animatableTimer.Run(-1, false);

    public void PauseTimer(int currentTime) => _animatableTimer.Pause(currentTime, false);

    public void StopTimer() => _animatableTimer.Stop();

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public void SetAnswerOptions(ItemViewModel[] answerOptions)
    {
        TInfo.LayoutMode = LayoutMode.AnswerOptions;
        TInfo.AnswerOptions.Options = answerOptions;
    }

    public async void ShowAnswerOptions()
    {
        try
        {
            for (var i = 0; i < TInfo.AnswerOptions.Options.Length; i++)
            {
                TInfo.AnswerOptions.Options[i].IsVisible = true;
                await Task.Delay(1000);
            }
        }
        catch (Exception exc)
        {
            Trace.TraceError("ShowAnswerOptions error: " + exc.Message);
        }
    }

    public void SetAnswerState(int answerIndex, ItemState state)
    {
        var answerOptions = TInfo.AnswerOptions.Options;

        if (answerIndex < 0 || answerIndex >= answerOptions.Length)
        {
            return;
        }

        if (state == ItemState.Active || state == ItemState.Right)
        {
            for (var i = 0; i < answerOptions.Length; i++)
            {
                if (i != answerIndex && answerOptions[i].State == ItemState.Active)
                {
                    answerOptions[i].State = ItemState.Normal;
                }
            }
        }

        answerOptions[answerIndex].State = state;
    }

    public void OnQuestionStart() => TInfo.LayoutMode = LayoutMode.Simple;

    public void Dispose()
    {
        SetSound();
        _animatableTimer.Dispose();
    }

    public void SetRound(string roundName, QuestionSelectionStrategyType selectionStrategyType)
    {
        SetText(roundName);
        SetStage(TableStage.Round);
        SetSound(_soundsSettings.RoundBegin);
    }

    public void SetTheme(string themeName, bool animate)
    {
        SetSound();
        SetText($"{Resources.Theme}: {themeName}");
        SetStage(TableStage.Theme);
    }

    public void SetQuestionPrice(int questionPrice)
    {
        SetText(questionPrice.ToString());
        SetStage(TableStage.QuestionPrice);
    }

    public bool OnQuestionContent(
        IReadOnlyCollection<ContentItem> content,
        Func<ContentItem, string?> tryGetMediaUri,
        string? textToShow)
    {
        var hasMedia = false;

        var screenContent = new List<ContentGroup>();
        ContentGroup? currentGroup = null;

        foreach (var contentItem in content)
        {
            switch (contentItem.Placement)
            {
                case ContentPlacements.Screen:
                    switch (contentItem.Type)
                    {
                        case ContentTypes.Text:
                            if (currentGroup != null)
                            {
                                currentGroup.Init();
                                screenContent.Add(currentGroup);
                                currentGroup = null;
                            }

                            // Show theme name and question price instead of empty text
                            var displayedText = textToShow ?? contentItem.Value;
                            SetText(displayedText); // For simple answer

                            var groupWeight = Math.Max(
                                SmallContentWeight,
                                Math.Min(MediaContentGroupWeight, (double)displayedText.Length / TextLengthWithBasicWeight));

                            var group = new ContentGroup { Weight = groupWeight };
                            group.Content.Add(new ContentViewModel(ContentType.Text, displayedText));
                            screenContent.Add(group);
                            break;

                        case ContentTypes.Image:
                            currentGroup ??= new ContentGroup { Weight = MediaContentGroupWeight };
                            var imageUri = tryGetMediaUri(contentItem);

                            if (imageUri != null)
                            {
                                currentGroup.Content.Add(new ContentViewModel(ContentType.Image, imageUri));
                            }
                            else
                            {
                                currentGroup.Content.Add(new ContentViewModel(ContentType.Void, ""));
                            }
                            break;

                        case ContentTypes.Video:
                            currentGroup ??= new ContentGroup { Weight = MediaContentGroupWeight };
                            var videoUri = tryGetMediaUri(contentItem);

                            if (videoUri != null)
                            {
                                currentGroup.Content.Add(new ContentViewModel(ContentType.Video, videoUri));
                                SetSound();
                                hasMedia = true;
                            }
                            else
                            {
                                currentGroup.Content.Add(new ContentViewModel(ContentType.Void, ""));
                            }
                            break;

                        case ContentTypes.Html:
                            currentGroup ??= new ContentGroup { Weight = MediaContentGroupWeight };
                            var htmlUri = tryGetMediaUri(contentItem);

                            if (htmlUri != null)
                            {
                                currentGroup.Content.Add(new ContentViewModel(ContentType.Html, htmlUri));
                                SetQuestionSound(false);
                                SetSound();
                            }
                            else
                            {
                                currentGroup.Content.Add(new ContentViewModel(ContentType.Void, ""));
                            }
                            break;

                        default:
                            break;
                    }
                    break;

                case ContentPlacements.Replic:
                    if (contentItem.Type == ContentTypes.Text)
                    {
                        // Show nothing. The text should be read by showman
                    }
                    break;

                case ContentPlacements.Background:
                    if (contentItem.Type == ContentTypes.Audio)
                    {
                        SetQuestionSound(true);

                        var audioUri = tryGetMediaUri(contentItem);

                        SetSound();

                        if (audioUri != null)
                        {
                            SetMedia(new MediaSource(audioUri), true);
                            hasMedia = true;
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        if (currentGroup != null)
        {
            currentGroup.Init();
            screenContent.Add(currentGroup);
        }

        if (screenContent.Any())
        {
            SetScreenContent(screenContent);
        }

        return hasMedia;
    }

    public void SetQuestionType(string typeName, string aliasName, int activeThemeIndex)
    {
        SetText(aliasName);

        for (var k = 0; k < TInfo.RoundInfo.Count; k++)
        {
            TInfo.RoundInfo[k].Active = k == activeThemeIndex;
        }

        SetStage(TableStage.Special);

        switch (typeName)
        {
            case QuestionTypes.Secret:
            case QuestionTypes.SecretPublicPrice:
            case QuestionTypes.SecretNoQuestion:
                SetSound(_soundsSettings.SecretQuestion);
                break;

            case QuestionTypes.Stake:
                SetSound(_soundsSettings.StakeQuestion);
                break;

            case QuestionTypes.NoRisk:
                SetSound(_soundsSettings.NoRiskQuestion);
                break;
        }
    }

    public void SetSimpleAnswer()
    {
        SetQuestionSound(false);
        SetQuestionContentType(QuestionContentType.Void);
        SetStage(TableStage.Answer);
        SetSound();
    }

    public void OnAnswerStart() => SetSound();

    public void ClearState() => SetStage(TableStage.Sign);

    public void OnPackage(string packageName, MediaInfo? packageLogo)
    {
        SetSound(_soundsSettings.BeginGame);
        
        if (!packageLogo.HasValue || packageLogo.Value.Uri == null)
        {
            return;
        }

        SetMedia(new MediaSource(packageLogo.Value.Uri.OriginalString), false);
        SetStage(TableStage.Question);
        SetQuestionSound(false);
        SetQuestionContentType(QuestionContentType.Image);
    }

    public void FinishQuestion()
    {
        SetText();
        SetActivePlayerIndex(-1);
    }

    public void NoAnswer() => SetSound(_soundsSettings.NoAnswer);

    public void PlayerIsRight(int playerIndex) => SetSound(_soundsSettings.AnswerRight);

    public void PlayerIsWrong(int playerIndex) => SetSound(_soundsSettings.AnswerWrong);

    public void BeginPressButton() => SetQuestionStyle(QuestionStyle.WaitingForPress);

    public void OnFinalThink() => SetSound(_soundsSettings.FinalThink);

    public event PropertyChangedEventHandler? PropertyChanged;
}
