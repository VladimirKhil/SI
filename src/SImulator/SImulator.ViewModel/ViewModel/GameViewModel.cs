using SIEngine;
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
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Utils;
using Utils.Commands;

namespace SImulator.ViewModel;

/// <summary>
/// Controls a single game run.
/// </summary>
public sealed class GameViewModel : INotifyPropertyChanged, IButtonManagerListener, IAsyncDisposable
{
    #region Fields

    internal event Action<string>? Error;
    internal event Action? RequestStop;

    private readonly Stack<Tuple<PlayerInfo, int, bool>> _answeringHistory = new();

    private readonly EngineBase _engine;

    /// <summary>
    /// Game buttons manager.
    /// </summary>
    private IButtonManager? _buttonManager;

    /// <summary>
    /// Game log writer.
    /// </summary>
    private readonly ILogger _logger;

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
                _chooser = _chooserIndex >= 0 && _chooserIndex < LocalInfo.Players.Count ? (PlayerInfo)LocalInfo.Players[_chooserIndex] : null;
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
                _chooserIndex = value != null ? LocalInfo.Players.IndexOf(value) : -1;
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

    private bool _playingQuestionType = false;

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

    public Round? ActiveRound => _activeRound;

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

    private Atom? _activeAtom;

    /// <summary>
    /// Current active question atom.
    /// </summary>
    public Atom? ActiveAtom
    {
        get => _activeAtom;
        set { if (_activeAtom != value) { _activeAtom = value; OnPropertyChanged(); } }
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
                    PresentationController?.SeekMedia(_mediaProgress);

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

    private NumberSet _stakeInfo;

    /// <summary>
    /// Player possible stakes info.
    /// </summary>
    public NumberSet StakeInfo
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

    public PlayerInfo Staker => (PlayerInfo)LocalInfo.Players[_stakerIndex];

    #endregion

    public GameViewModel(
        AppSettingsViewModel settings,
        ISIEngine engine,
        IExtendedListener presentationListener,
        IPresentationController presentationController,
        IList<SimplePlayerInfo> players,
        ILogger logger)
    {
        Settings = settings;
        _engine = (EngineBase)engine;
        _presentationListener = presentationListener;
        _logger = logger;
        PresentationController = presentationController;

        LocalInfo = new TableInfoViewModel(players);

        foreach (var playerInfo in LocalInfo.Players.Cast<PlayerInfo>())
        {
            playerInfo.IsRegistered = false;
            playerInfo.PropertyChanged += PlayerInfo_PropertyChanged;
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
        _addRight = new SimpleCommand(AddRight_Executed) { CanBeExecuted = false };
        _addWrong = new SimpleCommand(AddWrong_Executed) { CanBeExecuted = false };

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
        _engine.Round += Engine_Round;
        _engine.RoundThemes += Engine_RoundThemes;
        _engine.Theme += Engine_Theme;
        _engine.Question += Engine_Question;
        _engine.QuestionSelected += Engine_QuestionSelected;
        _engine.ShowScore += Engine_ShowScore;
        _engine.LogScore += LogScore;
        _engine.QuestionPostInfo += Engine_QuestionPostInfo;
        _engine.QuestionFinish += Engine_QuestionFinish;
        _engine.EndQuestion += Engine_EndQuestion;
        _engine.RoundTimeout += Engine_RoundTimeout;
        _engine.NextQuestion += Engine_NextQuestion;
        _engine.RoundEmpty += Engine_RoundEmpty;
        _engine.FinalThemes += Engine_FinalThemes;
        _engine.ThemeSelected += Engine_ThemeSelected;
        _engine.PrepareFinalQuestion += Engine_PrepareFinalQuestion;
        _engine.Error += OnError;
        _engine.EndGame += Engine_EndGame;

        _engine.PropertyChanged += Engine_PropertyChanged;

        _presentationListener.MediaStart += GameHost_MediaStart;
        _presentationListener.MediaProgress += GameHost_MediaProgress;
        _presentationListener.MediaEnd += GameHost_MediaEnd;
        _presentationListener.RoundThemesFinished += GameHost_RoundThemesFinished;
        _presentationListener.AnswerSelected += PresentationListener_AnswerSelected;
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
                foreach (PlayerInfo item in LocalInfo.Players.Cast<PlayerInfo>())
                {
                    if (item != sender)
                    {
                        item.WaitForRegistration = false;
                    }
                }
            }

            return;
        }

        PresentationController?.UpdatePlayerInfo(LocalInfo.Players.IndexOf(player), player);
    }

    private void QuestionTimer_Elapsed(object? state) =>
        UI.Execute(
            () =>
            {
                QuestionTime++;
                PresentationController.SetLeftTime(1.0 - (double)QuestionTime / QuestionTimeMax);

                if (QuestionTime < QuestionTimeMax)
                {
                    return;
                }

                SetSound(Settings.Model.Sounds.NoAnswer);
                StopQuestionTimer_Executed(null);
                ActiveQuestionCommand = null;

                if (!Settings.Model.SignalsAfterTimer && _buttonManager != null)
                {
                    _buttonManager.Stop();
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

        _presentationListener.OnThemeSelected(themeIndex);
    }

    private void QuestionInfo_Selected(QuestionInfoViewModel question)
    {
        if (!((TvEngine)_engine).CanSelectQuestion || _continuation != null)
        {
            return;
        }

        int questionIndex = -1;
        int themeIndex;

        for (themeIndex = 0; themeIndex < LocalInfo.RoundInfo.Count; themeIndex++)
        {
            bool found = false;

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

        _presentationListener.OnQuestionSelected(themeIndex, questionIndex);
    }

    #endregion

    #region Command handlers

    private void NextRound_Executed(object? arg)
    {
        StopRoundTimer_Executed(0);
        StopQuestionTimer_Executed(0);
        StopThinkingTimer_Executed(0);
        _engine.MoveNextRound();
        Continuation = null;
    }

    private void PreviousRound_Executed(object? arg)
    {
        StopRoundTimer_Executed(0);
        StopQuestionTimer_Executed(0);
        StopThinkingTimer_Executed(0);
        ActiveRoundCommand = null;
        PresentationController.SetStage(TableStage.Sign);

        _engine.MoveBackRound();
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
            PresentationController.SetLeftTime(1.0);
        }

        _questionTimer.Change(1000, 1000);

        ActiveQuestionCommand = StopQuestionTimer;
    }

    private void StopQuestionTimer_Executed(object? arg)
    {
        if (arg != null)
        {
            QuestionTime = 0;
            PresentationController.SetLeftTime(1.0);
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

        LocalInfo.Players.Add(playerInfo);
        PresentationController.AddPlayer();
    }

    private void RemovePlayer_Executed(object? arg)
    {
        if (arg is not SimplePlayerInfo player)
        {
            return;
        }

        player.PropertyChanged -= PlayerInfo_PropertyChanged;
        LocalInfo.Players.Remove(player);
        PresentationController.RemovePlayer(player.Name);
    }

    private void ClearPlayers_Executed(object? arg)
    {
        LocalInfo.Players.Clear();
        PresentationController.ClearPlayers();
    }

    private void ResetSums_Executed(object? arg)
    {
        foreach (var player in LocalInfo.Players.Cast<PlayerInfo>())
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

        SetSound(Settings.Model.Sounds.AnswerRight);

        _logger.Write("{0} +{1}", player.Name, Price);

        _answeringHistory.Push(Tuple.Create(player, Price, true));

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

        _logger.Write("{0} -{1}", player.Name, substract);

        _answeringHistory.Push(Tuple.Create(player, Price, false));

        ReturnToQuestion();
    }

    internal void Start()
    {
        UpdateNextCommand();

        PresentationController.ClearPlayers();

        for (int i = 0; i < LocalInfo.Players.Count; i++)
        {
            PresentationController.AddPlayer();
            PresentationController.UpdatePlayerInfo(i, (PlayerInfo)LocalInfo.Players[i]);
        }

        PresentationController.Start();

        _buttonManager = PlatformManager.Instance.ButtonManagerFactory.Create(Settings.Model, this);

        _logger.Write("Game started {0}", DateTime.Now);
        _logger.Write("Package: {0}", _engine.PackageName);

        _selectedPlayers.Clear();
        PresentationController.ClearPlayersState();

        if (Settings.Model.AutomaticGame)
        {
            Next_Executed();
        }
    }

    private void Engine_Question(Question question)
    {
        ActiveQuestion = question;

        PresentationController.SetText(question.Price.ToString());
        PresentationController.SetStage(TableStage.QuestionPrice);

        CurrentTheme = ActiveTheme?.Name;
        Price = question.Price;

        LocalInfo.Text = question.Price.ToString();
        LocalInfo.TStage = TableStage.QuestionPrice;

        _playingQuestionType = true;
    }

    private void SetCaption(string caption) => PresentationController.SetCaption(Settings.Model.ShowTableCaption ? caption : "");

    private void Engine_Theme(Theme theme)
    {
        PresentationController.SetText($"{Resources.Theme}: {theme.Name}");
        PresentationController.SetStage(TableStage.Theme);

        LocalInfo.Text = $"{Resources.Theme}: {theme.Name}";
        LocalInfo.TStage = TableStage.Theme;

        ActiveTheme = theme;
    }

    private void Engine_EndGame() => PresentationController?.SetStage(TableStage.Sign);

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

            case nameof(EngineBase.Stage):
                _addRight.CanBeExecuted = _addWrong.CanBeExecuted = _engine.CanChangeSum();
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

            if (_playingQuestionType)
            {
                _playingQuestionType = false;
                var (played, _) = PlayQuestionType();

                if (played)
                {
                    return;
                }
            }

            _engine.MoveNext();
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowMessage($"{Resources.Error}: {exc.Message}");
        }
    }

    private (bool played, bool highlightTheme) PlayQuestionType()
    {
        if (_activeQuestion == null)
        {
            return (false, false);
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
            return (false, false);
        }

        var highlightTheme = true;

        switch (typeName)
        {
            case QuestionTypes.Cat:
            case QuestionTypes.BagCat:
            case QuestionTypes.Secret:
            case QuestionTypes.SecretPublicPrice:
            case QuestionTypes.SecretNoQuestion:
                SetSound(Settings.Model.Sounds.SecretQuestion);
                PrintQuestionType(Resources.SecretQuestion.ToUpper(), Settings.Model.SpecialsAliases.SecretQuestionAlias);
                highlightTheme = false;
                break;

            case QuestionTypes.Auction:
            case QuestionTypes.Stake:
                SetSound(Settings.Model.Sounds.StakeQuestion);
                PrintQuestionType(Resources.StakeQuestion.ToUpper(), Settings.Model.SpecialsAliases.StakeQuestionAlias);
                break;

            case QuestionTypes.Sponsored:
            case QuestionTypes.NoRisk:
                SetSound(Settings.Model.Sounds.NoRiskQuestion);
                PrintQuestionType(Resources.NoRiskQuestion.ToUpper(), Settings.Model.SpecialsAliases.NoRiskQuestionAlias);
                highlightTheme = false;
                break;

            default:
                PresentationController.SetText(typeName);
                break;
        }

        LocalInfo.TStage = TableStage.Special;
        return (true, highlightTheme);
    }

    private void Engine_Package(Package package)
    {
        if (PresentationController == null)
        {
            return;
        }

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

            if (logo != null)
            {
                var media = _engine.Document.TryGetMedia(logo);

                if (media.HasValue && SetMedia(media.Value))
                {
                    PresentationController.SetStage(TableStage.Question);
                    PresentationController.SetQuestionSound(false);
                    PresentationController.SetQuestionContentType(QuestionContentType.Image);
                }
            }

            SetSound(Settings.Model.Sounds.BeginGame);
        }

        LocalInfo.TStage = TableStage.Sign;
    }

    private void Engine_GameThemes(string[] themes)
    {
        PresentationController.SetGameThemes(themes);
        LocalInfo.TStage = TableStage.GameThemes;

        SetSound(Settings.Model.Sounds.GameThemes);
    }

    private void Engine_Round(Round round)
    {
        _activeRound = round ?? throw new ArgumentNullException(nameof(round));

        if (PresentationController == null)
        {
            return;
        }

        PresentationController.SetText(round.Name);
        PresentationController.SetStage(TableStage.Round);
        SetSound(Settings.Model.Sounds.RoundBegin);
        LocalInfo.TStage = TableStage.Round;

        _logger.Write("\r\n{0} {1}", Resources.Round, round.Name);

        if (round.Type == RoundTypes.Standart)
        {
            if (Settings.Model.RoundTime > 0)
            {
                RunRoundTimer_Executed(0);
            }
        }
    }

    private void Engine_RoundThemes(Theme[] roundThemes)
    {
        LocalInfo.RoundInfo.Clear();

        _logger.Write($"{Resources.RoundThemes}:");

        int maxQuestion = roundThemes.Max(theme => theme.Questions.Count);
        foreach (var theme in roundThemes)
        {
            var themeInfo = new ThemeInfoViewModel { Name = theme.Name };
            LocalInfo.RoundInfo.Add(themeInfo);

            _logger.Write(theme.Name);

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
        PresentationController.SetStage(TableStage.RoundTable);

        if (!LocalInfo.Players.Any())
        {
            return false;
        }

        var minSum = LocalInfo.Players.Min(p => p.Sum);
        var playersWithMinSum = LocalInfo.Players.Select((p, i) => (p, i)).Where(pair => pair.p.Sum == minSum).ToArray();
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
        RunQuestionTimer_Executed(0);
    }

    public void AskAnswerDirect()
    {
        if (ActiveRound?.Type == RoundTypes.Final)
        {
            SetSound(Settings.Model.Sounds.FinalThink);

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
        _buttonManager?.Stop();
        ActiveMediaCommand = null;
    }

    private void Engine_RoundEmpty() => StopRoundTimer_Executed(0);

    private void Engine_NextQuestion()
    {
        if (Settings.Model.GameMode == GameModes.Tv)
        {
            PresentationController.SetStage(TableStage.RoundTable);
            LocalInfo.TStage = TableStage.RoundTable;
        }
        else
        {
            Next_Executed();
        }
    }

    private void Engine_RoundTimeout()
    {
        SetSound(Settings.Model.Sounds.RoundTimeout);
        _logger?.Write(Resources.RoundTimeout);
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

        _buttonManager?.Stop();

        UnselectPlayer();
        _selectedPlayers.Clear();

        foreach (var player in LocalInfo.Players)
        {
            ((PlayerInfo)player).BlockedTime = null;
        }

        ActiveQuestionCommand = null;
        ActiveMediaCommand = null;

        PresentationController.SetText();
        PresentationController.SetActivePlayerIndex(-1);

        CurrentTheme = null;
        Price = 0;

        _playingQuestionType = false;
        _selectedAnswerIndex = -1;
        LocalInfo.LayoutMode = LayoutMode.Simple;
        LocalInfo.AnswerOptions.Options = Array.Empty<ItemViewModel>();

        DecisionMode = DecisionMode.None;
        NegativePrice = null;
    }

    private void Engine_FinalThemes(Theme[] finalThemes, bool willPlayAllThemes, bool isFirstPlay)
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
    }

    /// <summary>
    /// Moves back.
    /// </summary>
    private void Back_Executed(object? arg = null)
    {
        var data = _engine.MoveBack();

        if (Settings.Model.GameMode == GameModes.Tv)
        {
            LocalInfo.RoundInfo[data.Item1].Questions[data.Item2].Price = data.Item3;

            PresentationController.RestoreQuestion(data.Item1, data.Item2, data.Item3);
            PresentationController.SetStage(TableStage.RoundTable);
            LocalInfo.TStage = TableStage.RoundTable;
        }
        else
        {
            PresentationController.SetText(data.Item3.ToString());
            PresentationController.SetStage(TableStage.QuestionPrice);
        }

        StopQuestionTimer_Executed(0);
        StopThinkingTimer_Executed(0);

        _buttonManager?.Stop();
        State = QuestionState.Normal;
        _previousState = QuestionState.Normal;

        DecisionMode = DecisionMode.None;

        UnselectPlayer();
        _selectedPlayers.Clear();

        foreach (var player in LocalInfo.Players)
        {
            ((PlayerInfo)player).BlockedTime = null;
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

    private void Engine_ThemeSelected(int themeIndex)
    {
        PresentationController.PlaySelection(themeIndex);
        SetSound(Settings.Model.Sounds.FinalDelete);
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
        try
        {
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
                    _logger.Write($"Engine dispose error: {exc}");
                }

                _logger.Dispose();

                PresentationController.SetSound();

                PlatformManager.Instance.ClearMedia();
            }
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowMessage(string.Format(Resources.GameEndingError, exc.Message));
        }
    }

    private void Engine_PrepareFinalQuestion(Theme theme, Question question)
    {
        ActiveTheme = theme;
        ActiveQuestion = question;

        PresentationController.SetSound();
        SetCaption(theme.Name);
    }

    /// <summary>
    /// Writes players scores to log.
    /// </summary>
    private void LogScore()
    {
        if (!Settings.Model.SaveLogs || LocalInfo.Players.Count <= 0)
        {
            return;
        }

        var sb = new StringBuilder("\r\n").Append(Resources.Score).Append(": ");
        var first = true;

        foreach (var player in LocalInfo.Players)
        {
            if (!first)
            {
                sb.Append(", ");
            }

            first = false;
            sb.AppendFormat("{0}:{1}", player.Name, player.Sum);
        }

        _logger?.Write(sb.ToString());
    }

    private void Engine_NextRound(bool showSign)
    {
        ActiveRoundCommand = null;
        PresentationController.SetSound();

        if (showSign)
        {
            PresentationController.SetStage(TableStage.Sign);
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

    private async void Engine_QuestionSelected(int themeIndex, int questionIndex, Theme theme, Question question)
    {
        _answeringHistory.Push(null);

        ActiveTheme = theme;
        ActiveQuestion = question;

        CurrentTheme = theme.Name;
        Price = question.Price;

        LogScore();
        _logger?.Write("\r\n{0}, {1}", theme.Name, question.Price);

        var (played, highlightTheme) = PlayQuestionType();

        if (!played)
        {
            SetSound(Settings.Model.Sounds.QuestionSelected);
            PresentationController.PlaySimpleSelection(themeIndex, questionIndex);

            try
            {
                await Task.Delay(700);
                _engine.MoveNext();
            }
            catch (Exception exc)
            {
                Trace.TraceError("QuestionSelected error: " + exc.Message);
            }
        }
        else
        {
            PresentationController.PlayComplexSelection(themeIndex, questionIndex, highlightTheme);
        }

        _logger.Write(question.GetText());
    }

    private void PrintQuestionType(string originalTypeName, string? aliasName)
    {
        var actualName = string.IsNullOrWhiteSpace(aliasName) ? originalTypeName : aliasName;

        PresentationController.SetText(actualName);
        _logger.Write(actualName);
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
                foreach (PlayerInfo playerInfo in LocalInfo.Players.Cast<PlayerInfo>())
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

        if (index == -1 || index >= LocalInfo.Players.Count)
        {
            return false;
        }

        var player = (PlayerInfo)LocalInfo.Players[index];
        return ProcessPlayerPress(index, player);
    }

    public bool OnPlayerPressed(PlayerInfo player)
    {
        var index = LocalInfo.Players.IndexOf(player);

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

        // It is no pressing time
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

        // Заблокирован
        if (player.BlockedTime.HasValue && DateTime.Now.Subtract(player.BlockedTime.Value).TotalSeconds < Settings.Model.BlockingTime)
        {
            return false;
        }

        // Все проверки пройдены, фиксируем нажатие
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

        return true;
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

    internal void CloseMainView() => PresentationController.StopGame();

    internal void StartButtons() => _buttonManager?.Start();

    internal void AskAnswerButton() => State = QuestionState.Pressing;

    internal void OnQuestionStart()
    {
        State = QuestionState.Normal;
        _previousState = QuestionState.Normal;

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
        var playerCount = LocalInfo.Players.Count;

        if (playerCount == 0)
        {
            return false;
        }

        if (playerCount == 1 && LocalInfo.Players[0] == Chooser)
        {
            if (canSelectChooser)
            {
                Chooser.IsSelected = true;
                _selectedPlayer = Chooser;
            }

            return false;
        }

        if (playerCount == 2 && !canSelectChooser && Chooser != null && LocalInfo.Players.Contains(Chooser))
        {
            var activePlayer = (PlayerInfo)(LocalInfo.Players[0] == Chooser ? LocalInfo.Players[1] : LocalInfo.Players[0]);
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

        var chooserIndex = LocalInfo.Players.IndexOf(Chooser);

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

        var orderedPlayerIndicies = LocalInfo.Players
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

            if (LocalInfo.Players[_stakers[i]].Sum <= Stake)
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
