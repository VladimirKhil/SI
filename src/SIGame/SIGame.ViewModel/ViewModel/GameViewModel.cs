using Microsoft.Extensions.Logging;
using SICore;
using SICore.Contracts;
using SICore.Models;
using SICore.Network.Servers;
using SIData;
using SIGame.ViewModel.Models;
using SIGame.ViewModel.PlatformSpecific;
using SIGame.ViewModel.Properties;
using SIGame.ViewModel.ViewModel.Data;
using SIPackages.Core;
using SIUI.ViewModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Utils;
using Utils.Commands;
using Utils.Timers;

namespace SIGame.ViewModel;

/// <summary>
/// Defines a single game view model.
/// </summary>
public sealed class GameViewModel : IAsyncDisposable, INotifyPropertyChanged
{
    private readonly Node _node;
    public ViewerHumanLogic? Logic { get; set; }

    private IViewerClient? _host;

    public IViewerClient? Host
    {
        get => _host;
        set { _host = value; UpdateCommands(); }
    }

    private readonly ViewerData _data;

    public ViewerData Data => _data;

    public TableInfoViewModel TInfo { get; private set; }

    /// <summary>
    /// Ends the game and returns to main menu/lobby.
    /// </summary>
    public SimpleCommand EndGame { get; }

    /// <summary>
    /// Задать/убрать паузу в игре
    /// </summary>
    public SimpleCommand ChangePauseInGame { get; }

    public SimpleCommand Move { get; }

    /// <summary>
    /// Open provided link.
    /// </summary>
    public ICommand OpenLink { get; private set; }

    /// <summary>
    /// Frees table.
    /// </summary>
    public SimpleCommand FreeTable { get; private set; }

    /// <summary>
    /// Deletes player table.
    /// </summary>
    public SimpleCommand DeleteTable { get; private set; }

    /// <summary>
    /// Replaces table person.
    /// </summary>
    public SimpleCommand Replace { get; private set; }

    /// <summary>
    /// Changes table type.
    /// </summary>
    public SimpleCommand ChangeType { get; private set; }

    public ICommand SetVideoAvatar { get; private set; }

    public ICommand DeleteVideoAvatar { get; private set; }

    private bool _networkGame = false;

    public bool NetworkGame
    {
        get => _networkGame;
        set { _networkGame = value; OnPropertyChanged(); }
    }

    private int _networkGamePort;

    public int NetworkGamePort
    {
        get => _networkGamePort;
        set { _networkGamePort = value; OnPropertyChanged(); }
    }

    private bool _isPaused;

    public bool IsPaused
    {
        get => _isPaused;
        set { if (_isPaused != value) { _isPaused = value; OnPropertyChanged(); } }
    }

    public event Action? GameEnded;

    public SimpleCommand Cancel { get; private set; }

    public ICommand Popup { get; private set; }


    public bool IsOnline { get; set; }

    public string? TempDocFolder { get; set; }

    public IAnimatableTimer[] Timers { get; } = new IAnimatableTimer[3];

    private string? _ad;

    public string? Ad
    {
        get => _ad;
        set { _ad = value; OnPropertyChanged(); }
    }

    public UserSettings UserSettings { get; }

    /// <summary>
    /// Sound volume level.
    /// </summary>
    public double Volume
    {
        get => TInfo.Volume * 100;
        set
        {
            var currentValue = TInfo.Volume;
            TInfo.Volume = Math.Min(100, Math.Max(1, value)) / 100;
            PlatformManager.Instance.UpdateVolume(TInfo.Volume / currentValue);
        }
    }

    /// <summary>
    /// Enables loading of external media.
    /// </summary>
    public ICommand EnableExtrenalMediaLoad { get; set; }

    private readonly IFileShare? _fileShare;

    private readonly ILogger<GameViewModel> _logger;

    private bool _useDialogWindow;

    private void DisableDialogWindow() => UseDialogWindow = false;

    public bool UseDialogWindow
    {
        get => _useDialogWindow;
        set
        {
            if (_useDialogWindow != value)
            {
                _useDialogWindow = value;
                OnPropertyChanged();

                if (_useDialogWindow)
                {
                    PlatformManager.Instance.ShowDialogWindow(this, DisableDialogWindow);
                }
            }
        }
    }

    private int _seletedTabIndex;

    public int SeletedTabIndex
    {
        get => _seletedTabIndex;
        set
        {
            if (_seletedTabIndex != value)
            {
                _seletedTabIndex = value;
                OnPropertyChanged();
            }
        }
    }

    public event Action? Disposed;

    private DialogModes _dialogMode = DialogModes.None;

    public DialogModes DialogMode
    {
        get => _dialogMode;
        set { _dialogMode = value; OnPropertyChanged(); }
    }

    private string _hint = "";

    public string Hint
    {
        get => _hint;
        set { _hint = value; OnPropertyChanged(); }
    }

    public ICommand SendAnswer { get; set; }

    public ICommand SendAnswerVersion { get; set; }

    private string _answer = "";

    /// <summary>
    /// Player answer.
    /// </summary>
    public string Answer
    {
        get => _answer;
        set { _answer = value; OnPropertyChanged(); }
    }

    public Queue<ValidationInfo> ValidationQueue { get; } = new();

    private ValidationInfo? _validationInfo = null;

    public ValidationInfo? ValidationInfo
    {
        get => _validationInfo;
        set
        {
            if (_validationInfo != value)
            {
                _validationInfo = value;
                OnPropertyChanged();
            }
        }
    }

    public void AddValidation(string name, string answer)
    {
        ValidationQueue.Enqueue(new ValidationInfo(name, answer));
        ValidationInfo = ValidationQueue.Peek();
    }

    public ValidationInfo PopValidation()
    {
        var info = ValidationQueue.Dequeue();
        ValidationInfo = ValidationQueue.Count > 0 ? ValidationQueue.Peek() : null;
        return info;
    }

    public void ClearValidation()
    {
        ValidationQueue.Clear();
        ValidationInfo = null;
    }

    private SimpleCommand _changeSums;

    /// <summary>
    /// Change players score.
    /// </summary>
    public SimpleCommand ChangeSums
    {
        get => _changeSums;
        set
        {
            if (_changeSums != value)
            {
                _changeSums = value;
                OnPropertyChanged();
            }
        }
    }

    private SimpleCommand _changeActivePlayer;

    /// <summary>
    /// Change active player.
    /// </summary>
    public SimpleCommand ChangeActivePlayer
    {
        get => _changeActivePlayer;
        set
        {
            if (_changeActivePlayer != value)
            {
                _changeActivePlayer = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isShowman;

    public bool IsShowman
    {
        get => _isShowman;
        set
        {
            if (_isShowman != value )
            {
                _isShowman = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isPlayer;

    public bool IsPlayer
    {
        get => _isPlayer;
        set
        {
            if (_isPlayer != value)
            {
                _isPlayer = value;
                OnPropertyChanged();
            }
        }
    }

    private SimpleCommand _kick;

    public SimpleCommand Kick
    {
        get => _kick;
        set
        {
            if (_kick != value)
            {
                _kick = value;
                OnPropertyChanged();
            }
        }
    }

    private SimpleCommand _ban;

    public SimpleCommand Ban
    {
        get => _ban;
        set
        {
            if (_ban != value)
            {
                _ban = value;
                OnPropertyChanged();
            }
        }
    }

    public SimpleCommand SendMessage { get; set; }

    private string _printedText = "";

    public string PrintedText
    {
        get => _printedText;
        set
        {
            if (_printedText != value)
            {
                _printedText = value;
                OnPropertyChanged();

                SendMessage.CanBeExecuted = value.Length > 0;
            }
        }
    }

    private SimpleCommand _setHost;

    public SimpleCommand SetHost
    {
        get => _setHost;
        set
        {
            if (_setHost != value)
            {
                _setHost = value;
                OnPropertyChanged();
            }
        }
    }

    private SimpleCommand _unban;

    public SimpleCommand Unban
    {
        get => _unban;
        set
        {
            if (_unban != value)
            {
                _unban = value;
                OnPropertyChanged();
            }
        }
    }

    private SimpleCommand _forceStart;

    public SimpleCommand ForceStart
    {
        get => _forceStart;
        set
        {
            if (_forceStart != value)
            {
                _forceStart = value;
                OnPropertyChanged();
            }
        }
    }

    private SimpleCommand _addTable;

    public SimpleCommand AddTable
    {
        get => _addTable;
        set
        {
            if (_addTable != value)
            {
                _addTable = value;
                OnPropertyChanged();
            }
        }
    }

    public SimpleCommand PressGameButton { get; }

    private bool _buttonDisabledByGame = false;
    private bool _buttonDisabledByTimer = false;

    private string _gameMetadata = "";

    /// <summary>
    /// Game metadata.
    /// </summary>
    public string GameMetadata
    {
        get => _gameMetadata;
        set { if (_gameMetadata != value) { _gameMetadata = value; OnPropertyChanged(); } }
    }

    public int GameId { get; set; }

    public Uri? HostUri { get; set; }

    internal string? HostKey { get; set; }

    private SICore.Models.JoinMode _joinMode = SICore.Models.JoinMode.AnyRole;

    /// <summary>
    /// Allowed join mode.
    /// </summary>
    public SICore.Models.JoinMode JoinMode
    {
        get => _joinMode;
        set
        {
            if (_joinMode != value)
            {
                _joinMode = value;
                OnPropertyChanged();
                OnJoinModeChanged(value);
            }
        }
    }

    public PersonAccount? Speaker { get; set; }

    public ObservableCollection<PlayerViewModel> Players { get; } = new();

    public ShowmanVM Showman { get; }

    public SimpleCommand SelectPlayer { get; }

    public SimpleCommand SendPass { get; set; }

    public SimpleCommand SendStake { get; set; }

    public SimpleCommand SendVabank { get; set; }

    public SimpleCommand SendNominal { get; set; }

    public SimpleCommand SendPassNew { get; set; }

    public SimpleCommand SendStakeNew { get; set; }

    public SimpleCommand SendAllInNew { get; set; }

    private PlayerSumInfo? _selectedPlayer = null;

    /// <summary>
    /// Currently selected player.
    /// </summary>
    public PlayerSumInfo? SelectedPlayer
    {
        get => _selectedPlayer;
        set
        {
            if (_selectedPlayer != value)
            {
                _selectedPlayer = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand ChangeSums2 { get; set; }

    /// <summary>
    /// Manage game table command.
    /// </summary>
    public SimpleCommand ManageTable { get; }

    public SimpleCommand Apellate { get; }

    public SimpleCommand Pass { get; }

    private int _apellationCount = int.MaxValue;

    public int ApellationCount
    {
        get => _apellationCount;
        set { _apellationCount = value; OnPropertyChanged(); }
    }

    public ICommand? IsRight { get; }

    public ICommand IsWrong { get; }

    /// <summary>
    /// Game report.
    /// </summary>
    public SIReport Report { get; } = new();

    public SimpleCommand Ready { get; }

    public SimpleCommand UnReady { get; }

    public event Action? GameButtonPressed;
    public event Action? NextButtonPressed;
    public event Action<string?, string, LogMode>? StringAdding;

    public bool AreAnswersShown
    {
        get => UserSettings.GameSettings.AppSettings.AreAnswersShown;
        set
        {
            if (UserSettings.GameSettings.AppSettings.AreAnswersShown != value)
            {
                UserSettings.GameSettings.AppSettings.AreAnswersShown = value;
                OnPropertyChanged();
            }
        }
    }

    internal SelectionMode SelectionMode { get; set; }

    public bool NewValidation { get; internal set; }

    private IPersonViewModel? _currentPerson;

    /// <summary>
    /// Currently selected person.
    /// </summary>
    public IPersonViewModel? CurrentPerson
    {
        get => _currentPerson;
        set { if (_currentPerson != value) { _currentPerson = value; OnPropertyChanged(); UpdateCurrentPlayerCommands(); } }
    }

    public GameViewModel(
        ViewerData viewerData,
        Node node,
        UserSettings userSettings,
        SettingsViewModel settings,
        IFileShare? fileShare,
        ILogger<GameViewModel> logger)
    {
        _node = node;
        _fileShare = fileShare;
        _logger = logger;

        _data = viewerData;

        TInfo = new TableInfoViewModel(Data.TInfo, settings)
        {
            AnimateText = true,
            Enabled = true,
            Volume = PlatformManager.Instance.Volume
        };

        IsShowman = Host?.Role == GameRole.Showman;
        IsPlayer = Host?.Role == GameRole.Player;

        Showman = new ShowmanVM(viewerData.ShowMan);

        _node.Reconnecting += Server_Reconnecting;
        _node.Reconnected += Server_Reconnected;

        UserSettings = userSettings;

        ChangePauseInGame = new SimpleCommand(ChangePauseInGame_Executed) { CanBeExecuted = false };
        Move = new SimpleCommand(Move_Executed) { CanBeExecuted = false };
        EndGame = new SimpleCommand(EndGame_Executed);
        Cancel = new SimpleCommand(Cancel_Executed);
        Popup = new SimpleCommand(arg => UseDialogWindow = true);

        EnableExtrenalMediaLoad = new SimpleCommand(EnableExtrenalMediaLoad_Executed);
        FreeTable = new SimpleCommand(FreeTable_Executed) { CanBeExecuted = false };
        DeleteTable = new SimpleCommand(DeleteTable_Executed) { CanBeExecuted = false };
        ChangeType = new SimpleCommand(ChangeType_Executed) { CanBeExecuted = false };
        Replace = new SimpleCommand(Replace_Executed) { CanBeExecuted = false };

        for (int i = 0; i < Timers.Length; i++)
        {
            Timers[i] = PlatformManager.Instance.GetAnimatableTimer();
        }

        Timers[1].TimeChanged += GameViewModel_TimeChanged;

        OpenLink = new SimpleCommand(OpenLink_Executed);

        SetVideoAvatar = new SimpleCommand(SetVideoAvatar_Executed);
        DeleteVideoAvatar = new SimpleCommand(DeleteVideoAvatar_Executed);

        SendAnswer = new SimpleCommand(SendAnswer_Executed);
        SendAnswerVersion = new SimpleCommand(SendAnswerVersion_Executed);
        _changeSums = new SimpleCommand(ChangeSums_Executed) { CanBeExecuted = IsShowman };
        _changeActivePlayer = new SimpleCommand(ChangeActivePlayer_Executed) { CanBeExecuted = IsShowman };

        _kick = new SimpleCommand(Kick_Executed);
        _ban = new SimpleCommand(Ban_Executed);
        _setHost = new SimpleCommand(SetHost_Executed);
        _unban = new SimpleCommand(Unban_Executed);

        SendMessage = new SimpleCommand(SendMessage_Executed) { CanBeExecuted = false };
        _forceStart = new SimpleCommand(ForceStart_Executed);

        _addTable = new SimpleCommand(AddTable_Executed);

        PressGameButton = new SimpleCommand(PressGameButton_Execute) { CanBeExecuted = IsPlayer };

        SendPass = new SimpleCommand(SendPass_Executed);
        SendStake = new SimpleCommand(SendStake_Executed);
        SendVabank = new SimpleCommand(SendVabank_Executed);
        SendNominal = new SimpleCommand(SendNominal_Executed);

        SendPassNew = new SimpleCommand(SendPassNew_Executed);
        SendStakeNew = new SimpleCommand(SendStakeNew_Executed);
        SendAllInNew = new SimpleCommand(SendAllInNew_Executed);

        ChangeSums2 = new SimpleCommand(ChangeSums2_Executed);
        ManageTable = new SimpleCommand(ManageTable_Executed) { CanBeExecuted = false };

        Apellate = new SimpleCommand(Apellate_Executed) { CanBeExecuted = false };
        Pass = new SimpleCommand(Pass_Executed) { CanBeExecuted = IsPlayer };

        IsRight = new SimpleCommand(IsRight_Executed);
        IsWrong = new SimpleCommand(IsWrong_Executed);

        Report.Title = Resources.ReportTitle;
        Report.Subtitle = Resources.ReportTip;

        Report.SendReport = new SimpleCommand(SendReport_Executed);
        Report.SendNoReport = new SimpleCommand(SendNoReport_Executed);

        Ready = new SimpleCommand(Ready_Executed) { CanBeExecuted = IsPlayer || IsShowman };
        UnReady = new SimpleCommand(UnReady_Executed) { CanBeExecuted = IsPlayer || IsShowman };

        SelectPlayer = new SimpleCommand(SelectPlayer_Executed);
    }

    private void SelectPlayer_Executed(object? arg)
    {
        if (arg is not PlayerViewModel player || !player.Model.CanBeSelected)
        {
            return;
        }

        var playerIndex = _data.Players.IndexOf(player.Model);

        if (playerIndex < 0 || playerIndex >= _data.Players.Count)
        {
            return;
        }

        switch (SelectionMode)
        {
            case SelectionMode.SelectPlayer:
                Host?.Actions.SendMessageWithArgs(Messages.SelectPlayer, playerIndex);
                ClearSelections();
                break;

            case SelectionMode.SetActivePlayer:
                Host?.Actions.SendMessageWithArgs(Messages.SetChooser, playerIndex);
                ClearSelections();
                break;

            case SelectionMode.ChangeSums:
                SelectedPlayer = new PlayerSumInfo { PlayerIndex = playerIndex + 1, PlayerScore = player.Model.Sum };
                DialogMode = DialogModes.ChangeSum;

                for (int j = 0; j < Data.Players.Count; j++)
                {
                    Data.Players[j].CanBeSelected = false;
                }

                Hint = Resources.HintChangeSum;
                break;
        }
    }

    public void OnAddString(string? person, string text, LogMode mode) => StringAdding?.Invoke(person, text, mode);

    public void AddLog(string s) => StringAdding?.Invoke(null, s, LogMode.Log);

    private void Ready_Executed(object? arg) => Host?.Actions.SendMessage(Messages.Ready);

    private void UnReady_Executed(object? arg) => Host?.Actions.SendMessage(Messages.Ready, "-");

    private void IsRight_Executed(object? arg)
    {
        if (ValidationQueue.Count == 0)
        {
            return;
        }

        var validation = PopValidation();
        
        if (NewValidation)
        {
            Host?.Actions.SendMessage(Messages.Validate, validation.Answer, "+");

            if (ValidationInfo == null)
            {
                ClearSelections();
            }
        }
        else
        {
            Host?.Actions.SendMessage(Messages.IsRight, "+", arg?.ToString() ?? "1");
            ClearSelections();
        }
    }

    private void IsWrong_Executed(object? arg)
    {
        if (ValidationQueue.Count == 0)
        {
            return;
        }

        var validation = PopValidation();
        
        if (NewValidation)
        {
            Host?.Actions.SendMessage(Messages.Validate, validation.Answer, "-");

            if (ValidationInfo == null)
            {
                ClearSelections();
            }
        }
        else
        {
            Host?.Actions.SendMessage(Messages.IsRight, "-", arg?.ToString() ?? "1");
            ClearSelections();
        }
    }

    private void Pass_Executed(object? arg) => Host?.Actions.SendMessage(Messages.Pass);

    private void SendNoReport_Executed(object? arg)
    {
        Host?.Actions.SendMessage(Messages.Report, "DECLINE");
        ClearSelections();
    }

    private void SendReport_Executed(object? arg)
    {
        if (_data.SystemLog.Length > 0)
        {
            Host?.Actions.SendMessage(Messages.Report, MessageParams.Report_Log, _data.SystemLog.ToString());
        }

        Host?.Actions.SendMessage(Messages.Report, "ACCEPT", Report.Comment);
        ClearSelections();
    }

    private void Apellate_Executed(object? arg)
    {
        _apellationCount--;
        Host?.Actions.SendMessage(Messages.Apellate, arg?.ToString() ?? "");
    }

    private void ManageTable_Executed(object? arg) => TInfo.IsEditable = !TInfo.IsEditable;

    private void ChangeSums2_Executed(object? arg)
    {
        if (SelectedPlayer == null)
        {
            return;
        }

        Host?.Actions.SendMessageWithArgs(Messages.Change, SelectedPlayer.PlayerIndex, SelectedPlayer.PlayerScore);
        ClearSelections();
    }

    private void SendPass_Executed(object? arg)
    {
        Host?.Actions.SendMessageWithArgs(Messages.Stake, 2);
        ClearSelections();
    }

    private void SendStake_Executed(object? arg)
    {
        Host?.Actions.SendMessageWithArgs(Messages.Stake, 1, Host.MyData.PersonDataExtensions.StakeInfo.Stake);
        ClearSelections();
    }

    private void SendVabank_Executed(object? arg)
    {
        Host?.Actions.SendMessageWithArgs(Messages.Stake, 3);
        ClearSelections();
    }

    private void SendNominal_Executed(object? arg)
    {
        Host?.Actions.SendMessageWithArgs(Messages.Stake, 0);
        ClearSelections();
    }

    private void SendPassNew_Executed(object? arg)
    {
        Host?.Actions.SendMessageWithArgs(Messages.SetStake, SICore.Models.StakeModes.Pass);
        ClearSelections();
    }

    private void SendStakeNew_Executed(object? arg)
    {
        Host?.Actions.SendMessageWithArgs(Messages.SetStake, SICore.Models.StakeModes.Stake, Host.MyData.PersonDataExtensions.StakeInfo.Stake);
        ClearSelections();
    }

    private void SendAllInNew_Executed(object? arg)
    {
        Host?.Actions.SendMessageWithArgs(Messages.SetStake, SICore.Models.StakeModes.AllIn);
        ClearSelections();
    }

    internal void ClearReplic()
    {
        if (Speaker != null)
        {
            Speaker.Replic = "";
        }
    }

    private void OnJoinModeChanged(SICore.Models.JoinMode joinMode) =>
        Host?.Actions.SendMessage(Messages.SetJoinMode, joinMode.ToString());

    private void PressGameButton_Execute(object? arg)
    {
        if (Host == null)
        {
            return;
        }

        Host.Actions.PressButton(Host.MyData.TryStartTime);
        GameButtonPressed?.Invoke();
        DisableGameButton(false);
        ReleaseGameButton();
    }

    internal void DisableGameButton(bool byGame)
    {
        PressGameButton.CanBeExecuted = false;

        if (byGame)
        {
            _buttonDisabledByGame = true;
        }
        else
        {
            _buttonDisabledByTimer = true;
        }
    }

    internal void EnableGameButton(bool byGame)
    {
        if (byGame)
        {
            _buttonDisabledByGame = false;
        }
        else
        {
            _buttonDisabledByTimer = false;
        }

        if (!_buttonDisabledByGame && !_buttonDisabledByTimer)
        {
            PressGameButton.CanBeExecuted = IsPlayer;
        }
    }

    internal async void ReleaseGameButton()
    {
        if (Host == null)
        {
            return;
        }

        try
        {
            await Task.Delay(Host.MyData.ButtonBlockingTime * 1000);
            EnableGameButton(false);
        }
        catch (Exception exc)
        {
            _logger.LogWarning(exc, "ReleaseGameButton error: {error}", exc.Message);
        }
    }

    private void AddTable_Executed(object? arg) => Host?.Actions.SendMessage(Messages.Config, MessageParams.Config_AddTable);

    internal void UpdateAddTableCommand() =>
        AddTable.CanBeExecuted = Host != null && Host.IsHost && Host.MyData.Players.Count < Constants.MaxPlayers;

    private void ForceStart_Executed(object? arg) => Host?.Actions.SendMessage(Messages.Start);

    private void Unban_Executed(object? arg)
    {
        if (arg is not BannedInfo bannedInfo)
        {
            return;
        }

        Host?.Actions.SendMessage(Messages.Unban, bannedInfo.Ip);
    }

    private void SetHost_Executed(object? arg)
    {
        if (arg is not ViewerAccount person)
        {
            return;
        }

        if (person.Name == Data.Name)
        {
            AddLog(Resources.CannotSetHostToYourself);
            return;
        }

        if (!person.IsHuman)
        {
            AddLog(Resources.CannotSetHostToBot);
            return;
        }

        Host?.Actions.SendMessage(Messages.SetHost, person.Name);
    }

    private void SendMessage_Executed(object? arg)
    {
        Host?.Say(PrintedText);
        PrintedText = "";
    }

    private void Ban_Executed(object? arg)
    {
        if (arg is not ViewerAccount person)
        {
            return;
        }

        if (person.Name == Data.Name)
        {
            AddLog(Resources.CannotBanYourself);
            return;
        }

        if (!person.IsHuman)
        {
            AddLog(Resources.CannotBanBots);
            return;
        }

        Host?.Actions.SendMessage(Messages.Ban, person.Name);
    }

    private void Kick_Executed(object? arg)
    {
        if (arg is not ViewerAccount person)
        {
            return;
        }

        if (person.Name == Data.Name)
        {
            AddLog(Resources.CannotKickYouself);
            return;
        }

        if (!person.IsHuman)
        {
            AddLog(Resources.CannotKickBots);
            return;
        }

        Host?.Actions.SendMessage(Messages.Kick, person.Name);
    }

    public void OnMediaContentCompleted(string contentType, string contentValue) =>
        Host?.Actions.SendMessageWithArgs(Messages.Atom, contentType, contentValue);

    private void ChangeActivePlayer_Executed(object? arg)
    {
        for (var i = 0; i < Data.Players.Count; i++)
        {
            Data.Players[i].CanBeSelected = true;
        }

        Hint = Resources.HintSelectActivePlayer;
        SelectionMode = SelectionMode.SetActivePlayer;
    }

    internal void ClearSelections(bool full = false)
    {
        if (full)
        {
            TInfo.Selectable = false;
            TInfo.SelectQuestion.CanBeExecuted = false;
            TInfo.SelectTheme.CanBeExecuted = false;
            TInfo.SelectAnswer.CanBeExecuted = false;
        }

        Hint = "";
        DialogMode = DialogModes.None;
        ClearValidation();

        for (var i = 0; i < _data.Players.Count; i++)
        {
            _data.Players[i].CanBeSelected = false;
        }

        _data.Host.OnFlash(false);
    }

    private void ChangeSums_Executed(object? arg)
    {
        for (var i = 0; i < Data.Players.Count; i++)
        {
            Data.Players[i].CanBeSelected = true;
        }

        Hint = Resources.HintSelectPlayerForSumChange;
        SelectionMode = SelectionMode.ChangeSums;
    }

    private void SendAnswer_Executed(object? arg)
    {
        _host?.Actions.SendMessage(Messages.Answer, (string?)arg ?? Answer);
        Hint = "";
        DialogMode = DialogModes.None;
    }

    private void SendAnswerVersion_Executed(object? arg) => _host?.Actions.SendMessage(Messages.AnswerVersion, Answer);

    private void SetVideoAvatar_Executed(object? arg)
    {
        var avatarUri = PlatformManager.Instance.AskText(Resources.SetVideoAvatar);

        if (avatarUri != null)
        {
            Host?.Actions.SendMessageWithArgs(Messages.Avatar, ContentTypes.Video, avatarUri);
        }
    }

    private void DeleteVideoAvatar_Executed(object? arg) => Host?.Actions.SendMessageWithArgs(Messages.Avatar, ContentTypes.Video, "");

    private void UpdateCurrentPlayerCommands()
    {
        if (Host == null)
        {
            return;
        }

        FreeTable.CanBeExecuted = Host.IsHost && CurrentPerson != null && CurrentPerson.IsHuman && CurrentPerson.IsConnected;
        DeleteTable.CanBeExecuted = Host.IsHost && Host.MyData.Players.Count > 2 && CurrentPerson != null && CurrentPerson.IsPlayer;
        ChangeType.CanBeExecuted = Host.IsHost;
        Replace.CanBeExecuted = Host.IsHost && CurrentPerson != null && CurrentPerson.Others != null && CurrentPerson.Others.Any();
        Kick.CanBeExecuted = Ban.CanBeExecuted = SetHost.CanBeExecuted = Unban.CanBeExecuted = Host.IsHost;
    }

    private void FreeTable_Executed(object? arg)
    {
        if (arg is not IPersonViewModel account)
        {
            return;
        }

        var player = account as PlayerViewModel;

        var indexString = "";

        if (player != null)
        {
            var index = _data.Players.IndexOf(player.Model);

            if (index < 0 || index >= _data.Players.Count)
            {
                return;
            }

            indexString = index.ToString();
        }

        Host?.Actions.SendMessage(
            Messages.Config,
            MessageParams.Config_Free,
            player != null ? Constants.Player : Constants.Showman,
            indexString);
    }

    private void DeleteTable_Executed(object? arg)
    {
        if (arg is not PlayerViewModel player)
        {
            return;
        }

        var playerIndex = _data.Players.IndexOf(player.Model);

        if (playerIndex < 0 || playerIndex >= _data.Players.Count)
        {
            return;
        }

        Host?.Actions.SendMessage(Messages.Config, MessageParams.Config_DeleteTable, playerIndex.ToString());
    }

    private void ChangeType_Executed(object? arg)
    {
        if (arg is not IPersonViewModel account)
        {
            return;
        }

        var player = account as PlayerViewModel;

        var indexString = "";

        if (player != null)
        {
            var index = _data.Players.IndexOf(player.Model);

            if (index < 0 || index >= _data.Players.Count)
            {
                return;
            }

            indexString = index.ToString();
        }

        Host?.Actions.SendMessage(
            Messages.Config,
            MessageParams.Config_ChangeType,
            player != null ? Constants.Player : Constants.Showman,
            indexString);
    }

    private void Replace_Executed(object? arg)
    {
        if (arg is not Account account)
        {
            return;
        }

        var player = CurrentPerson as PlayerViewModel;

        string index;

        if (player != null)
        {
            var playerIndex = _data.Players.IndexOf(player.Model);

            if (playerIndex == -1)
            {
                return;
            }

            index = playerIndex.ToString();
        }
        else
        {
            index = "";
        }

        Host?.Actions.SendMessage(
            Messages.Config,
            MessageParams.Config_Set,
            player != null ? Constants.Player : Constants.Showman,
            index,
            account.Name);
    }

    private void OpenLink_Executed(object? arg)
    {
        if (arg == null)
        {
            throw new ArgumentNullException(nameof(arg));
        }

        var link = arg.ToString();

        if (string.IsNullOrEmpty(link))
        {
            return;
        }

        try
        {
            Browser.Open(link);
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowMessage(
                string.Format(Resources.ErrorMovingToSite, exc.Message),
                MessageType.Error);
        }
    }

    private void EnableExtrenalMediaLoad_Executed(object? arg)
    {
        UserSettings.LoadExternalMedia = true;
        Host?.MyLogic.ReloadMedia();
    }

    public void OnIsPausedChanged(bool isPaused) => IsPaused = isPaused;

    private void Server_Reconnected()
    {
        AddLog(Resources.ReconnectedMessage);
        Host?.GetInfo(); // Invalidate game state
    }

    private void Server_Reconnecting() => AddLog(Resources.ReconnectingMessage);

    private void GameViewModel_TimeChanged(IAnimatableTimer timer) =>
        TInfo.TimeLeft = timer.Time < 0.001 ? 0.0 : 1.0 - timer.Time / 100;

    public void OnTimerChanged(int timerIndex, string timerCommand, string arg)
    {
        var timer = Timers[timerIndex];

        switch (timerCommand)
        {
            case MessageParams.Timer_Go:
                var maxTime = int.Parse(arg);
                timer.Run(maxTime, false);
                break;

            case MessageParams.Timer_Stop:
                timer.Stop();
                break;

            case MessageParams.Timer_Pause:
                var currentTime = int.Parse(arg);
                timer.Pause(currentTime, false);
                break;

            case MessageParams.Timer_UserPause:
                var currentTime2 = int.Parse(arg);
                timer.Pause(currentTime2, true);
                break;

            case MessageParams.Timer_Resume:
                timer.Run(-1, false);
                break;

            case MessageParams.Timer_UserResume:
                double? fromValue = int.TryParse(arg, out var passedTime) ? 100.0 * passedTime / timer.MaxTime : null;
                timer.Run(-1, true, fromValue);
                break;

            case MessageParams.Timer_MaxTime:
                var maxTime2 = int.Parse(arg);
                timer.MaxTime = maxTime2;
                break;
        }
    }

    public void UpdateCommands()
    {
        IsShowman = Host?.Role == GameRole.Showman;
        IsPlayer = Host?.Role == GameRole.Player;
        Move.CanBeExecuted = Data.Stage != GameStage.Before && (Host != null && Host.IsHost || IsShowman);
        ChangePauseInGame.CanBeExecuted = Move.CanBeExecuted;
        _changeSums.CanBeExecuted = _changeActivePlayer.CanBeExecuted = IsShowman;
        ForceStart.CanBeExecuted = Host != null && Host.IsHost && Host.MyData.Stage == GameStage.Before;
        PressGameButton.CanBeExecuted = Pass.CanBeExecuted = IsPlayer;
        Ready.CanBeExecuted = UnReady.CanBeExecuted = IsPlayer || IsShowman;
        Apellate.CanBeExecuted &= IsPlayer;

        UpdateAddTableCommand();
        UpdateCurrentPlayerCommands();
    }

    private void Move_Executed(object? arg)
    {
        if (arg == null)
        {
            return;
        }

        Host?.Move(arg);
        
        if (Equals(arg, 1))
        {
            NextButtonPressed?.Invoke();
        }
    }

    private void Cancel_Executed(object? arg)
    {
        DialogMode = DialogModes.None;
        Hint = "";
        ClearValidation();
    }

    private void ChangePauseInGame_Executed(object? arg) => Host?.Pause();

    private void EndGame_Executed(object? arg) => GameEnded?.Invoke();

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void Init()
    {
        if (NetworkGame)
        {
            PrintNetworkInformation();
        }
        else if (IsOnline)
        {
            PrintOnlineInformation();
        }
    }

    private async void PrintOnlineInformation()
    {
        await Task.Delay(4000);

        try
        {
            string uriParams;
            
            if (HostKey != null)
            {
                uriParams = $"_{ HostKey}{GameId}";
            }
            else if (HostUri != null)
            {
                var hostInfo = "&host=" + Uri.EscapeDataString(HostUri.ToString());
                uriParams = $"gameId={GameId}{hostInfo}";
            }
            else
            {
                uriParams = $"gameId={GameId}";
            }

            AddLog($"{Resources.OnlineGameAddress}: {CommonSettings.NewOnlineGameUrl}{uriParams}");
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "PrintOnlineInformation error");
        }
    }

    private async void PrintNetworkInformation(CancellationToken cancellationToken = default)
    {
        var ips = new List<string>();

        try
        {
            using var client = new HttpClient { DefaultRequestVersion = HttpVersion.Version20 };
            var ipInfo = await client.GetFromJsonAsync<IpResponse>("https://api.ipify.org?format=json", cancellationToken);

            if (ipInfo.Ip != null)
            {
                ips.Add($"{ipInfo.Ip}:{NetworkGamePort}");
            }
        }
        catch (Exception exc)
        {
            _logger.LogWarning(exc, "Error while getting network information");
        }

        try
        {
            foreach (var ip in await Dns.GetHostAddressesAsync(Environment.MachineName, cancellationToken))
            {
                ips.Add($"{ip}:{NetworkGamePort}");
            }
        }
        catch (Exception exc)
        {
            _logger.LogWarning(exc, "Error while getting dns information");
        }

        if (!ips.Any())
        {
            return;
        }

        await Task.Delay(2000, cancellationToken);

        AddLog($"IP:\n{string.Join("\n", ips)}");
    }

    public async ValueTask DisposeAsync()
    {
        // For correct WebView2 disposal (https://github.com/MicrosoftEdge/WebView2Feedback/issues/1136)
        TInfo.TStage = SIUI.ViewModel.TableStage.Void;

        await _node.DisposeAsync();

        if (_fileShare != null)
        {
            await _fileShare.DisposeAsync();
        }

        Timers[1].TimeChanged -= GameViewModel_TimeChanged;

        for (int i = 0; i < Timers.Length; i++)
        {
            Timers[i].Dispose();
        }

        if (TempDocFolder != null && Directory.Exists(TempDocFolder))
        {
            try
            {
                Directory.Delete(TempDocFolder, true);
            }
            catch (IOException exc)
            {
                _logger.LogWarning(exc, "Temp folder delete error");
            }
        }

        if (UseDialogWindow)
        {
            PlatformManager.Instance.CloseDialogWindow();
        }

        Disposed?.Invoke();
    }
}
