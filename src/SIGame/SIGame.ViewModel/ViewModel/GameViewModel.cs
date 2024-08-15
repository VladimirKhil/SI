using Microsoft.Extensions.Logging;
using SICore;
using SICore.Clients.Viewer;
using SICore.Contracts;
using SICore.Network.Clients;
using SICore.Network.Servers;
using SIData;
using SIGame.ViewModel.PlatformSpecific;
using SIGame.ViewModel.Properties;
using SIGame.ViewModel.ViewModel.Data;
using SIPackages.Core;
using SIUI.ViewModel;
using System.ComponentModel;
using System.Diagnostics;
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
        set
        {
            if (_host != null)
            {
                _host.Switch -= Host_Switch;
                _host.StageChanged -= Host_StageChanged;
                _host.PersonConnected -= UpdateCommands;
                _host.PersonDisconnected -= UpdateCommands;
                _host.IsHostChanged -= UpdateCommands;
                _host.Timer -= Host_Timer;
                _host.Ad -= Host_Ad;
                _host.IsPausedChanged -= Host_IsPausedChanged;
            }

            _host = value;

            if (_host != null)
            {
                _host.Switch += Host_Switch;
                _host.StageChanged += Host_StageChanged;
                _host.PersonConnected += UpdateCommands;
                _host.PersonDisconnected += UpdateCommands;
                _host.IsHostChanged += UpdateCommands;
                _host.Timer += Host_Timer;
                _host.Ad += Host_Ad;
                _host.IsPausedChanged += Host_IsPausedChanged;
            }
        }
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

    public SimpleCommand Move { get; set; }

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

    public ICommand ManageTables { get; private set; }

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

    private SimpleCommand _atomViewed;

    public SimpleCommand AtomViewed
    {
        get => _atomViewed;
        set
        {
            if (_atomViewed != value)
            {
                _atomViewed = value;
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

    private SimpleCommand _pressGameButton;

    public SimpleCommand PressGameButton
    {
        get => _pressGameButton;
        set
        {
            if (_pressGameButton != value)
            {
                _pressGameButton = value;
                OnPropertyChanged();
            }
        }
    }

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
        _data.CurrentPlayerChanged += UpdateCurrentPlayerCommands;

        TInfo = new TableInfoViewModel(Data.TInfo, settings)
        {
            AnimateText = true,
            Enabled = true,
            Volume = PlatformManager.Instance.Volume
        };

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
        ManageTables = new SimpleCommand(ManageTables_Executed);

        SendAnswer = new SimpleCommand(SendAnswer_Executed);
        SendAnswerVersion = new SimpleCommand(SendAnswerVersion_Executed);
        IsShowman = Host?.Role == GameRole.Showman;
        _changeSums = new SimpleCommand(ChangeSums_Executed) { CanBeExecuted = IsShowman };
        _changeActivePlayer = new SimpleCommand(ChangeActivePlayer_Executed) { CanBeExecuted = IsShowman };

        _atomViewed = new SimpleCommand(AtomViewed_Executed);
        _kick = new SimpleCommand(Kick_Executed);
        _ban = new SimpleCommand(Ban_Executed);
        _setHost = new SimpleCommand(SetHost_Executed);
        _unban = new SimpleCommand(Unban_Executed);

        SendMessage = new SimpleCommand(SendMessage_Executed) { CanBeExecuted = false };
        _forceStart = new SimpleCommand(ForceStart_Executed);

        _addTable = new SimpleCommand(AddTable_Executed);
        
        _pressGameButton = new SimpleCommand(PressGameButton_Execute) { CanBeExecuted = Host?.Role == GameRole.Player };
    }

    private void PressGameButton_Execute(object? arg)
    {
        if (Host == null)
        {
            return;
        }

        Host.Actions.PressButton(Host.MyData.PlayerDataExtensions.TryStartTime);
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
            PressGameButton.CanBeExecuted = true;
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
            Host?.AddLog(Resources.CannotSetHostToYourself);
            return;
        }

        if (!person.IsHuman)
        {
            Host?.AddLog(Resources.CannotSetHostToBot);
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
            Host?.AddLog(Resources.CannotBanYourself);
            return;
        }

        if (!person.IsHuman)
        {
            Host?.AddLog(Resources.CannotBanBots);
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
            Host?.AddLog(Resources.CannotKickYouself);
            return;
        }

        if (!person.IsHuman)
        {
            Host?.AddLog(Resources.CannotKickBots);
            return;
        }

        Host?.Actions.SendMessage(Messages.Kick, person.Name);
    }

    private void AtomViewed_Executed(object? arg) => Host?.Actions.SendMessage(Messages.Atom);

    private void ChangeActivePlayer_Executed(object? arg)
    {
        for (int i = 0; i < Data.Players.Count; i++)
        {
            Data.Players[i].CanBeSelected = true;
            int num = i;

            Data.Players[i].SelectionCallback = player =>
            {
                Host?.Actions.SendMessageWithArgs(Messages.SetChooser, num);
                ClearSelections();
            };
        }

        Hint = Resources.HintSelectActivePlayer;
    }

    private void ClearSelections(bool full = false)
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

        for (int i = 0; i < _data.Players.Count; i++)
        {
            _data.Players[i].CanBeSelected = false;
        }

        _data.Host.OnFlash(false);
    }

    private void ChangeSums_Executed(object? arg)
    {
        for (int i = 0; i < Data.Players.Count; i++)
        {
            Data.Players[i].CanBeSelected = true;
            int num = i;

            Data.Players[i].SelectionCallback = player =>
            {
                Data.ShowmanDataExtensions.SelectedPlayer = new Pair { First = num + 1, Second = player.Sum };
                DialogMode = DialogModes.ChangeSum;

                for (int j = 0; j < Data.Players.Count; j++)
                {
                    Data.Players[j].CanBeSelected = false;
                }

                Hint = Resources.HintChangeSum;
            };
        }

        Hint = Resources.HintSelectPlayerForSumChange;
    }

    private void SendAnswer_Executed(object? arg)
    {
        _host?.Actions.SendMessage(Messages.Answer, (string?)arg ?? Answer);
        Hint = "";
        DialogMode = DialogModes.None;
    }

    private void SendAnswerVersion_Executed(object? arg) => _host?.Actions.SendMessage(Messages.AnswerVersion, Answer);

    private void ManageTables_Executed(object? arg)
    {
        Data.IsChatOpened = true;
        SeletedTabIndex = 2;
    }

    private void SetVideoAvatar_Executed(object? arg)
    {
        var avatarUri = PlatformManager.Instance.AskText(Resources.SetVideoAvatar);

        if (avatarUri != null)
        {
            Host.Actions.SendMessageWithArgs(Messages.Avatar, ContentTypes.Video, avatarUri);
        }
    }

    private void DeleteVideoAvatar_Executed(object? arg) => Host.Actions.SendMessageWithArgs(Messages.Avatar, ContentTypes.Video, "");

    private void UpdateCurrentPlayerCommands()
    {
        if (Host == null)
        {
            return;
        }

        FreeTable.CanBeExecuted = Host.IsHost && Host.MyData.CurrentPerson != null && Host.MyData.CurrentPerson.IsHuman && Host.MyData.CurrentPerson.IsConnected;
        DeleteTable.CanBeExecuted = Host.IsHost && Host.MyData.Players.Count > 2 && Host.MyData.CurrentPerson is PlayerAccount;
        ChangeType.CanBeExecuted = Host.IsHost;
        Replace.CanBeExecuted = Host.IsHost && Host.MyData.CurrentPerson != null && Host.MyData.CurrentPerson.Others != null && Host.MyData.CurrentPerson.Others.Any();
        Kick.CanBeExecuted = Ban.CanBeExecuted = SetHost.CanBeExecuted = Unban.CanBeExecuted = Host.IsHost;
    }

    private void FreeTable_Executed(object? arg) => Host.MyData.CurrentPerson?.Free.Execute(arg);

    private void DeleteTable_Executed(object? arg) => ((PlayerAccount?)Host.MyData.CurrentPerson)?.Delete.Execute(arg);

    private void ChangeType_Executed(object? arg) => Host.MyData.CurrentPerson?.ChangeType.Execute(arg);

    private void Replace_Executed(object? arg) => Host.MyData.CurrentPerson?.Replace.Execute(arg);

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

    private void Host_StageChanged(GameStage gameStage) => UpdateCommands();

    private void EnableExtrenalMediaLoad_Executed(object? arg)
    {
        UserSettings.LoadExternalMedia = true;
        Host?.MyLogic.ReloadMedia();
    }

    private void Host_IsPausedChanged(bool isPaused) => IsPaused = isPaused;

    private void Server_Reconnected()
    {
        Host?.AddLog(Resources.ReconnectedMessage);
        Host?.GetInfo(); // Invalidate game state
    }

    private void Server_Reconnecting() => Host?.AddLog(Resources.ReconnectingMessage);

    private void Host_Ad(string? text)
    {
        Ad = text;

        if (!string.IsNullOrEmpty(text))
        {
            TInfo.Text = "";
            TInfo.QuestionContentType = QuestionContentType.Text;
            TInfo.Sound = false;
            TInfo.TStage = TableStage.Question;
        }
    }

    private void GameViewModel_TimeChanged(IAnimatableTimer timer) =>
        TInfo.TimeLeft = timer.Time < 0.001 ? 0.0 : 1.0 - timer.Time / 100;

    private void Host_Timer(int timerIndex, string timerCommand, string arg)
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

    private void UpdateCommands()
    {
        IsShowman = Host?.Role == GameRole.Showman;
        Move.CanBeExecuted = Data.Stage != GameStage.Before && (Host != null && Host.IsHost || IsShowman);
        ChangePauseInGame.CanBeExecuted = Move.CanBeExecuted;
        _changeSums.CanBeExecuted = _changeActivePlayer.CanBeExecuted = IsShowman;
        ForceStart.CanBeExecuted = Host != null && Host.IsHost && Host.MyData.Stage == GameStage.Before;
        _pressGameButton.CanBeExecuted = Host?.Role == GameRole.Player;

        UpdateAddTableCommand();
        UpdateCurrentPlayerCommands();
    }

    private void Host_Switch(IViewerClient newHost)
    {
        newHost.Connector = Host.Connector;
        newHost.Connector?.SetHost(newHost);

        Host = newHost;

        UpdateCommands();

        OnPropertyChanged(nameof(TInfo));
    }

    private void Move_Executed(object? arg) => Host.Move(arg);

    private void Cancel_Executed(object? arg)
    {
        DialogMode = DialogModes.None;
        Hint = "";
    }

    private void ChangePauseInGame_Executed(object? arg) => Host.Pause();

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
            var hostUri = Host.Connector?.HostUri;
            var hostInfo = hostUri != null ? "&host=" + Uri.EscapeDataString(hostUri.ToString()) : "";
            Host.AddLog($"{Resources.OnlineGameAddress}: {CommonSettings.NewOnlineGameUrl}{Host.Connector?.GameId}{hostInfo}&invite=true");
        }
        catch (Exception exc)
        {
            Trace.TraceError("PrintOnlineInformation error: " + exc.ToString());
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

        Host.AddLog($"IP:\n{string.Join("\n", ips)}");
    }

    public async ValueTask DisposeAsync()
    {
        // For correct WebView2 disposal (https://github.com/MicrosoftEdge/WebView2Feedback/issues/1136)
        TInfo.TStage = TableStage.Void;

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
