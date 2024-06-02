using SIEngine;
using SIEngine.Rules;
using SImulator.ViewModel.ButtonManagers;
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
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Utils;
using Utils.Commands;
using Utils.Timers;

namespace SImulator.ViewModel;

/// <summary>
/// Controls a single game run.
/// </summary>
public sealed class GameViewModel : ITaskRunHandler<Tasks>, INotifyPropertyChanged, IButtonManagerListener, IAsyncDisposable
{
    #region Fields
    private bool _isDisposed = false;

    internal event Action<string>? Error;
    internal event Action? RequestStop;

    private readonly Stack<Tuple<PlayerInfo, int, bool>?> _answeringHistory = new();

    private readonly EngineBase _engine;

    /// <summary>
    /// Game buttons manager.
    /// </summary>
    private IButtonManager? _buttonManager;

    /// <summary>
    /// Game log writer.
    /// </summary>
    private readonly IGameLogger _gameLogger;

    private readonly Timer _roundTimer;
    private readonly Timer _questionTimer;
    private readonly Timer _thinkingTimer;

    private bool _timerStopped;
    private bool _mediaStopped;

    private QuestionState _state = QuestionState.Normal;
    private QuestionState _previousState;

    internal QuestionState State
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                _state = value;
                OnStateChanged();
            }
        }
    }

    private PlayerInfo? _chooser = null;
    private PlayerInfo? _selectedPlayer = null;

    private int _chooserIndex = -1;

    /// <summary>
    /// Index of player having turn.
    /// </summary>
    public int ChooserIndex
    {
        get => _chooserIndex;
        set
        {
            if (_chooserIndex != value)
            {
                _chooserIndex = value;
                _chooser = _chooserIndex >= 0 && _chooserIndex < Players.Count ? Players[_chooserIndex] : null;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Chooser));
            }
        }
    }

    /// <summary>
    /// Player that has turn.
    /// </summary>
    public PlayerInfo? Chooser
    {
        get => _chooser;
        set
        {
            if (_chooser != value)
            {
                _chooser = value;
                _chooserIndex = value != null ? Players.IndexOf(value) : -1;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ChooserIndex));
            }
        }
    }

    private readonly List<PlayerInfo> _selectedPlayers = new();
    private readonly Dictionary<string, PlayerInfo> _playersTable = new();

    #endregion

    #region Commands

    private readonly SimpleCommand _stop;
    private readonly SimpleUICommand _next;
    private readonly SimpleCommand _back;

    private readonly SimpleCommand _addRight;
    private readonly SimpleCommand _addWrong;

    private readonly SimpleCommand _nextRound;
    private readonly SimpleCommand _previousRound;

    public ICommand Stop => _stop;
    public ICommand Next => _next;
    public ICommand Back => _back;

    public ICommand RunRoundTimer { get; private set; }
    public ICommand StopRoundTimer { get; private set; }

    public ICommand RunQuestionTimer { get; private set; }
    public ICommand StopQuestionTimer { get; private set; }

    public ICommand RunMediaTimer { get; private set; }
    public ICommand StopMediaTimer { get; private set; }

    public ICommand AddPlayer { get; private set; }
    public ICommand RemovePlayer { get; private set; }
    public ICommand ClearPlayers { get; private set; }

    public ICommand ResetSums { get; private set; }

    public ICommand GiveTurn { get; private set; }

    public SimpleCommand Pass { get; private set; }

    public ICommand MakeStake { get; private set; }

    public ICommand AddRight => _addRight;
    public ICommand AddWrong => _addWrong;

    public ICommand NextRound => _nextRound;

    public ICommand PreviousRound => _previousRound;

    private ICommand? _activeRoundCommand;

    public ICommand? ActiveRoundCommand
    {
        get => _activeRoundCommand;
        set { _activeRoundCommand = value; OnPropertyChanged(); }
    }

    private ICommand? _activeQuestionCommand;

    public ICommand? ActiveQuestionCommand
    {
        get => _activeQuestionCommand;
        set { _activeQuestionCommand = value; OnPropertyChanged(); }
    }

    private ICommand? _activeMediaCommand;

    public ICommand? ActiveMediaCommand
    {
        get => _activeMediaCommand;
        set { if (_activeMediaCommand != value) { _activeMediaCommand = value; OnPropertyChanged(); } }
    }

    #endregion

    #region Properties

    public AppSettingsViewModel Settings { get; }

    /// <summary>
    /// Presentation link.
    /// </summary>
    public IPresentationController PresentationController { get; }

    /// <summary>
    /// Table info view model.
    /// </summary>
    public TableInfoViewModel LocalInfo { get; set; }

    /// <summary>
    /// Game players.
    /// </summary>
    public IList<PlayerInfo> Players { get; }

    private Func<bool>? _continuation = null;

    public Func<bool>? Continuation
    {
        set
        {
            if (_continuation != value)
            {
                _continuation = value;
                UpdateNextCommand();
            }
        }
    }

    private int _price;

    public int Price
    {
        get => _price;
        set 
        {
            if (_price != value)
            {
                _price = value;
                OnPropertyChanged();
                UpdateCaption();
            }
        }
    }

    /// <summary>
    /// Amount to subtract on player wrong answer.
    /// </summary>
    public int? NegativePrice { get; private set; } = null;

    private int _rountTime = 0;

    public int RoundTime
    {
        get => _rountTime;
        set { _rountTime = value; OnPropertyChanged(); }
    }

    private int _questionTime = 0;

    /// <summary>
    /// Current question time value.
    /// </summary>
    public int QuestionTime
    {
        get => _questionTime;
        set { _questionTime = value; OnPropertyChanged(); }
    }

    private int _questionTimeMax = int.MaxValue;

    /// <summary>
    /// Maximum question time value.
    /// </summary>
    public int QuestionTimeMax
    {
        get => _questionTimeMax;
        set { _questionTimeMax = value; OnPropertyChanged(); }
    }

    private int _thinkingTime = 0;

    /// <summary>
    /// Current thinking time value.
    /// </summary>
    public int ThinkingTime
    {
        get => _thinkingTime;
        set { _thinkingTime = value; OnPropertyChanged(); }
    }

    private int _thinkingTimeMax = int.MaxValue;

    /// <summary>
    /// Maximum thinking time value.
    /// </summary>
    public int ThinkingTimeMax
    {
        get => _thinkingTimeMax;
        set { _thinkingTimeMax = value; OnPropertyChanged(); }
    }

    private Round? _activeRound;

    public Round ActiveRound => _activeRound ?? throw new InvalidOperationException("Active round is undefined");

    private Question? _activeQuestion;

    public Question? ActiveQuestion
    {
        get => _activeQuestion;
        set { _activeQuestion = value; OnPropertyChanged(); }
    }

    private Theme? _activeTheme;

    public Theme? ActiveTheme
    {
        get => _activeTheme;
        set { _activeTheme = value; OnPropertyChanged(); }
    }

    private IEnumerable<ContentItem>? _contentItems = null;

    /// <summary>
    /// Currently played content items.
    /// </summary>
    public IEnumerable<ContentItem>? ContentItems
    {
        get => _contentItems;
        set { _contentItems = value; OnPropertyChanged(); }
    }

    private IReadOnlyCollection<ContentItem> _activeContent = Array.Empty<ContentItem>();

    /// <summary>
    /// Currently active content items.
    /// </summary>
    public IReadOnlyCollection<ContentItem> ActiveContent
    {
        get => _activeContent;
        set { if (_activeContent != value) { _activeContent = value; OnPropertyChanged(); } }
    }

    private int _mediaProgress;

    private bool _mediaProgressBlock = false;

    public int MediaProgress
    {
        get => _mediaProgress;
        set
        {
            if (_mediaProgress != value)
            {
                _mediaProgress = value;
                OnPropertyChanged();

                if (!_mediaProgressBlock)
                {
                    PresentationController.SeekMedia(_mediaProgress);

                    if (_presentationListener.IsMediaEnded)
                    {
                        ActiveMediaCommand = StopMediaTimer;
                        _presentationListener.IsMediaEnded = false;
                    }
                }
            }
        }
    }

    private bool _isMediaControlled;

    public bool IsMediaControlled
    {
        get => _isMediaControlled;
        set
        {
            if (_isMediaControlled != value)
            {
                _isMediaControlled = value;
                OnPropertyChanged();
            }
        }
    }

    public int ButtonBlockTime => (int)(Settings.Model.BlockingTime * 1000);

    private readonly IExtendedListener _presentationListener;

    private int _selectedAnswerIndex = -1;

    private DecisionMode _decisionMode = DecisionMode.None;

    /// <summary>
    /// Player decision mode.
    /// </summary>
    public DecisionMode DecisionMode
    {
        get => _decisionMode;
        set
        {
            if (_decisionMode != value)
            {
                _decisionMode = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _canSelectChooser;

    /// <summary>
    /// Can current chooser be selected.
    /// </summary>
    public bool CanSelectChooser
    {
        get => _canSelectChooser;
        set
        {
            if (_canSelectChooser != value)
            {
                _canSelectChooser = value;
                OnPropertyChanged();
            }
        }
    }

    private NumberSet? _stakeInfo;

    /// <summary>
    /// Player possible stakes info.
    /// </summary>
    public NumberSet? StakeInfo
    {
        get => _stakeInfo;
        set 
        {
            if (_stakeInfo != value)
            {
                _stakeInfo = value;
                OnPropertyChanged();
            }
        }
    }

    private readonly List<int> _stakers = new();
    private int _stakersIterator = -1;
    private int _stakerIndex = -1;

    public int StakerIndex
    {
        get => _stakerIndex;
        set
        {
            _stakerIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Staker));
        }
    }

    public PlayerInfo? Staker => _stakerIndex > -1 && _stakerIndex < Players.Count ? Players[_stakerIndex] : null;

    private int _stake;

    public int Stake
    {
        get => _stake;
        set
        {
            if (_stake != value)
            {
                _stake = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isAllIn = false;

    private readonly TaskRunner<Tasks> _taskRunner;

    private IReadOnlyList<Theme>? _roundThemes;

    #endregion

    public GameViewModel(
        AppSettingsViewModel settings,
        ISIEngine engine,
        IExtendedListener presentationListener,
        IPresentationController presentationController,
        IList<SimplePlayerInfo> players,
        IGameLogger gameLogger)
    {
        Settings = settings;
        _engine = (EngineBase)engine;
        _presentationListener = presentationListener;
        _gameLogger = gameLogger;
        PresentationController = presentationController;
        _taskRunner = new TaskRunner<Tasks>(this);

        LocalInfo = new TableInfoViewModel();
        Players = new ObservableCollection<PlayerInfo>(players.Cast<PlayerInfo>());

        foreach (var player in Players)
        {
            player.IsRegistered = false;
            player.PropertyChanged += PlayerInfo_PropertyChanged;
        }

        LocalInfo.QuestionSelected += QuestionInfo_Selected;
        LocalInfo.ThemeSelected += ThemeInfo_Selected;
        LocalInfo.AnswerSelected += LocalInfo_AnswerSelected;

        _presentationListener.Next = _next = new SimpleUICommand(Next_Executed) { Name = Resources.Next };
        _presentationListener.Back = _back = new SimpleCommand(Back_Executed) { CanBeExecuted = false };
        _presentationListener.Stop = _stop = new SimpleCommand(Stop_Executed);

        AddPlayer = new SimpleCommand(AddPlayer_Executed);
        RemovePlayer = new SimpleCommand(RemovePlayer_Executed);
        ClearPlayers = new SimpleCommand(ClearPlayers_Executed);
        ResetSums = new SimpleCommand(ResetSums_Executed);
        GiveTurn = new SimpleCommand(GiveTurn_Executed);
        Pass = new SimpleCommand(Pass_Executed);
        MakeStake = new SimpleCommand(MakeStake_Executed);
        _addRight = new SimpleCommand(AddRight_Executed);
        _addWrong = new SimpleCommand(AddWrong_Executed);

        RunRoundTimer = new SimpleUICommand(RunRoundTimer_Executed) { Name = Resources.Run };
        StopRoundTimer = new SimpleUICommand(StopRoundTimer_Executed) { Name = Resources.Pause };

        RunQuestionTimer = new SimpleUICommand(RunQuestionTimer_Executed) { Name = Resources.Run };
        StopQuestionTimer = new SimpleUICommand(StopQuestionTimer_Executed) { Name = Resources.Pause };

        RunMediaTimer = new SimpleUICommand(RunMediaTimer_Executed) { Name = Resources.Run };
        StopMediaTimer = new SimpleUICommand(StopMediaTimer_Executed) { Name = Resources.Pause };

        _presentationListener.NextRound = _nextRound = new SimpleCommand(NextRound_Executed) { CanBeExecuted = false };
        _presentationListener.PreviousRound = _previousRound = new SimpleCommand(PreviousRound_Executed) { CanBeExecuted = false };

        UpdateNextCommand();

        _roundTimer = new Timer(RoundTimer_Elapsed, null, Timeout.Infinite, Timeout.Infinite);
        _questionTimer = new Timer(QuestionTimer_Elapsed, null, Timeout.Infinite, Timeout.Infinite);
        _thinkingTimer = new Timer(ThinkingTimer_Elapsed, null, Timeout.Infinite, Timeout.Infinite);

        settings.Model.SIUISettings.PropertyChanged += Default_PropertyChanged;
        settings.SIUISettings.PropertyChanged += Default_PropertyChanged;
        settings.Model.PropertyChanged += Settings_PropertyChanged;

        _engine.Package += Engine_Package;
        _engine.GameThemes += Engine_GameThemes;
        _engine.NextRound += Engine_NextRound;
        _engine.ShowScore += Engine_ShowScore;
        _engine.LogScore += LogScore;
        _engine.QuestionPostInfo += Engine_QuestionPostInfo;
        _engine.QuestionFinish += Engine_QuestionFinish;
        _engine.EndQuestion += Engine_EndQuestion;
        _engine.RoundTimeout += Engine_RoundTimeout;
        _engine.NextQuestion += Engine_NextQuestion;
        _engine.RoundEmpty += Engine_RoundEmpty;
        _engine.Error += OnError;
        _engine.EndGame += Engine_EndGame;

        _engine.PropertyChanged += Engine_PropertyChanged;

        _presentationListener.MediaStart += GameHost_MediaStart;
        _presentationListener.MediaProgress += GameHost_MediaProgress;
        _presentationListener.MediaEnd += GameHost_MediaEnd;
        _presentationListener.RoundThemesFinished += GameHost_RoundThemesFinished;
        _presentationListener.AnswerSelected += PresentationListener_AnswerSelected;
    }

    public void ExecuteTask(Tasks taskId, int arg)
    {
        switch (taskId)
        {
            case Tasks.MoveNext:
                _engine.MoveNext();
                break;

            case Tasks.PlayQuestionType:
                PlayQuestionType();
                break;
        }

        _taskRunner.ScheduleExecution(Tasks.NoTask, 0, runTimer: false);
    }

    private void LocalInfo_AnswerSelected(ItemViewModel answer)
    {
        if (answer.State != ItemState.Normal)
        {
            if (answer.State == ItemState.Active)
            {
                answer.State = ItemState.Normal;
                PresentationController.SetAnswerState(_selectedAnswerIndex, ItemState.Normal);
                _selectedAnswerIndex = -1;
            }

            return;
        }

        answer.State = ItemState.Active;

        var answerOptions = LocalInfo.AnswerOptions.Options;
        _selectedAnswerIndex = -1;

        for (var answerIndex = 0; answerIndex < answerOptions.Length; answerIndex++)
        {
            if (answerOptions[answerIndex] == answer)
            {
                _selectedAnswerIndex = answerIndex;
            }
            else if (answerOptions[answerIndex].State == ItemState.Active)
            {
                answerOptions[answerIndex].State = ItemState.Normal;
            }
        }

        if (_selectedAnswerIndex == -1)
        {
            return;
        }

        PresentationController.SetAnswerState(_selectedAnswerIndex, ItemState.Active);
    }

    private void PresentationListener_AnswerSelected(int answerIndex)
    {
        var answerOptions = LocalInfo.AnswerOptions.Options;

        if (answerIndex < 0 || answerIndex >= answerOptions.Length)
        {
            return;
        }

        if (answerOptions[answerIndex].State != ItemState.Normal)
        {
            if (answerOptions[answerIndex].State == ItemState.Active)
            {
                answerOptions[answerIndex].State = ItemState.Normal;
                PresentationController.SetAnswerState(answerIndex, ItemState.Normal);
                _selectedAnswerIndex = -1;
            }

            return;
        }

        _selectedAnswerIndex = answerIndex;

        for (var i = 0; i < answerOptions.Length; i++)
        {
            if (i != answerIndex && answerOptions[i].State == ItemState.Active)
            {
                answerOptions[i].State = ItemState.Normal;
            }
        }

        answerOptions[answerIndex].State = ItemState.Active;
        PresentationController.SetAnswerState(answerIndex, ItemState.Active);
    }

    private void Engine_QuestionFinish() => ClearState();

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender != null && e.PropertyName == nameof(AppSettings.ShowPlayers))
        {
            PresentationController.UpdateShowPlayers(((AppSettings)sender).ShowPlayers);
        }
    }

    private async void Engine_QuestionPostInfo()
    {
        await Task.Yield();

        try
        {
            PresentationController.OnQuestionEnd();
            _engine.MoveNext();
        }
        catch (Exception exc)
        {
            OnError(exc.ToString());
        }
    }

    private void GameHost_RoundThemesFinished()
    {
        if (_continuation == null)
        {
            return;
        }

        _continuation();
        _continuation = null;
    }

    private void GameHost_MediaEnd()
    {
        if (ActiveMediaCommand == StopMediaTimer)
        {
            ActiveMediaCommand = RunMediaTimer;
        }
    }

    private void GameHost_MediaProgress(double progress)
    {
        _mediaProgressBlock = true;

        try
        {
            MediaProgress = (int)(progress * 100);
        }
        finally
        {
            _mediaProgressBlock = false;
        }
    }

    private void GameHost_MediaStart() => IsMediaControlled = true;

    #region Event handlers

    private void Default_PropertyChanged(object? sender, PropertyChangedEventArgs e) =>
        PresentationController.UpdateSettings(Settings.SIUISettings.Model);

    private void PlayerInfo_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlayerInfo.IsSelected) || e.PropertyName == nameof(PlayerInfo.IsRegistered))
        {
            return;
        }

        if (sender == null)
        {
            return;
        }

        var player = (PlayerInfo)sender;

        if (e.PropertyName == nameof(PlayerInfo.WaitForRegistration))
        {
            if (player.WaitForRegistration)
            {
                foreach (var playerInfo in Players)
                {
                    if (playerInfo != sender)
                    {
                        playerInfo.WaitForRegistration = false;
                    }
                }
            }

            return;
        }

        PresentationController.UpdatePlayerInfo(Players.IndexOf(player), player, e.PropertyName);
    }

    private void QuestionTimer_Elapsed(object? state) =>
        UI.Execute(
            () =>
            {
                QuestionTime++;

                if (QuestionTime < QuestionTimeMax)
                {
                    return;
                }

                SetSound(Settings.Model.Sounds.NoAnswer);
                PresentationController.NoAnswer();
                StopQuestionTimer_Executed(1);
                ActiveQuestionCommand = null;

                if (!Settings.Model.SignalsAfterTimer)
                {
                    StopButtons();
                }
            },
            exc => OnError(exc.ToString()));

    private void SetSound(string soundName)
    {
        if (!Settings.Model.PlaySounds)
        {
            return;
        }

        PresentationController.SetSound(soundName);
    }

    private void RoundTimer_Elapsed(object? state) => UI.Execute(
        () =>
        {
            RoundTime++;

            if (RoundTime >= Settings.Model.RoundTime)
            {
                _engine.SetTimeout();
                StopRoundTimer_Executed(null);
            }
        },
        exc => OnError(exc.ToString()));

    private void ThinkingTimer_Elapsed(object? state) => UI.Execute(
        () =>
        {
            ThinkingTime++;

            if (ThinkingTime < ThinkingTimeMax)
            {
                return;
            }

            StopThinkingTimer_Executed(null);
        },
        exc => OnError(exc.ToString()));

    private void ThemeInfo_Selected(ThemeInfoViewModel theme)
    {
        int themeIndex;

        for (themeIndex = 0; themeIndex < LocalInfo.RoundInfo.Count; themeIndex++)
        {
            if (LocalInfo.RoundInfo[themeIndex] == theme)
            {
                break;
            }
        }

        PresentationController.DeletionCallback?.Invoke(themeIndex);
    }

    private void QuestionInfo_Selected(QuestionInfoViewModel question)
    {
        if (_continuation != null)
        {
            return;
        }

        int questionIndex = -1;
        int themeIndex;

        for (themeIndex = 0; themeIndex < LocalInfo.RoundInfo.Count; themeIndex++)
        {
            var found = false;

            for (questionIndex = 0; questionIndex < LocalInfo.RoundInfo[themeIndex].Questions.Count; questionIndex++)
            {
                if (LocalInfo.RoundInfo[themeIndex].Questions[questionIndex] == question)
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

        PresentationController.SelectionCallback?.Invoke(themeIndex, questionIndex);
    }

    #endregion

    #region Command handlers

    private void NextRound_Executed(object? arg)
    {
        if (!_engine.MoveNextRound())
        {
            return;
        }

        DropCurrentRound();
    }

    private void PreviousRound_Executed(object? arg)
    {
        if (!_engine.MoveBackRound())
        {
            return;
        }

        DropCurrentRound();
    }

    private void DropCurrentRound()
    {
        StopRoundTimer_Executed(0);
        StopQuestionTimer_Executed(0);
        StopThinkingTimer_Executed(0);
        ActiveRoundCommand = null;
        PresentationController.ClearState();
        LocalInfo.TStage = TableStage.Sign;
        Continuation = null;
    }

    private void RunRoundTimer_Executed(object? arg)
    {
        if (arg != null)
        {
            RoundTime = 0;
        }

        _roundTimer.Change(1000, 1000);

        ActiveRoundCommand = StopRoundTimer;
    }

    private void StopRoundTimer_Executed(object? arg)
    {
        if (arg != null)
        {
            RoundTime = 0;
        }

        _roundTimer.Change(Timeout.Infinite, Timeout.Infinite);
        
        ActiveRoundCommand = RunRoundTimer;
    }

    private void RunQuestionTimer_Executed(object? arg)
    {
        if (arg != null)
        {
            QuestionTime = 0;
        }

        PresentationController.RunTimer();

        _questionTimer.Change(1000, 1000);

        ActiveQuestionCommand = StopQuestionTimer;
    }

    private void StopQuestionTimer_Executed(object? arg)
    {
        if ((int?)arg == 0)
        {
            QuestionTime = 0;
            PresentationController.StopTimer();
        }
        else if (arg == null)
        {
            PresentationController.PauseTimer(QuestionTime * 10);
        }

        _questionTimer.Change(Timeout.Infinite, Timeout.Infinite);
        ActiveQuestionCommand = RunQuestionTimer;
    }

    private void RunThinkingTimer_Executed(object? arg)
    {
        if (arg != null)
        {
            ThinkingTime = 0;
        }

        _thinkingTimer.Change(1000, 1000);
    }

    public void StopThinkingTimer_Executed(object? arg)
    {
        if (arg != null)
        {
            ThinkingTime = 0;
        }

        _thinkingTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private void RunMediaTimer_Executed(object? arg)
    {
        PresentationController.RunMedia();
        ActiveMediaCommand = StopMediaTimer;
    }

    private void StopMediaTimer_Executed(object? arg)
    {
        PresentationController.StopMedia();
        ActiveMediaCommand = RunMediaTimer;
    }

    private void AddPlayer_Executed(object? arg)
    {
        var playerInfo = new PlayerInfo();
        playerInfo.PropertyChanged += PlayerInfo_PropertyChanged;

        Players.Add(playerInfo);
        PresentationController.AddPlayer();
    }

    private void RemovePlayer_Executed(object? arg)
    {
        if (arg is not PlayerInfo player)
        {
            return;
        }

        player.PropertyChanged -= PlayerInfo_PropertyChanged;
        var playerIndex = Players.IndexOf(player);
        Players.Remove(player);
        PresentationController.RemovePlayer(playerIndex);
    }

    private void ClearPlayers_Executed(object? arg)
    {
        Players.Clear();
        PresentationController.ClearPlayers();
    }

    private void ResetSums_Executed(object? arg)
    {
        foreach (var player in Players)
        {
            player.Sum = 0;
            player.State = PlayerState.None;
            player.Right = 0;
            player.Wrong = 0;
        }
    }

    private void GiveTurn_Executed(object? arg)
    {
        if (arg is not PlayerInfo player)
        {
            return;
        }

        Chooser = player;

        if (DecisionMode == DecisionMode.StarterChoosing)
        {
            DecisionMode = DecisionMode.None;
        }
        else if (DecisionMode == DecisionMode.AnswererChoosing)
        {
            DecisionMode = DecisionMode.None;
            Chooser.IsSelected = true;
            _selectedPlayer = Chooser;
        }
    }

    private void AddRight_Executed(object? arg)
    {
        try
        {
            if (arg is not PlayerInfo player)
            {
                if (_selectedPlayer == null)
                {
                    return;
                }

                player = _selectedPlayer;
            }

            player.Right++;
            player.Sum += Price;

            Chooser = player;

            _gameLogger.Write("{0} +{1}", player.Name, Price);

            if (_activeQuestion == null)
            {
                return;
            }

            SetSound(Settings.Model.Sounds.AnswerRight);

            _answeringHistory.Push(Tuple.Create(player, Price, true));

            PresentationController.PlayerIsRight(Players.IndexOf(player));

            if (Settings.Model.EndQuestionOnRightAnswer)
            {
                _engine.MoveToAnswer();
                Next_Executed();
            }
            else
            {
                ReturnToQuestion();
            }

        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowMessage($"{Resources.Error}: {exc.Message}");
        }
    }

    private void AddWrong_Executed(object? arg)
    {
        if (arg is not PlayerInfo player)
        {
            if (_selectedPlayer == null)
            {
                return;
            }

            player = _selectedPlayer;
        }

        player.Wrong++;

        var substract = Settings.Model.SubstractOnWrong ? (NegativePrice ?? Price) : 0;
        player.Sum -= substract;

        SetSound(Settings.Model.Sounds.AnswerWrong);

        if (LocalInfo.LayoutMode == LayoutMode.AnswerOptions && _selectedAnswerIndex > -1)
        {
            if (_selectedAnswerIndex < LocalInfo.AnswerOptions.Options.Length)
            {
                LocalInfo.AnswerOptions.Options[_selectedAnswerIndex].State = ItemState.Wrong;
            }

            PresentationController.SetAnswerState(_selectedAnswerIndex, ItemState.Wrong);
        }

        _gameLogger.Write("{0} -{1}", player.Name, substract);

        _answeringHistory.Push(Tuple.Create(player, Price, false));

        PresentationController.PlayerIsWrong(Players.IndexOf(player));

        ReturnToQuestion();
    }

    internal async Task StartAsync()
    {
        UpdateNextCommand();

        await PresentationController.StartAsync();

        PresentationController.ClearPlayers();

        for (int i = 0; i < Players.Count; i++)
        {
            PresentationController.AddPlayer();
            PresentationController.UpdatePlayerInfo(i, Players[i]);
        }

        PresentationController.SetLanguage(Thread.CurrentThread.CurrentUICulture.Name);
        PresentationController.UpdateSettings(Settings.SIUISettings.Model);
        PresentationController.UpdateShowPlayers(Settings.Model.ShowPlayers);
        PresentationController.ClearPlayersState();

        _buttonManager = PlatformManager.Instance.ButtonManagerFactory.Create(Settings.Model, this);

        _gameLogger.Write("Game started {0}", DateTime.Now);
        _gameLogger.Write("Package: {0}", _engine.PackageName);

        _selectedPlayers.Clear();

        if (Settings.Model.AutomaticGame)
        {
            Next_Executed();
        }
    }

    internal void OnQuestion(Question question)
    {
        ActiveQuestion = question;

        PresentationController.SetQuestion(question.Price);

        CurrentTheme = ActiveTheme?.Name;
        Price = question.Price;

        LocalInfo.Text = question.Price.ToString();
        LocalInfo.TStage = TableStage.QuestionPrice;
    }

    private void SetCaption(string caption) => PresentationController.SetCaption(Settings.Model.ShowTableCaption ? caption : "");

    internal void OnTheme(Theme theme)
    {
        PresentationController.SetTheme(theme.Name);

        LocalInfo.Text = $"{Resources.Theme}: {theme.Name}";
        LocalInfo.TStage = TableStage.Theme;

        ActiveTheme = theme;
    }

    private void Engine_EndGame() => PresentationController.ClearState();

    private void Stop_Executed(object? arg = null) => RequestStop?.Invoke();

    private void Engine_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(EngineBase.CanMoveNext):
                UpdateNextCommand();
                break;

            case nameof(EngineBase.CanMoveBack):
                _back.CanBeExecuted = _engine.CanMoveBack;
                break;

            case nameof(EngineBase.CanMoveNextRound):
                _nextRound.CanBeExecuted = _engine.CanMoveNextRound;
                break;

            case nameof(EngineBase.CanMoveBackRound):
                _previousRound.CanBeExecuted = _engine.CanMoveBackRound;
                break;
        }
    }

    /// <summary>
    /// Moves game to next stage.
    /// </summary>
    private void Next_Executed(object? arg = null)
    {
        try
        {
            if (_continuation != null)
            {
                _continuation();
                _continuation = null;
                return;
            }

            if (_taskRunner.CurrentTask != Tasks.NoTask)
            {
                _taskRunner.ExecuteImmediate();
                return;
            }

            _engine.MoveNext();
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowMessage($"{Resources.Error}: {exc.Message}");
        }
    }

    private void PlayQuestionType()
    {
        if (_activeQuestion == null)
        {
            return;
        }

        var typeName = _activeQuestion.TypeName ?? QuestionTypes.Simple;

        // Only StakeAll type is supported in final for now
        // This will be removed when full question type support will have been implemented
        if (_activeRound?.Type == RoundTypes.Final)
        {
            typeName = QuestionTypes.StakeAll;
        }

        if (typeName == QuestionTypes.Simple)
        {
            _engine.MoveNext();
            return;
        }

        switch (typeName)
        {
            case QuestionTypes.Secret:
            case QuestionTypes.SecretPublicPrice:
            case QuestionTypes.SecretNoQuestion:
                SetSound(Settings.Model.Sounds.SecretQuestion);
                PrintQuestionType(typeName, Resources.SecretQuestion.ToUpper(), Settings.Model.SpecialsAliases.SecretQuestionAlias, -1);
                break;

            case QuestionTypes.Stake:
                SetSound(Settings.Model.Sounds.StakeQuestion);
                PrintQuestionType(typeName, Resources.StakeQuestion.ToUpper(), Settings.Model.SpecialsAliases.StakeQuestionAlias, ActiveRound.Themes.IndexOf(ActiveTheme));
                break;

            case QuestionTypes.NoRisk:
                SetSound(Settings.Model.Sounds.NoRiskQuestion);
                PrintQuestionType(typeName, Resources.NoRiskQuestion.ToUpper(), Settings.Model.SpecialsAliases.NoRiskQuestionAlias, -1);
                break;

            default:
                PresentationController.SetText(typeName);
                break;
        }

        LocalInfo.TStage = TableStage.Special;
    }

    private void Engine_Package(Package package)
    {
        var videoUrl = Settings.Model.VideoUrl;

        if (!string.IsNullOrWhiteSpace(videoUrl))
        {
            if (SetMedia(new MediaInfo(videoUrl)))
            {
                PresentationController.SetStage(TableStage.Question);
                PresentationController.SetQuestionContentType(QuestionContentType.Video);
            }
        }
        else
        {
            var logo = package.LogoItem;
            var media = logo != null ? _engine.Document.TryGetMedia(logo) : null;

            PresentationController.OnPackage(package.Name, media);

            SetSound(Settings.Model.Sounds.BeginGame);
        }

        LocalInfo.TStage = TableStage.Sign;
    }

    private void Engine_GameThemes(IEnumerable<string> themes)
    {
        PresentationController.SetGameThemes(themes);
        LocalInfo.TStage = TableStage.GameThemes;

        SetSound(Settings.Model.Sounds.GameThemes);
    }

    internal void OnRound(Round round, QuestionSelectionStrategyType selectionStrategyType)
    {
        _activeRound = round ?? throw new ArgumentNullException(nameof(round));
        OnPropertyChanged(nameof(ActiveRound));

        if (PresentationController == null)
        {
            return;
        }

        PresentationController.SetRound(round.Name, selectionStrategyType);
        SetSound(Settings.Model.Sounds.RoundBegin);
        LocalInfo.TStage = TableStage.Round;

        _gameLogger.Write("\r\n{0} {1}", Resources.Round, round.Name);

        if (round.Type == RoundTypes.Standart)
        {
            if (Settings.Model.RoundTime > 0)
            {
                RunRoundTimer_Executed(0);
            }
        }
    }

    public void OnRoundThemes(IReadOnlyList<Theme> roundThemes)
    {
        _roundThemes = roundThemes;
        LocalInfo.RoundInfo.Clear();

        _gameLogger.Write($"{Resources.RoundThemes}:");

        int maxQuestion = roundThemes.Max(theme => theme.Questions.Count);
        
        foreach (var theme in roundThemes)
        {
            var themeInfo = new ThemeInfoViewModel { Name = theme.Name };
            LocalInfo.RoundInfo.Add(themeInfo);

            _gameLogger.Write(theme.Name);

            for (int i = 0; i < maxQuestion; i++)
            {
                var questionInfo = new QuestionInfoViewModel { Price = i < theme.Questions.Count ? theme.Questions[i].Price : -1 };
                themeInfo.Questions.Add(questionInfo);
            }
        }

        PresentationController.SetRoundThemes(LocalInfo.RoundInfo.ToArray(), false);
        SetSound(Settings.Model.Sounds.RoundThemes);
        LocalInfo.TStage = TableStage.RoundTable;
        Continuation = AfterRoundThemes;
    }

    private bool AfterRoundThemes()
    {
        Continuation = null;
        PresentationController.SetRoundTable();
        _engine.MoveNext();

        if (!Players.Any())
        {
            return false;
        }

        var minSum = Players.Min(p => p.Sum);
        var playersWithMinSum = Players.Select((p, i) => (p, i)).Where(pair => pair.p.Sum == minSum).ToArray();
        var playersWithMinSumCount = playersWithMinSum.Length;

        if (playersWithMinSumCount == 1)
        {
            ChooserIndex = playersWithMinSum[0].i;
            return false;
        }
        else if (playersWithMinSumCount > 1)
        {
            DecisionMode = DecisionMode.StarterChoosing;
        }

        return true;
    }

    public void StartQuestionTimer()
    {
        if (Settings.Model.ThinkingTime <= 0)
        {
            return;
        }

        // Runs timer in game with false starts
        QuestionTimeMax = Settings.Model.ThinkingTime;
        PresentationController.SetTimerMaxTime(QuestionTimeMax * 10);
        RunQuestionTimer_Executed(0);
    }

    public void AskAnswerDirect()
    {
        if (ActiveRound?.Type == RoundTypes.Final)
        {
            SetSound(Settings.Model.Sounds.FinalThink);
            PresentationController.OnFinalThink();

            var time = Settings.Model.FinalQuestionThinkingTime;

            if (time > 0)
            {
                ThinkingTimeMax = time;
                RunThinkingTimer_Executed(0);
            }

            return;
        }
        else
        {
            var time = Settings.Model.SpecialQuestionThinkingTime;

            if (time > 0)
            {
                ThinkingTimeMax = time;
                RunThinkingTimer_Executed(0);
            }
        }
    }

    public void OnRightAnswer()
    {
        StopQuestionTimer.Execute(0);
        StopThinkingTimer_Executed(0);
        StopButtons();
        ActiveMediaCommand = null;
    }

    private void Engine_RoundEmpty() => StopRoundTimer_Executed(0);

    private void Engine_NextQuestion() => _engine.MoveNext();

    private void Engine_RoundTimeout()
    {
        SetSound(Settings.Model.Sounds.RoundTimeout);
        _gameLogger.Write(Resources.RoundTimeout);
    }

    private void Engine_EndQuestion(int themeIndex, int questionIndex)
    {
        if (themeIndex > -1 && themeIndex < LocalInfo.RoundInfo.Count)
        {
            var themeInfo = LocalInfo.RoundInfo[themeIndex];

            if (questionIndex > -1 && questionIndex < themeInfo.Questions.Count)
            {
                themeInfo.Questions[questionIndex].Price = -1;
            }
        }
    }

    private void ClearState()
    {
        StopQuestionTimer_Executed(0);
        StopThinkingTimer_Executed(0);

        StopButtons();

        UnselectPlayer();
        _selectedPlayers.Clear();

        foreach (var player in Players)
        {
            player.BlockedTime = null;
        }

        ActiveQuestionCommand = null;
        ActiveMediaCommand = null;

        PresentationController.FinishQuestion();

        CurrentTheme = null;
        Price = 0;

        _selectedAnswerIndex = -1;
        LocalInfo.LayoutMode = LayoutMode.Simple;
        LocalInfo.AnswerOptions.Options = Array.Empty<ItemViewModel>();

        DecisionMode = DecisionMode.None;
        NegativePrice = null;
    }

    internal void OnFinalThemes(IReadOnlyList<Theme> finalThemes)
    {
        LocalInfo.RoundInfo.Clear();

        foreach (var theme in finalThemes)
        {
            if (theme.Questions.Count == 0)
            {
                continue;
            }

            var themeInfo = new ThemeInfoViewModel { Name = theme.Name };
            LocalInfo.RoundInfo.Add(themeInfo);
        }

        PresentationController.SetRoundThemes(LocalInfo.RoundInfo.ToArray(), true);
        PresentationController.SetSound();
        LocalInfo.TStage = TableStage.Final;
        _taskRunner.ScheduleExecution(Tasks.MoveNext, 1);
    }

    /// <summary>
    /// Moves back.
    /// </summary>
    private void Back_Executed(object? arg = null)
    {
        _engine.MoveBack();
        _engine.MoveNext();

        // Handle normal question ending for all of this

        StopQuestionTimer_Executed(0);
        StopThinkingTimer_Executed(0);

        StopButtons();
        State = QuestionState.Normal;
        _previousState = QuestionState.Normal;

        DecisionMode = DecisionMode.None;

        UnselectPlayer();
        _selectedPlayers.Clear();

        foreach (var player in Players)
        {
            player.BlockedTime = null;
        }

        ActiveQuestionCommand = null;
        ActiveMediaCommand = null;

        _engine.UpdateCanNext();

        if (Settings.Model.DropStatsOnBack)
        {
            while (_answeringHistory.Count > 0)
            {
                var item = _answeringHistory.Pop();

                if (item == null)
                {
                    break;
                }

                if (item.Item3)
                {
                    item.Item1.Right--;
                    item.Item1.Sum -= item.Item2;
                }
                else
                {
                    item.Item1.Wrong--;
                    item.Item1.Sum += item.Item2;
                }
            }
        }
    }

    #endregion

    internal void OnThemeDeleted(int themeIndex)
    {
        PresentationController.PlaySelection(themeIndex);
        LocalInfo.RoundInfo[themeIndex].Name = "";
        SetSound(Settings.Model.Sounds.FinalDelete);
        _taskRunner.ScheduleExecution(Tasks.MoveNext, 1);
    }

    private void UpdateNextCommand() => _next.CanBeExecuted = _continuation != null || _engine != null && _engine.CanMoveNext;

    private void Engine_ShowScore()
    {
        PresentationController.SetStage(TableStage.Score);
        LocalInfo.TStage = TableStage.Score;
    }

    private void ReturnToQuestion()
    {
        State = _previousState;

        if (_timerStopped)
        {
            RunQuestionTimer_Executed(null);
        }

        UnselectPlayer();

        StopThinkingTimer_Executed(0);

        if (_mediaStopped)
        {
            RunMediaTimer_Executed(null);
        }
    }

    /// <summary>
    /// Ends the game.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        try
        {
            _taskRunner.Dispose();
            PresentationController.Dispose();

            StopRoundTimer_Executed(null);
            _roundTimer.Dispose();

            StopQuestionTimer_Executed(0);
            _questionTimer.Dispose();

            StopThinkingTimer_Executed(0);
            _thinkingTimer.Dispose();

            Settings.Model.SIUISettings.PropertyChanged -= Default_PropertyChanged;
            Settings.SIUISettings.PropertyChanged -= Default_PropertyChanged;

            if (_buttonManager != null)
            {
                _buttonManager.Stop();
                await _buttonManager.DisposeAsync();
                _buttonManager = null;
            }

            lock (_engine.SyncRoot)
            {
                _engine.PropertyChanged -= Engine_PropertyChanged;
                
                try
                {
                    _engine.Dispose();
                }
                catch (IOException exc)
                {
                    _gameLogger.Write($"Engine dispose error: {exc}");
                }

                _gameLogger.Dispose();

                PresentationController.SetSound();

                PlatformManager.Instance.ClearMedia();
            }
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowMessage(string.Format(Resources.GameEndingError, exc.Message));
        }

        _isDisposed = true;
    }

    internal void OnThemeSelected(int themeIndex, int questionIndex)
    {
        var theme = ActiveRound.Themes[themeIndex];

        ActiveTheme = theme;
        ActiveQuestion = theme.Questions[questionIndex];

        PresentationController.SetTheme(theme.Name);
        PresentationController.SetSound();
        SetCaption(theme.Name);
    }

    /// <summary>
    /// Writes players scores to log.
    /// </summary>
    private void LogScore()
    {
        if (!Settings.Model.SaveLogs || Players.Count <= 0)
        {
            return;
        }

        var sb = new StringBuilder("\r\n").Append(Resources.Score).Append(": ");
        var first = true;

        foreach (var player in Players)
        {
            if (!first)
            {
                sb.Append(", ");
            }

            first = false;
            sb.AppendFormat("{0}:{1}", player.Name, player.Sum);
        }

        _gameLogger.Write(sb.ToString());
    }

    private void Engine_NextRound(bool showSign)
    {
        ActiveRoundCommand = null;
        PresentationController.SetSound();

        if (showSign)
        {
            PresentationController.ClearState();
        }
    }

    internal void InitMedia()
    {
        ActiveMediaCommand = StopMediaTimer;
        IsMediaControlled = false;
        MediaProgress = 0;
    }

    private bool SetMedia(MediaInfo media, bool background = false)
    {
        if (media.Uri == null)
        {
            return false;
        }

        PresentationController.SetMedia(new MediaSource(media.Uri.OriginalString), background);
        return true;
    }

    private string? _currentTheme;

    public string? CurrentTheme
    {
        get => _currentTheme;
        set
        {
            _currentTheme = value;
            UpdateCaption();
        }
    }

    private void UpdateCaption()
    {
        if (_currentTheme == null)
        {
            return;
        }

        var caption = _price > 0 ? $"{_currentTheme}, {_price}" : _currentTheme;
        SetCaption(caption);
    }

    internal void OnQuestionSelected(int themeIndex, int questionIndex)
    {
        try
        {
            if (_roundThemes == null)
            {
                throw new InvalidOperationException("_roundThemes == null");
            }

            _answeringHistory.Push(null);

            ActiveTheme = _roundThemes[themeIndex];
            ActiveQuestion = ActiveTheme.Questions[questionIndex];

            CurrentTheme = ActiveTheme.Name;
            Price = ActiveQuestion.Price;

            LogScore();
            _gameLogger.Write("\r\n{0}, {1}", CurrentTheme, Price);

            SetSound(Settings.Model.Sounds.QuestionSelected);
            PresentationController.PlaySimpleSelection(themeIndex, questionIndex);
            _taskRunner.ScheduleExecution(Tasks.PlayQuestionType, 17);

            _gameLogger.Write(ActiveQuestion.GetText());
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowMessage(string.Format(Resources.GameEndingError, exc.Message));
        }
    }

    private void PrintQuestionType(string typeName, string originalTypeName, string? aliasName, int activeThemeIndex)
    {
        var actualName = string.IsNullOrWhiteSpace(aliasName) ? originalTypeName : aliasName;

        PresentationController.SetQuestionType(typeName, actualName, activeThemeIndex);
        _gameLogger.Write(actualName);
    }

    private void OnError(string error) => Error?.Invoke(error);

    public PlayerInfo? GetPlayerById(string playerId, bool strict)
    {
        if (Settings.Model.UsePlayersKeys != PlayerKeysModes.Web)
        {
            return null;
        }

        lock (_playersTable)
        {
            if (_playersTable.TryGetValue(playerId, out var player))
            {
                return player;
            }

            if (!strict)
            {
                foreach (var playerInfo in Players)
                {
                    if (playerInfo.WaitForRegistration)
                    {
                        playerInfo.WaitForRegistration = false;
                        playerInfo.IsRegistered = true;

                        _playersTable[playerId] = playerInfo;

                        return playerInfo;
                    }
                }
            }
        }

        return null;
    }

    public bool OnKeyPressed(GameKey key)
    {
        var index = Settings.Model.PlayerKeys2.IndexOf(key);

        if (index == -1 || index >= Players.Count)
        {
            return false;
        }

        var player = Players[index];
        return ProcessPlayerPress(index, player);
    }

    public bool OnPlayerPressed(PlayerInfo player)
    {
        var index = Players.IndexOf(player);

        if (index == -1)
        {
            return false;
        }

        return ProcessPlayerPress(index, player);
    }

    private bool ProcessPlayerPress(int index, PlayerInfo player)
    {
        // The player has pressed already
        if (_selectedPlayers.Contains(player))
        {
            return false;
        }

        // It is not pressing time
        if (_state != QuestionState.Pressing)
        {
            player.BlockedTime = DateTime.Now;

            // Somebody is answering already
            if (_selectedPlayer != null)
            {
                if (Settings.Model.ShowLostButtonPlayers && _selectedPlayer != player && !_selectedPlayers.Contains(player))
                {
                    PresentationController.AddLostButtonPlayerIndex(index);
                }
            }

            return false;
        }

        // Player is blocked
        if (player.BlockedTime.HasValue && DateTime.Now.Subtract(player.BlockedTime.Value).TotalSeconds < Settings.Model.BlockingTime)
        {
            return false;
        }

        // All checks passed, confirming press
        player.IsSelected = true;
        _selectedPlayer = player;
        _selectedPlayers.Add(_selectedPlayer);

        SetSound(Settings.Model.Sounds.PlayerPressed);
        PresentationController.SetActivePlayerIndex(index);

        _previousState = State;
        State = QuestionState.Pressed;

        _timerStopped = ActiveQuestionCommand == StopQuestionTimer;

        if (_timerStopped)
        {
            StopQuestionTimer.Execute(null);
        }

        _mediaStopped = ActiveMediaCommand == StopMediaTimer;

        if (_mediaStopped)
        {
            StopMediaTimer_Executed(null);
        }

        ThinkingTimeMax = Settings.Model.ThinkingTime2;
        RunThinkingTimer_Executed(0);

        BlockNextButtonForAWhile();

        return true;
    }

    private async void BlockNextButtonForAWhile()
    {
        if (!_next.CanBeExecuted)
        {
            return;
        }

        _next.CanBeExecuted = false;
        await Task.Delay(500);
        UpdateNextCommand();
    }

    private void UnselectPlayer()
    {
        if (_selectedPlayer != null)
        {
            _selectedPlayer.IsSelected = false;
            _selectedPlayer = null;
        }

        PresentationController.ClearPlayersState();
    }

    private void OnStateChanged()
    {
        switch (_state)
        {
            case QuestionState.Normal:
                PresentationController.SetQuestionStyle(QuestionStyle.Normal);
                StopThinkingTimer_Executed(0);
                break;

            case QuestionState.Pressing:
                if (Settings.Model.ShowQuestionBorder)
                {
                    PresentationController.SetQuestionStyle(QuestionStyle.WaitingForPress);
                }
                break;

            case QuestionState.Pressed:
                PresentationController.SetQuestionStyle(Settings.Model.ShowPlayers ? QuestionStyle.Normal : QuestionStyle.Pressed);
                break;

            case QuestionState.Thinking:
                break;

            default:
                throw new InvalidOperationException($"_state has an invalid value of {_state}");
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;

    internal Task CloseMainViewAsync() => PresentationController.StopAsync();

    internal void StartButtons()
    {
        if (_buttonManager == null)
        {
            return;
        }

        UI.Execute(() => _buttonManager.Start(), exc => OnError(exc.Message));
    }

    private void StopButtons()
    {
        if (_buttonManager == null)
        {
            return;
        }

        UI.Execute(() => _buttonManager.Stop(), exc => OnError(exc.Message));
    }

    internal void AskAnswerButton()
    {
        PresentationController.BeginPressButton();
        State = QuestionState.Pressing;
    }

    internal void OnQuestionStart()
    {
        State = QuestionState.Normal;
        _previousState = QuestionState.Normal;
    }

    internal void OnContentStart()
    {
        LocalInfo.TStage = TableStage.Question;
    }

    internal bool SelectStake(NumberSet availableRange)
    {
        if (availableRange.Maximum == availableRange.Minimum && availableRange.Minimum == 0)
        {
            // Minimum or maximum in round
            if (_activeRound == null || _activeRound.Themes.Max(t => t.Questions.Count) == 0)
            {
                return false;
            }

            var minimum = _activeRound.Themes[0].Questions[0].Price;
            var maximum = 0;

            foreach (var theme in _activeRound.Themes)
            {
                foreach (var question in theme.Questions)
                {
                    if (question.Price == Question.InvalidPrice)
                    {
                        continue;
                    }

                    minimum = Math.Min(minimum, question.Price);
                    maximum = Math.Max(maximum, question.Price);
                }
            }

            availableRange = new NumberSet { Minimum = minimum, Maximum = maximum, Step = maximum - minimum };
        }

        DecisionMode = DecisionMode.SimpleStake;
        StakeInfo = availableRange;
        Price = availableRange.Minimum;
        return true;
    }

    internal void OnSetNoRiskPrice()
    {
        Price = _activeQuestion.Price * 2;
        NegativePrice = 0;
    }

    internal bool OnSetAnswererDirectly(bool canSelectChooser)
    {
        var playerCount = Players.Count;

        if (playerCount == 0)
        {
            return false;
        }

        if (playerCount == 1 && Players[0] == Chooser)
        {
            if (canSelectChooser)
            {
                Chooser.IsSelected = true;
                _selectedPlayer = Chooser;
            }

            return false;
        }

        if (playerCount == 2 && !canSelectChooser && Chooser != null && Players.Contains(Chooser))
        {
            var activePlayer = Players[0] == Chooser ? Players[1] : Players[0];
            activePlayer.IsSelected = true;
            _selectedPlayer = activePlayer;

            return false;
        }

        CanSelectChooser = canSelectChooser;
        DecisionMode = DecisionMode.AnswererChoosing;

        return true;
    }

    internal void Accept()
    {
        if (_selectedPlayer != null)
        {
            _selectedPlayer.Sum += Price;
        }
    }

    internal bool OnSetAnswererByHighestStake()
    {
        if (Chooser == null)
        {
            return false;
        }

        var chooserIndex = Players.IndexOf(Chooser);

        if (chooserIndex == -1)
        {
            return false;
        }

        InitStakers(chooserIndex);

        if (_stakers.Count == 1)
        {
            SetStakerToAnswer();
            return false;
        }

        _isAllIn = false;
        AskNextStake(true);
        Pass.CanBeExecuted = false;
        DecisionMode = DecisionMode.Stake;

        return true;
    }

    private void InitStakers(int chooserIndex)
    {
        Stake = Price;

        _stakers.Clear();

        _stakers.Add(chooserIndex);

        var orderedPlayerIndicies = Players
            .Select((p, i) => (p, i))
            .Where(pair => pair.p.Sum > Stake && pair.i != chooserIndex)
            .OrderBy(pair => pair.p.Sum)
            .Select(pair => pair.i);

        _stakers.AddRange(orderedPlayerIndicies);
        _stakersIterator = 0;
    }

    private void Pass_Executed(object? arg)
    {
        _stakers.RemoveAt(_stakersIterator);

        if (_stakersIterator == _stakers.Count)
        {
            _stakersIterator = 0;
        }

        if (_stakers.Count == 1)
        {
            SetStakerToAnswer();
            return;
        }

        AskNextStake();
    }

    private void MakeStake_Executed(object? arg)
    {
        Price = Stake;

        for (var i = 0; i < _stakers.Count; )
        {
            if (i == _stakersIterator)
            {
                i++;
                continue;
            }

            if (Players[_stakers[i]].Sum <= Stake)
            {
                _stakers.RemoveAt(i);

                if (_stakersIterator > i)
                {
                    _stakersIterator--;
                }

                continue;
            }

            i++;
        }

        if (_stakers.Count == 1)
        {
            SetStakerToAnswer();
            return;
        }

        _isAllIn = Staker.Sum == Stake;

        _stakersIterator++;

        if (_stakersIterator == _stakers.Count)
        {
            _stakersIterator = 0;
        }

        AskNextStake();
        Pass.CanBeExecuted = true;
    }

    private void AskNextStake(bool firstTime = false)
    {
        StakerIndex = _stakers[_stakersIterator];
        var staker = Staker;

        if (_isAllIn)
        {
            Stake = staker.Sum;
        }
        else if (!firstTime)
        {
            Stake = Math.Min(staker.Sum, Stake + 100);
        }

        StakeInfo = new NumberSet { Minimum = Stake, Maximum = staker.Sum, Step = 100 };
    }

    private void SetStakerToAnswer()
    {
        StakerIndex = _stakers[0];
        var staker = Staker;
        staker.IsSelected = true;
        _selectedPlayer = staker;
        Chooser = _selectedPlayer;
        DecisionMode = DecisionMode.None;
    }

    internal bool OnSetAnswererAsCurrent()
    {
        if (Chooser != null)
        {
            Chooser.IsSelected = true;
            _selectedPlayer = Chooser;
        }

        return false;
    }
}
