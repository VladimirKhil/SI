﻿using Microsoft.Extensions.Logging;
using SICore;
using SICore.Contracts;
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
    private readonly ViewerHumanLogic _logic;

    public IViewerClient Host { get; private set; }

    public ViewerData Data => Host.MyData;

    public TableInfoViewModel TInfo => _logic.TInfo;

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

    public GameViewModel(
        Node node,
        IViewerClient host,
        ViewerHumanLogic logic,
        UserSettings userSettings,
        IFileShare? fileShare,
        ILogger<GameViewModel> logger)
    {
        _node = node;
        _logic = logic;
        _fileShare = fileShare;
        _logger = logger;

        _node.Reconnecting += Server_Reconnecting;
        _node.Reconnected += Server_Reconnected;

        Host = host;

        Host.Switch += Host_Switch;
        Host.StageChanged += Host_StageChanged;
        Host.PersonConnected += UpdateMoveCommand;
        Host.PersonDisconnected += UpdateMoveCommand;
        Host.IsHostChanged += UpdateMoveCommand;
        Host.Timer += Host_Timer;
        Host.Ad += Host_Ad;
        Host.IsPausedChanged += Host_IsPausedChanged;

        TInfo.Volume = PlatformManager.Instance.Volume;

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
        Host.MyData.CurrentPlayerChanged += MyData_CurrentPlayerChanged;

        SetVideoAvatar = new SimpleCommand(SetVideoAvatar_Executed);
        DeleteVideoAvatar = new SimpleCommand(DeleteVideoAvatar_Executed);
        ManageTables = new SimpleCommand(ManageTables_Executed);
    }

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

    private void MyData_CurrentPlayerChanged()
    {
        FreeTable.CanBeExecuted = Host.IsHost && Host.MyData.CurrentPerson != null && Host.MyData.CurrentPerson.IsHuman && Host.MyData.CurrentPerson.IsConnected;
        DeleteTable.CanBeExecuted = Host.IsHost && Host.MyData.Players.Count > 2 && Host.MyData.CurrentPerson is PlayerAccount;
        ChangeType.CanBeExecuted = Host.IsHost;
        Replace.CanBeExecuted = Host.IsHost && Host.MyData.CurrentPerson != null && Host.MyData.CurrentPerson.Others != null && Host.MyData.CurrentPerson.Others.Any();
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

    private void Host_StageChanged(GameStage gameStage) => UpdateMoveCommand();

    private void EnableExtrenalMediaLoad_Executed(object? arg)
    {
        UserSettings.LoadExternalMedia = true;
        Host.MyLogic.ReloadMedia();
    }

    private void Host_IsPausedChanged(bool isPaused) => IsPaused = isPaused;

    private void Server_Reconnected()
    {
        Host.AddLog(Resources.ReconnectedMessage);
        Host.GetInfo(); // Invalidate game state
    }

    private void Server_Reconnecting() => Host.AddLog(Resources.ReconnectingMessage);

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

    private void UpdateMoveCommand()
    {
        Move.CanBeExecuted = Data.Stage != GameStage.Before && (Host.IsHost || Host is Showman);
        ChangePauseInGame.CanBeExecuted = Move.CanBeExecuted;
    }

    private void Host_Switch(IViewerClient newHost)
    {
        newHost.Connector = Host.Connector;
        newHost.Connector?.SetHost(newHost);

        Host.Switch -= Host_Switch;
        Host.StageChanged -= Host_StageChanged;
        Host.PersonConnected -= UpdateMoveCommand;
        Host.PersonDisconnected -= UpdateMoveCommand;
        Host.IsHostChanged -= UpdateMoveCommand;
        Host.Timer -= Host_Timer;
        Host.Ad -= Host_Ad;
        Host.IsPausedChanged -= Host_IsPausedChanged;

        Host = newHost;

        Host.Switch += Host_Switch;
        Host.StageChanged += Host_StageChanged;
        Host.PersonConnected += UpdateMoveCommand;
        Host.PersonDisconnected += UpdateMoveCommand;
        Host.IsHostChanged += UpdateMoveCommand;
        Host.Timer += Host_Timer;
        Host.Ad += Host_Ad;
        Host.IsPausedChanged += Host_IsPausedChanged;

        UpdateMoveCommand();

        OnPropertyChanged(nameof(TInfo));
    }

    private void Move_Executed(object? arg) => Host.Move(arg);

    private void Cancel_Executed(object? arg)
    {
        if (Host.MyLogic is IViewerLogic logic)
        {
            ((ViewerData)logic.Data).DialogMode = DialogModes.None;
            ((ViewerData)logic.Data).Hint = "";
        }
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
        await _logic.DisposeAsync();

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
                Trace.TraceWarning($"Temp folder delete error: {exc}");
            }
        }

        if (UseDialogWindow)
        {
            PlatformManager.Instance.CloseDialogWindow();
        }
    }
}
