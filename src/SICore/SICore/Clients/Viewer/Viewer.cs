using SICore.BusinessLogic;
using SICore.Clients.Viewer;
using SICore.Models;
using SICore.Network;
using SICore.Network.Clients;
using SICore.Network.Contracts;
using SICore.PlatformSpecific;
using SICore.Special;
using SIData;
using SIPackages.Core;
using SIUI.Model;
using SIUI.ViewModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using R = SICore.Properties.Resources;

namespace SICore;

/// <summary>
/// Implements a game viewer.
/// </summary>
public abstract class Viewer<L> : Actor<ViewerData, L>, IViewerClient, INotifyPropertyChanged
    where L : class, IViewerLogic
{
    protected readonly ViewerActions _viewerActions;

    public event Action? IsHostChanged;

    private bool _isHost;

    /// <summary>
    /// Is current person a game host (has rights to manage the game process).
    /// </summary>
    public bool IsHost
    {
        get => _isHost;

        private set
        {
            if (_isHost != value)
            {
                _isHost = value;
                IsHostChanged?.Invoke();

                if (ClientData.Kick != null)
                {
                    ClientData.Kick.CanBeExecuted = IsHost;
                }

                if (ClientData.Ban != null)
                {
                    ClientData.Ban.CanBeExecuted = IsHost;
                }

                if (ClientData.SetHost != null)
                {
                    ClientData.SetHost.CanBeExecuted = IsHost;
                }

                if (ClientData.Unban != null)
                {
                    ClientData.Unban.CanBeExecuted = IsHost;
                }

                if (ClientData.ForceStart != null)
                {
                    ClientData.ForceStart.CanBeExecuted = IsHost && ClientData.Stage == GameStage.Before;
                }

                foreach (var account in MyData.MainPersons)
                {
                    account.IsExtendedMode = IsHost;
                }

                UpdateAddTableCommand();
                UpdateDeleteTableCommand();
                OnPropertyChanged();
            }
        }
    }

    public IConnector? Connector { get; set; }

    public IViewerLogic MyLogic => _logic;

    public event Action<IViewerClient>? Switch;
    public event Action<GameStage>? StageChanged;
    public event Action<string?>? Ad;
    public event Action<bool>? IsPausedChanged;

    public ViewerData MyData => ClientData;

    public string? Avatar { get; set; }

    public event Action? PersonConnected;
    public event Action? PersonDisconnected;
    public event Action<int, string, string>? Timer;
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Initialize(bool isHost)
    {
        IsHost = isHost;

        ClientData.Name = _client.Name;

        ClientData.EventLog.Append($"Initial name {ClientData.Name}");

        ClientData.MessageSending = msg => Say(msg);
        ClientData.JoinModeChanged += ClientData_JoinModeChanged;

        ClientData.Kick = new CustomCommand(Kick_Executed) { CanBeExecuted = IsHost };
        ClientData.Ban = new CustomCommand(Ban_Executed) { CanBeExecuted = IsHost };
        ClientData.SetHost = new CustomCommand(SetHost_Executed) { CanBeExecuted = IsHost };
        ClientData.Unban = new CustomCommand(Unban_Executed) { CanBeExecuted = IsHost };

        ClientData.ForceStart = new CustomCommand(ForceStart_Executed) { CanBeExecuted = IsHost };
        ClientData.AddTable = new CustomCommand(AddTable_Executed) { CanBeExecuted = IsHost };
        ClientData.DeleteTable = new CustomCommand(DeleteTable_Executed) { CanBeExecuted = IsHost };

        ClientData.AtomViewed = new CustomCommand(arg => _viewerActions.SendMessage(Messages.Atom));
    }

    private void ClientData_JoinModeChanged(JoinMode joinMode) =>
        _viewerActions.SendMessage(Messages.SetJoinMode, joinMode.ToString());

    private void ChangeType_Executed(object arg)
    {
        var account = (PersonAccount)arg;
        var player = account as PlayerAccount;

        _viewerActions.SendMessage(
            Messages.Config,
            MessageParams.Config_ChangeType,
            player != null ? Constants.Player : Constants.Showman,
            player != null ? ClientData.Players.IndexOf(player).ToString() : "");
    }

    private void Replace_Executed(PersonAccount person, object arg)
    {
        var account = (Account)arg;
        var player = person as PlayerAccount;

        string index;

        if (player != null)
        {
            var playerIndex = ClientData.Players.IndexOf(player);

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

        _viewerActions.SendMessage(
            Messages.Config,
            MessageParams.Config_Set,
            player != null ? Constants.Player : Constants.Showman,
            index,
            account.Name);
    }

    private void Free_Executed(object arg)
    {
        var account = (PersonAccount)arg;
        var player = account as PlayerAccount;

        var indexString = "";

        if (player != null)
        {
            var index = ClientData.Players.IndexOf(player);

            if (index < 0 || index >= ClientData.Players.Count)
            {
                AddLog($"Wrong index: {index}" + Environment.NewLine);
                return;
            }

            indexString = index.ToString();
        }

        _viewerActions.SendMessage(
            Messages.Config,
            MessageParams.Config_Free,
            player != null ? Constants.Player : Constants.Showman,
            indexString);
    }

    private void Delete_Executed(object arg)
    {
        var player = (PlayerAccount)arg;
        _viewerActions.SendMessage(Messages.Config, MessageParams.Config_DeleteTable, ClientData.Players.IndexOf(player).ToString());
    }

    /// <summary>
    /// Упрощённый клиент (используется в качестве предка)
    /// </summary>
    protected Viewer(Client client, Account personData, bool isHost, ILocalizer localizer, ViewerData data)
        : base(client, localizer, data)
    {
        if (personData == null)
        {
            throw new ArgumentNullException(nameof(personData));
        }

        _viewerActions = new ViewerActions(client, localizer);
        _logic = CreateLogic(personData);

        Initialize(isHost);

        ClientData.Picture = personData.Picture;
    }

    protected abstract L CreateLogic(Account personData);

    public void Move(object arg) => _viewerActions.SendMessageWithArgs(Messages.Move, arg);

    private void Kick_Executed(object? arg)
    {
        if (arg is not ViewerAccount person)
        {
            return;
        }

        if (person == ClientData.Me)
        {
            AddLog(LO[nameof(R.CannotKickYouself)] + Environment.NewLine);
            return;
        }

        if (!person.IsHuman)
        {
            AddLog(LO[nameof(R.CannotKickBots)] + Environment.NewLine);
            return;
        }

        _viewerActions.SendMessage(Messages.Kick, person.Name);
    }

    private void Ban_Executed(object? arg)
    {
        if (arg is not ViewerAccount person)
        {
            return;
        }

        if (person == ClientData.Me)
        {
            AddLog(LO[nameof(R.CannotBanYourself)] + Environment.NewLine);
            return;
        }

        if (!person.IsHuman)
        {
            AddLog(LO[nameof(R.CannotBanBots)] + Environment.NewLine);
            return;
        }

        _viewerActions.SendMessage(Messages.Ban, person.Name);
    }

    private void SetHost_Executed(object? arg)
    {
        if (arg is not ViewerAccount person)
        {
            return;
        }

        if (person == ClientData.Me)
        {
            AddLog(LO[nameof(R.CannotSetHostToYourself)] + Environment.NewLine);
            return;
        }

        if (!person.IsHuman)
        {
            AddLog(LO[nameof(R.CannotSetHostToBot)] + Environment.NewLine);
            return;
        }

        _viewerActions.SendMessage(Messages.SetHost, person.Name);
    }

    private void Unban_Executed(object? arg)
    {
        if (arg is not BannedInfo bannedInfo)
        {
            return;
        }

        _viewerActions.SendMessage(Messages.Unban, bannedInfo.Ip);
    }

    private void ForceStart_Executed(object? arg) => _viewerActions.SendMessage(Messages.Start);

    private void AddTable_Executed(object? arg) => _viewerActions.SendMessage(Messages.Config, MessageParams.Config_AddTable);

    private void DeleteTable_Executed(object? arg)
    {
        for (var i = 0; i < ClientData.Players.Count; i++)
        {
            var player = ClientData.Players[i];
            player.CanBeSelected = ClientData.Stage == GameStage.Before || !player.IsConnected || !player.IsHuman;
            int num = i;

            player.SelectionCallback = p =>
            {
                _viewerActions.SendMessageWithArgs(Messages.Config, MessageParams.Config_DeleteTable, num);
                Clear();
            };
        }

        ClientData.Hint = LO[nameof(R.DeleteTableHint)];
    }

    private void Clear()
    {
        ClientData.Hint = "";
        ClientData.DialogMode = DialogModes.None;

        for (int i = 0; i < ClientData.Players.Count; i++)
        {
            ClientData.Players[i].CanBeSelected = false;
        }

        ClientData.BackLink.OnFlash(false);
    }

    /// <summary>
    /// Processes received system message.
    /// </summary>
    /// <param name="mparams">Message arguments.</param>
    protected virtual async ValueTask OnSystemMessageReceivedAsync(string[] mparams)
    {
        try
        {
            switch (mparams[0])
            {
                case Messages.Connected:
                    #region Connected
                    {
                        if (mparams[3] == _client.Name)
                        {
                            return;
                        }

                        var role = mparams[1];

                        if (role != Constants.Showman && role != Constants.Player && role != Constants.Viewer)
                        {
                            return;
                        }

                        if (!_client.Node.IsMain)
                        {
                            await _client.Node.ConnectionsLock.WithLockAsync(() =>
                            {
                                var currentConnection = ((ISecondaryNode)_client.Node).HostServer;

                                if (currentConnection != null)
                                {
                                    lock (currentConnection.ClientsSync)
                                    {
                                        currentConnection.Clients.Add(mparams[3]);
                                    }
                                }
                            });
                        }

                        var account = new Account(mparams[3], mparams[4] == "m");
                        _ = int.TryParse(mparams[2], out var index);

                        InsertPerson(role, account, index);

                        PersonConnected?.Invoke();

                        UpdateDeleteTableCommand();

                        break;
                    }
                    #endregion

                case SystemMessages.Disconnect:
                    #region Disconnect
                    {
                        // TODO: Viewer should do nothing for reconnection; that should be handled by underlying connection
                        // For SignalR the reconnection logic is automatic

                        _logic.Print(ReplicManager.Special(LO[nameof(R.DisconnectMessage)]));

                        if (Connector != null && !Connector.IsReconnecting)
                        {
                            _logic.TryConnect(Connector);
                        }
                        break;
                    }
                    #endregion

                case Messages.Disconnected:
                    await OnDisconnectedAsync(mparams);
                    break;

                case Messages.GameMetadata:
                    if (mparams.Length > 3)
                    {
                        _logic.OnGameMetadata(mparams[1], mparams[2], mparams[3], mparams.Length > 4 ? mparams[4] : "");
                    }
                    break;

                case Messages.Info2:
                    await ProcessInfoAsync(mparams);
                    break;

                case Messages.Config:
                    if (ClientData.Me == null)
                    {
                        break;
                    }

                    await ProcessConfigAsync(mparams);
                    break;

                case Messages.ReadingSpeed:
                    {
                        #region ReadingSpeed
                        if (mparams.Length > 1)
                        {
                            if (int.TryParse(mparams[1], out int readingSpeed))
                            {
                                if (readingSpeed > 0)
                                {
                                    _logic.OnTextSpeed(1.0 / readingSpeed);
                                }
                                else
                                {
                                    _logic.OnTextSpeed(0.0);
                                }
                            }
                        }
                        break;
                        #endregion
                    }

                case Messages.ButtonBlockingTime:
                    if (mparams.Length > 1)
                    {
                        if (int.TryParse(mparams[1], out var buttonBlockingTime) && buttonBlockingTime > 0)
                        {
                            ClientData.ButtonBlockingTime = buttonBlockingTime;
                        }
                    }
                    break;

                case Messages.ApellationEnabled:
                    if (mparams.Length > 1)
                    {
                        ClientData.ApellationEnabled = mparams[1] == "+";
                    }
                    break;

                case Messages.Hostname:
                    if (mparams.Length > 1)
                    {
                        ClientData.HostName = mparams[1];
                        IsHost = _client.Name == ClientData.HostName;

                        if (mparams.Length > 2) // Host has been changed
                        {
                            _logic.OnReplic(
                                ReplicCodes.Special.ToString(),
                                string.Format(LO[nameof(R.HostChanged)],
                                    mparams[2].Length > 0 ? mparams[2] : LO[nameof(R.ByGame)],
                                    mparams[1]));
                        }
                    }
                    break;

                case Messages.BannedList:
                    OnBannedList(mparams);
                    break;

                case Messages.Banned:
                    OnBanned(mparams);
                    break;

                case Messages.Unbanned:
                    OnUnbanned(mparams);
                    break;

                case Messages.ComputerAccounts:
                    {
                        ClientData.DefaultComputerPlayers = mparams.Skip(1).Select(name => new Account { Name = name }).ToArray();
                        break;
                    }

                case Messages.SetJoinMode:
                    OnSetJoinMode(mparams);
                    break;

                case Messages.PackageId:
                    {
                        if (mparams.Length > 1)
                        {
                            ClientData.PackageId = mparams[1];
                        }
                        break;
                    }

                case Messages.PackageLogo:
                    _logic.OnPackageLogo(mparams[1]);
                    break;

                case Messages.FalseStart:
                    {
                        if (mparams.Length > 1)
                        {
                            ClientData.FalseStart = mparams[1] != "-";
                        }
                        break;
                    }

                case Messages.Replic:
                    OnReplic(mparams);
                    break;

                case Messages.Pause:
                    {
                        #region Pause

                        var isPaused = mparams[1] == "+";
                        _logic.OnPauseChanged(isPaused);
                        IsPausedChanged?.Invoke(isPaused);

                        if (mparams.Length > 4)
                        {
                            var message = ClientData.TInfo.Pause ? MessageParams.Timer_UserPause : "USER_RESUME";

                            _logic.OnTimerChanged(0, message, mparams[2], null);
                            _logic.OnTimerChanged(1, message, mparams[3], null);
                            _logic.OnTimerChanged(2, message, mparams[4], null);
                            
                            Timer?.Invoke(0, message, mparams[2]);
                            Timer?.Invoke(1, message, mparams[3]);
                            Timer?.Invoke(2, message, mparams[4]);
                        }

                        break;

                        #endregion
                    }

                case Messages.Sums:
                    {
                        #region Sums

                        var players = ClientData.Players;
                        var max = Math.Min(players.Count, mparams.Length - 1);

                        for (int i = 0; i < max; i++)
                        {
                            if (int.TryParse(mparams[i + 1], out int sum))
                                players[i].Sum = sum;
                        }

                        break;

                        #endregion
                    }

                case Messages.Ready:
                    {
                        #region Ready

                        if (ClientData == null || ClientData.Players.Count == 0)
                        {
                            return;
                        }

                        var ready = mparams.Length == 2 || mparams[2] == "+";

                        if (mparams[1] == _client.Name)
                        {
                            ClientData.IReady = ready;
                        }

                        if (ClientData.ShowMan == null)
                        {
                            return;
                        }

                        var person = ClientData.MainPersons.FirstOrDefault(item => item.Name == mparams[1]);
                        if (person != null)
                        {
                            person.Ready = ready;
                        }

                        #endregion
                        break;
                    }

                case Messages.RoundsNames:
                    ClientData.RoundNames = mparams.Skip(1).ToArray();
                    break;

                case Messages.Stage:
                    {
                        #region Stage

                        ClientData.Stage = (GameStage)Enum.Parse(typeof(GameStage), mparams[1]);

                        if (ClientData.Stage != GameStage.Before)
                        {
                            for (int i = 0; i < ClientData.Players.Count; i++)
                            {
                                ClientData.Players[i].GameStarted = true;
                            }

                            if (ClientData.ShowMan != null)
                            {
                                ClientData.ShowMan.GameStarted = true;
                            }

                            ClientData.ForceStart.CanBeExecuted = false;
                        }

                        _logic.SetCaption("");

                        if (mparams.Length > 3)
                        {
                            if (int.TryParse(mparams[3], out var roundIndex))
                            {
                                if (roundIndex > -1 && roundIndex < ClientData.RoundNames.Length)
                                {
                                    ClientData.StageName = ClientData.RoundNames[roundIndex];
                                }
                            }
                        }

                        switch (ClientData.Stage)
                        {
                            case GameStage.Round:
                                _logic.SetText(mparams[2]);

                                for (int i = 0; i < ClientData.Players.Count; i++)
                                {
                                    ClientData.Players[i].InGame = true;
                                    ClientData.Players[i].IsChooser = false;
                                }

                                break;

                            case GameStage.Final:
                                _logic.SetText(mparams[2]);
                                ClientData.AtomType = AtomTypes.Text;
                                break;

                            case GameStage.After:
                                ClientData.BackLink.OnGameFinished(ClientData.PackageId);
                                ClientData.StageName = "";
                                break;
                        }

                        _logic.Stage();
                        StageChanged?.Invoke(ClientData.Stage);
                        OnAd();

                        #endregion
                        break;
                    }

                case Messages.Timer:
                    {
                        var timerIndex = int.Parse(mparams[1]);
                        var timerCommand = mparams[2];

                        _logic.OnTimerChanged(timerIndex, timerCommand, mparams.Length > 3 ? mparams[3] : null, mparams.Length > 4 ? mparams[4] : null);
                        Timer?.Invoke(timerIndex, timerCommand, mparams.Length > 3 ? mparams[3] : null);

                        break;
                    }

                case Messages.GameThemes:
                    {
                        #region GameThemes

                        for (var i = 1; i < mparams.Length; i++)
                        {
                            ClientData.TInfo.GameThemes.Add(mparams[i]);
                            _logic.Print(ReplicManager.System(mparams[i]));
                        }

                        _logic.GameThemes();

                        #endregion
                        break;
                    }

                case Messages.RoundThemes:
                    OnRoundThemes(mparams);
                    break;

                case Messages.RoundContent:
                    _logic.OnRoundContent(mparams);
                    break;

                case Messages.Theme:
                    if (mparams.Length > 1)
                    {
                        _logic.SetText(mparams[1], TableStage.Theme);
                        OnThemeOrQuestion();
                        ClientData.ThemeName = mparams[1];
                    }
                    break;

                case Messages.Question:
                    if (mparams.Length > 1)
                    {
                        _logic.SetText(mparams[1], TableStage.QuestionPrice);
                        OnThemeOrQuestion();
                        _logic.SetCaption($"{ClientData.ThemeName}, {mparams[1]}");
                    }
                    break;

                case Messages.Table:
                    {
                        #region Table

                        lock (ClientData.TInfoLock)
                        {
                            // TODO: clear existing table and renew it
                            if (ClientData.TInfo.RoundInfo.Any(t => t.Questions.Any()))
                            {
                                break;
                            }

                            var index = 1;

                            for (int i = 0; i < ClientData.TInfo.RoundInfo.Count; i++)
                            {
                                if (index == mparams.Length)
                                {
                                    break;
                                }

                                while (index < mparams.Length && mparams[index].Length > 0) // empty value separates the themes content
                                {
                                    if (!int.TryParse(mparams[index++], out int price))
                                    {
                                        price = -1;
                                    }

                                    ClientData.TInfo.RoundInfo[i].Questions.Add(new QuestionInfo { Price = price });
                                }

                                index++;
                            }
                        }

                        _logic.TableLoaded();

                        #endregion
                        break;
                    }

                case Messages.Toggle:
                    OnToggle(mparams);
                    break;

                case Messages.ShowTable:
                    _logic.ShowTablo();
                    break;

                case Messages.Choice:
                    OnChoice(mparams);
                    break;

                case Messages.QuestionCaption:
                    if (mparams.Length > 1)
                    {
                        _logic.SetCaption(mparams[1]);
                    }
                    break;

                case Messages.QType:
                    OnQType(mparams);
                    break;

                case Messages.TextShape:
                    _logic.TextShape(mparams);
                    break;

                case Messages.Atom:
                    _logic.OnScreenContent(mparams);
                    break;

                case Messages.Atom_Hint:
                    if (mparams.Length > 1)
                    {
                        _logic.OnAtomHint(mparams[1]);
                    }
                    break;

                case Messages.Atom_Second:
                    _logic.OnBackgroundContent(mparams);
                    break;

                case Messages.MediaLoaded:
                    OnMediaLoaded(mparams);
                    break;

                case Messages.RightAnswer:
                    _logic.SetRight(mparams[2]);
                    break;

                case Messages.Resume:
                    _logic.Resume();
                    break;

                case Messages.Try:
                    {
                        #region Try

                        if (mparams.Length > 1 && mparams[1] == MessageParams.Try_NotFinished)
                        {
                            // Здесь можно не показывать рамку
                            if (!ClientData.BackLink.ShowBorderOnFalseStart)
                            {
                                return;
                            }
                        }

                        _logic.Try();

                        #endregion
                        break;
                    }
                case Messages.EndTry:
                    {
                        #region EndTry

                        if (mparams[1] == "A")
                        {
                            _logic.OnTimerChanged(1, MessageParams.Timer_Stop, "", null);
                            Timer?.Invoke(1, MessageParams.Timer_Stop, "");
                        }

                        _logic.EndTry(mparams[1]);

                        #endregion
                        break;
                    }
                case Messages.WrongTry:
                    {
                        #region WrongTry
                        var p = int.Parse(mparams[1]);
                        if (p > 0 && p < ClientData.Players.Count)
                        {
                            var player = ClientData.Players[p];
                            if (player.State == PlayerState.None)
                            {
                                player.State = PlayerState.Lost;
                                Task.Run(async () =>
                                {
                                    await Task.Delay(200);
                                    if (player.State == PlayerState.Lost)
                                        player.State = PlayerState.None;
                                });
                            }
                        }
                        #endregion
                        break;
                    }
                case Messages.Person:
                    {
                        #region Person

                        if (mparams.Length < 4)
                        {
                            break;
                        }

                        var isRight = mparams[1] == "+";
                        if (!int.TryParse(mparams[2], out var playerIndex)
                            || playerIndex < 0 || playerIndex >= ClientData.Players.Count)
                        {
                            break;
                        }

                        if (!int.TryParse(mparams[3], out var price))
                        {
                            break;
                        }

                        ClientData.CurPriceWrong = ClientData.CurPriceRight = price;

                        _logic.Person(playerIndex, isRight);

                        #endregion
                        break;
                    }

                case Messages.Pass:
                    _logic.OnPersonPass(int.Parse(mparams[1]));
                    break;

                case Messages.PersonFinalAnswer:
                    {
                        if (mparams.Length > 1 && int.TryParse(mparams[1], out int playerIndex))
                        {
                            _logic.OnPersonFinalAnswer(playerIndex);
                        }
                        break;
                    }
                case Messages.PersonApellated:
                    {
                        if (int.TryParse(mparams[1], out int playerIndex))
                        {
                            _logic.OnPersonApellated(playerIndex);
                        }
                        break;
                    }
                case Messages.PersonFinalStake:
                    {
                        if (int.TryParse(mparams[1], out int playerIndex))
                        {
                            _logic.OnPersonFinalStake(playerIndex);
                        }
                        break;
                    }

                case Messages.PersonStake:
                    OnPersonStake(mparams);
                    break;

                case Messages.Stop:
                    {
                        #region Stop

                        _logic.StopRound();

                        _logic.OnTimerChanged(0, MessageParams.Timer_Stop, "", null);
                        _logic.OnTimerChanged(1, MessageParams.Timer_Stop, "", null);
                        _logic.OnTimerChanged(2, MessageParams.Timer_Stop, "", null);

                        Timer?.Invoke(0, MessageParams.Timer_Stop, "");
                        Timer?.Invoke(1, MessageParams.Timer_Stop, "");
                        Timer?.Invoke(2, MessageParams.Timer_Stop, "");

                        OnAd();

                        #endregion
                        break;
                    }
                case Messages.FinalRound:
                    {
                        #region FinalRound

                        for (var i = 0; i < ClientData.Players.Count; i++)
                        {
                            ClientData.Players[i].InGame = i + 1 < mparams.Length && mparams[i + 1] == "+";
                        }

                        ClientData.AtomIndex = -1;
                        ClientData.IsPartial = false;

                        #endregion
                        break;
                    }
                case Messages.Out:
                    {
                        #region Out

                        lock (ClientData.ChoiceLock)
                        lock (ClientData.TInfoLock)
                        {
                            ClientData.ThemeIndex = int.Parse(mparams[1]);

                            if (ClientData.ThemeIndex > -1 && ClientData.ThemeIndex < ClientData.TInfo.RoundInfo.Count)
                            {
                                _logic.Out(ClientData.ThemeIndex);
                            }
                        }

                        #endregion
                        break;
                    }
                case Messages.Winner:
                    {
                        #region Winner

                        ClientData.Winner = int.Parse(mparams[1]);
                        _logic.Winner();

                        #endregion
                        break;
                    }
                case Messages.Timeout:
                    {
                        #region Timeout

                        _logic.TimeOut();

                        #endregion
                        break;
                    }

                case Messages.FinalThink:
                    _logic.FinalThink();
                    break;

                case Messages.Picture:
                    {
                        #region Picture

                        var per = ClientData.MainPersons.FirstOrDefault(person => person.Name == mparams[1]);

                        if (per != null)
                        {
                            _logic.UpdatePicture(per, mparams[2]);
                        }

                        #endregion
                        break;
                    }

                case Messages.SetChooser:
                    {
                        var index = int.Parse(mparams[1]);

                        for (int i = 0; i < ClientData.Players.Count; i++)
                        {
                            ClientData.Players[i].IsChooser = i == index;

                            if (i == index && mparams.Length > 2)
                            {
                                ClientData.Players[i].State = PlayerState.Press;
                            }
                        }

                        break;
                    }

                case Messages.Ads:
                    if (mparams.Length > 1)
                    {
                        OnAd(mparams[1]);
                    }
                    break;
            }
        }
        catch (Exception exc)
        {
            throw new Exception(string.Join(Message.ArgsSeparator, mparams), exc);
        }
    }

    private void OnSetJoinMode(string[] mparams)
    {
        if (mparams.Length < 2 || !Enum.TryParse<JoinMode>(mparams[1], out var joinMode))
        {
            return;
        }

        MyData.JoinMode = joinMode;
    }

    private void OnQType(string[] mparams)
    {
        ClientData.AtomType = AtomTypes.Text;
        ClientData.AtomIndex = -1;
        ClientData.IsPartial = false;
        ClientData.QuestionType = mparams[1];

        _logic.OnQuestionType();
    }

    private void OnChoice(string[] mparams)
    {
        lock (ClientData.ChoiceLock)
        lock (ClientData.TInfoLock)
        {
            ClientData.ThemeIndex = int.Parse(mparams[1]);
            ClientData.QuestionIndex = int.Parse(mparams[2]);

            if (ClientData.ThemeIndex > -1
                && ClientData.ThemeIndex < ClientData.TInfo.RoundInfo.Count
                && ClientData.QuestionIndex > -1
                && ClientData.QuestionIndex < ClientData.TInfo.RoundInfo[ClientData.ThemeIndex].Questions.Count)
            {
                var selectedTheme = ClientData.TInfo.RoundInfo[ClientData.ThemeIndex];
                var selectedQuestion = selectedTheme.Questions[ClientData.QuestionIndex];
                ClientData.CurPriceRight = ClientData.CurPriceWrong = selectedQuestion.Price;
                _logic.SetCaption($"{selectedTheme.Name}, {selectedQuestion.Price}");
            }
        }

        foreach (var player in ClientData.Players.ToArray())
        {
            player.State = PlayerState.None;
            player.Pass = false;
            player.Stake = 0;
            player.SafeStake = false;
            player.MediaLoaded = false;
        }

        _logic.Choice();
    }

    private void OnMediaLoaded(string[] mparams)
    {
        if (mparams.Length < 2)
        {
            return;
        }

        var player = ClientData.Players.FirstOrDefault(p => p.Name == mparams[1]);

        if (player == null)
        {
            return;
        }

        player.MediaLoaded = true;
    }

    private void OnToggle(string[] mparams)
    {
        if (mparams.Length < 4)
        {
            return;
        }

        if (!int.TryParse(mparams[1], out var themeIndex)
            || !int.TryParse(mparams[2], out var questionIndex)
            || !int.TryParse(mparams[3], out var price))
        {
            return;
        }

        if (themeIndex < 0 || themeIndex >= ClientData.TInfo.RoundInfo.Count)
        {
            return;
        }

        var theme = ClientData.TInfo.RoundInfo[themeIndex];

        if (questionIndex < 0 || questionIndex >= theme.Questions.Count)
        {
            return;
        }

        theme.Questions[questionIndex].Price = price;
    }

    private void OnUnbanned(string[] mparams)
    {
        if (mparams.Length < 2)
        {
            return;
        }

        _logic.OnUnbanned(mparams[1]);
    }

    private void OnBanned(string[] mparams)
    {
        if (mparams.Length < 3)
        {
            return;
        }

        _logic.OnBanned(new BannedInfo(mparams[1], mparams[2]));
    }

    private void OnBannedList(string[] mparams)
    {
        var banned = new List<BannedInfo>();

        for (int i = 1; i < mparams.Length - 1; i += 2)
        {
            banned.Add(new BannedInfo(mparams[i], mparams[i + 1]));
        }

        _logic.OnBannedList(banned);
    }

    private async ValueTask OnDisconnectedAsync(string[] mparams)
    {
        if (mparams.Length < 2)
        {
            return;
        }

        var name = mparams[1];

        if (ClientData.AllPersons.TryGetValue(name, out var person))
        {
            ClientData.BeginUpdatePersons($"Disconnected {name}");

            try
            {
                person.IsConnected = false;
                person.Name = Constants.FreePlace;
                person.Picture = "";

                var personAccount = person as PersonAccount;

                if (ClientData.Stage == GameStage.Before && personAccount != null)
                {
                    personAccount.Ready = false;
                }

                if (personAccount == null)
                {
                    ClientData.Viewers.Remove(person);
                }
                else if (personAccount is PlayerAccount player)
                {
                    UpdatePlayerCommands(player);
                }
                else
                {
                    UpdateShowmanCommands();
                }
            }
            finally
            {
                ClientData.EndUpdatePersons();
            }
        }

        if (!_client.Node.IsMain)
        {
            await _client.Node.ConnectionsLock.WithLockAsync(() =>
            {
                var currentConnection = ((ISecondaryNode)_client.Node).HostServer;

                if (currentConnection != null)
                {
                    lock (currentConnection.ClientsSync)
                    {
                        if (currentConnection.Clients.Contains(name))
                        {
                            currentConnection.Clients.Remove(name);
                        }
                    }
                }
            });
        }

        PersonDisconnected?.Invoke();

        UpdateDeleteTableCommand();
    }

    private void OnAd(string? text = null) => Ad?.Invoke(text);

    private void OnRoundThemes(string[] mparams)
    {
        var print = mparams[1] == "+";

        lock (ClientData.TInfoLock)
        {
            ClientData.TInfo.RoundInfo.Clear();

            for (var i = 2; i < mparams.Length; i++)
            {
                ClientData.TInfo.RoundInfo.Add(new ThemeInfo { Name = mparams[i] });

                if (print)
                {
                    _logic.Print(ReplicManager.System(mparams[i]));
                }
            }
        }

        try
        {
            _logic.RoundThemes(print);
        }
        catch (InvalidProgramException exc)
        {
            ClientData.BackLink.SendError(exc, true);
        }

        OnAd();
    }

    private void OnThemeOrQuestion()
    {
        foreach (var player in ClientData.Players)
        {
            player.State = PlayerState.None;
            player.Pass = false;
            player.Stake = 0;
            player.SafeStake = false;
            player.MediaLoaded = false;
        }

        OnAd();
    }

    private void OnPersonStake(string[] mparams)
    {
        if (!int.TryParse(mparams[1], out var lastStakerIndex) ||
            lastStakerIndex < 0 ||
            lastStakerIndex >= ClientData.Players.Count)
        {
            return;
        }

        ClientData.LastStakerIndex = lastStakerIndex;

        int stake;

        if (mparams[2] == "0")
        {
            stake = -1;
        }
        else if (mparams[2] == "2")
        {
            stake = -2;
        }
        else if (mparams[2] == "3")
        {
            stake = -3;
        }
        else
        {
            if (!int.TryParse(mparams[3], out stake))
            {
                return;
            }

            if (mparams.Length > 4)
            {
                ClientData.Players[ClientData.LastStakerIndex].SafeStake = true;
            }
        }

        ClientData.Players[ClientData.LastStakerIndex].Stake = stake;

        OnAd();
    }

    private void OnReplic(string[] mparams)
    {
        var personCode = mparams[1];

        var text = new StringBuilder();

        for (var i = 2; i < mparams.Length; i++)
        {
            if (text.Length > 0)
            {
                text.Append(Message.ArgsSeparatorChar);
            }

            text.Append(mparams[i]);
        }

        _logic.OnReplic(personCode, text.ToString().Trim());
    }

    private async ValueTask ProcessConfigAsync(string[] mparams)
    {
        switch (mparams[1])
        {
            case MessageParams.Config_AddTable:
                OnConfigAddTable(mparams);
                break;

            case MessageParams.Config_Free:
                await OnConfigFreeAsync(mparams);
                break;

            case MessageParams.Config_DeleteTable:
                await OnConfigDeleteTableAsync(mparams);
                break;

            case MessageParams.Config_Set:
                await OnConfigSetAsync(mparams);
                break;

            case MessageParams.Config_ChangeType:
                await OnConfigChangeTypeAsync(mparams);
                break;
        }

        foreach (var item in ClientData.Players)
        {
            UpdatePlayerCommands(item);
        }

        UpdateShowmanCommands();
    }

    private void OnConfigAddTable(string[] mparams)
    {
        ClientData.BeginUpdatePersons($"Config_AddTable {string.Join(" ", mparams)}");

        try
        {
            var account = new PlayerAccount(mparams[2], mparams[3] == "+", mparams[4] == "+", ClientData.Stage != GameStage.Before)
            {
                IsHuman = mparams[5] == "+",
                Ready = mparams[6] == "+",
                IsExtendedMode = IsHost
            };

            CreatePlayerCommands(account);
            ClientData.Players.Add(account);

            UpdateAddTableCommand();
            UpdateDeleteTableCommand();

            UpdatePlayerCommands(account);

            var canDelete = ClientData.Players.Count > 2;

            foreach (var player in ClientData.Players)
            {
                player.Delete.CanBeExecuted = canDelete;
            }

            Logic.AddPlayer(account);
        }
        finally
        {
            ClientData.EndUpdatePersons();
        }
    }

    private async ValueTask OnConfigFreeAsync(string[] mparams)
    {
        if (mparams.Length < 4)
        {
            return;
        }

        var personType = mparams[2];
        var indexString = mparams[3];

        var me = ClientData.Me;

        PersonAccount account;

        var isPlayer = personType == Constants.Player;
        if (isPlayer)
        {
            if (!int.TryParse(indexString, out int index) || index < 0 || index >= ClientData.Players.Count)
            {
                return;
            }

            account = ClientData.Players[index];
        }
        else
        {
            account = ClientData.ShowMan;
        }

        var clone = new List<ViewerAccount>(ClientData.Viewers);
        var newAccount = new ViewerAccount(account) { IsConnected = true };

        clone.Add(newAccount);

        ClientData.BeginUpdatePersons($"Config_Free {string.Join(" ", mparams)}");
        try
        {
            ClientData.Viewers = clone;

            account.Name = Constants.FreePlace;
            account.IsConnected = false;
            account.Ready = false;
            account.Picture = "";
        }
        finally
        {
            ClientData.EndUpdatePersons();
        }

        if (account == me)
        {
            // Необходимо самого себя перевести в зрители
            await SwitchToNewTypeAsync(GameRole.Viewer, newAccount, me);
        }

        UpdateDeleteTableCommand();
    }

    private async ValueTask OnConfigChangeTypeAsync(string[] mparams)
    {
        if (mparams.Length < 7)
        {
            return;
        }

        var personType = mparams[2];
        var indexString = mparams[3];
        var newTypeHuman = mparams[4] == "+";
        var newName = mparams[5];
        var newSex = mparams[6] == "+";

        var me = ClientData.Me;

        PersonAccount account;
        var isPlayer = personType == Constants.Player;

        if (isPlayer)
        {
            if (!int.TryParse(indexString, out var index) || index < 0 || index >= ClientData.Players.Count)
            {
                return;
            }

            account = ClientData.Players[index];
        }
        else
        {
            account = ClientData.ShowMan;
        }

        if (account.IsHuman == newTypeHuman)
        {
            return;
        }

        if (newTypeHuman)
        {
            ClientData.BeginUpdatePersons($"Config_ChangeType {string.Join(" ", mparams)}");

            try
            {
                if (account.Name == ClientData.Name /* for computer accounts being deleted */)
                {
                    ThrowComputerAccountError();
                }

                account.IsHuman = true;
                account.Name = Constants.FreePlace;
                account.Picture = "";
                account.IsConnected = false;
                account.Ready = false;
            }
            finally
            {
                ClientData.EndUpdatePersons();
            }
        }
        else
        {
            ClientData.BeginUpdatePersons($"Config_ChangeType {string.Join(" ", mparams)}");
            ViewerAccount? newAccount = null;

            try
            {
                if (account.IsConnected)
                {
                    var clone = new List<ViewerAccount>(ClientData.Viewers);
                    newAccount = new ViewerAccount(account) { IsConnected = true };

                    clone.Add(newAccount);

                    ClientData.Viewers = clone;
                }
                else if (account == ClientData.Me)
                {
                    throw new InvalidOperationException("I am not connected!");
                }

                account.IsHuman = false;
                account.Name = newName;
                account.IsMale = newSex;
                account.Picture = "";
                account.IsConnected = true;
                account.Ready = false;
            }
            finally
            {
                ClientData.EndUpdatePersons();
            }

            if (account == me && newAccount != null)
            {
                // Необходимо самого себя перевести в зрители
                await SwitchToNewTypeAsync(GameRole.Viewer, newAccount, me);
            }

            UpdateDeleteTableCommand();
        }
    }

    private void ThrowComputerAccountError() =>
        throw new InvalidOperationException($"Computer account should never receive this\n{ClientData.EventLog}");

    private async ValueTask OnConfigDeleteTableAsync(string[] mparams)
    {
        if (mparams.Length < 3)
        {
            return;
        }

        var indexString = mparams[2];

        var me = ClientData.Me;

        if (!int.TryParse(indexString, out int index) || index < 0 || index >= ClientData.Players.Count)
        {
            return;
        }

        PlayerAccount account;
        ViewerAccount? newAccount = null;

        ClientData.BeginUpdatePersons($"Config_DeleteTable {string.Join(" ", mparams)}");

        try
        {
            account = ClientData.Players[index];

            ClientData.Players.RemoveAt(index);

            UpdateAddTableCommand();

            if (!account.IsHuman && account.Name == ClientData.Name /* for computer accounts being deleted */)
            {
                ThrowComputerAccountError();
            }

            if (account.IsConnected && account.IsHuman)
            {
                newAccount = new ViewerAccount(account) { IsConnected = true };

                var cloneV = new List<ViewerAccount>(ClientData.Viewers)
                {
                    newAccount
                };

                ClientData.Viewers = cloneV;
            }

            var canDelete = ClientData.Players.Count > 2;

            foreach (var player in ClientData.Players)
            {
                player.Delete.CanBeExecuted = canDelete;
            }

            Logic.RemovePlayerAt(index);
        }
        finally
        {
            ClientData.EndUpdatePersons();
        }

        if (account == me && newAccount != null && _logic.CanSwitchType)
        {
            // Необходимо самого себя перевести в зрители
            await SwitchToNewTypeAsync(GameRole.Viewer, newAccount, me);
        }

        UpdateDeleteTableCommand();
    }

    private PersonAccount GetAccountByType(bool isPlayer, string indexString)
    {
        if (isPlayer)
        {
            if (!int.TryParse(indexString, out int index) || index < 0 || index >= ClientData.Players.Count)
            {
                return null;
            }

            return ClientData.Players[index];
        }
        
        return ClientData.ShowMan;
    }

    private async ValueTask OnConfigSetAsync(string[] mparams)
    {
        if (mparams.Length < 6)
        {
            return;
        }

        var personType = mparams[2];
        var indexString = mparams[3];
        var replacer = mparams[4];
        var replacerIsMale = mparams[5] == "+";

        var isPlayer = personType == Constants.Player;

        var me = ClientData.Me;

        // Кого заменяем
        var account = GetAccountByType(isPlayer, indexString);

        if (account == null || account.Name == replacer)
        {
            return;
        }

        if (!account.IsHuman)
        {
            ClientData.BeginUpdatePersons($"Config_Set {string.Join(" ", mparams)}");

            try
            {
                if (account.Name == ClientData.Name /* for computer accounts being deleted */)
                {
                    ThrowComputerAccountError();
                }

                account.Name = replacer;
                account.IsMale = replacerIsMale;
                account.Ready = false;
            }
            finally
            {
                ClientData.EndUpdatePersons();
            }
        }
        else
        {
            var name = account.Name;
            var sex = account.IsMale;
            var picture = account.Picture;
            var ready = account.Ready;
            var isOnline = account.IsConnected;

            // Кто садится на его место
            ViewerAccount other = null;
            GameRole role = GameRole.Viewer;

            ClientData.BeginUpdatePersons($"Config_Set {string.Join(" ", mparams)}");

            try
            {
                if (isPlayer && ClientData.ShowMan.Name == replacer)
                {
                    // Ведущего сажаем на место игрока
                    var showman = ClientData.ShowMan;
                    other = showman;
                    role = GameRole.Showman;

                    account.Name = showman.Name;
                    account.IsMale = showman.IsMale;
                    account.Picture = showman.Picture;
                    account.Ready = showman.Ready;
                    account.IsConnected = showman.IsConnected;

                    showman.Name = name;
                    showman.IsMale = sex;
                    showman.Picture = picture;
                    showman.Ready = ready;
                    showman.IsConnected = isOnline;
                }
                else
                {
                    var found = false;

                    foreach (var item in ClientData.Players)
                    {
                        if (item.Name == replacer)
                        {
                            other = item;
                            role = GameRole.Player;

                            account.Name = item.Name;
                            account.IsMale = item.IsMale;
                            account.Picture = item.Picture;
                            account.Ready = item.Ready;
                            account.IsConnected = item.IsConnected;

                            item.Name = name;
                            item.IsMale = sex;
                            item.Picture = picture;
                            item.Ready = ready;
                            item.IsConnected = isOnline;

                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        foreach (var item in ClientData.Viewers)
                        {
                            if (item.Name == replacer)
                            {
                                other = item;
                                role = GameRole.Viewer;

                                account.Name = item.Name;
                                account.IsMale = item.IsMale;
                                account.Picture = item.Picture;
                                account.Ready = false;

                                if (isOnline)
                                {
                                    item.Name = name;
                                    item.IsMale = sex;
                                    item.Picture = picture;
                                }
                                else
                                {
                                    // место было пустым, зрителя нужо удалить
                                    ClientData.Viewers.Remove(item);
                                    account.IsConnected = true;
                                }

                                found = true;
                                break;
                            }
                        }
                    }
                }
            }
            finally
            {
                ClientData.EndUpdatePersons();
            }

            if (account == me)
            {
                var newRole = isPlayer ? GameRole.Player : GameRole.Showman;

                if (newRole != role)
                {
                    await SwitchToNewTypeAsync(role, other, me);
                }
                else
                {
                    var current = (PersonAccount)me;

                    ((PersonAccount)other).BeReadyCommand = current.BeReadyCommand;
                    ((PersonAccount)other).BeUnReadyCommand = current.BeUnReadyCommand;

                    current.BeReadyCommand = null;
                    current.BeUnReadyCommand = null;
                }
            }
            else if (other == me)
            {
                var newRole = isPlayer ? GameRole.Player : GameRole.Showman;

                if (newRole != role)
                {
                    await SwitchToNewTypeAsync(newRole, account, me);
                }
                else
                {
                    var current = (PersonAccount)me;

                    account.BeReadyCommand = current.BeReadyCommand;
                    account.BeUnReadyCommand = current.BeUnReadyCommand;

                    current.BeReadyCommand = null;
                    current.BeUnReadyCommand = null;
                }
            }
        }

        UpdateDeleteTableCommand();
    }

    /// <summary>
    /// Сменить тип своего аккаунта
    /// </summary>
    /// <param name="role">Целевой тип</param>
    private async ValueTask SwitchToNewTypeAsync(GameRole role, ViewerAccount newAccount, ViewerAccount oldAccount)
    {
        if (newAccount == null)
        {
            throw new ArgumentNullException(nameof(newAccount));
        }

        if (!_logic.CanSwitchType)
        {
            static string printAccount(ViewerAccount acc) => $"{acc.Name} {acc.IsHuman} {acc.IsMale} {acc.IsConnected}";

            var info = new StringBuilder()
                .Append("Showman: ").Append(ClientData.ShowMan?.Name).AppendLine()
                .Append("Players: ").Append(string.Join(", ", ClientData.Players.Select(p => p.Name))).AppendLine()
                .Append("Viewers: ").Append(string.Join(", ", ClientData.Viewers.Select(v => v.Name))).AppendLine()
                .Append("Me: ").Append(ClientData.Name).AppendLine()
                .Append("role: ").Append(role).AppendLine()
                .Append("newAccount: ").Append(printAccount(newAccount)).AppendLine()
                .Append("oldAccount: ").Append(printAccount(oldAccount)).AppendLine();

            throw new InvalidOperationException($"Trying to switch type of computer account:\n{info}");
        }

        ClientData.PersonDataExtensions.IsRight =
            ClientData.PersonDataExtensions.IsWrong =
            ClientData.PersonDataExtensions.SendCatCost =
            ClientData.PersonDataExtensions.SendFinalStake =
            ClientData.PlayerDataExtensions.SendAnswerVersion =
            ClientData.PlayerDataExtensions.SendAnswer =
            ClientData.ShowmanDataExtensions.ChangeSums =
            ClientData.ShowmanDataExtensions.ChangeActivePlayer =
            ClientData.ShowmanDataExtensions.ChangeSums2 =
            ClientData.ShowmanDataExtensions.Manage =
            ClientData.ShowmanDataExtensions.ManageTable =
            ClientData.PersonDataExtensions.SendNominal =
            ClientData.PersonDataExtensions.SendPass =
            ClientData.PersonDataExtensions.SendStake =
            ClientData.PersonDataExtensions.SendVabank =
            ClientData.PlayerDataExtensions.Apellate =
            ClientData.PlayerDataExtensions.PressGameButton = null;

        ClientData.PlayerDataExtensions.Report.SendReport = ClientData.PlayerDataExtensions.Report.SendNoReport = null;

        ClientData.Kick = ClientData.AtomViewed = ClientData.Ban = ClientData.SetHost = ClientData.Unban = ClientData.ForceStart = ClientData.AddTable = null;

        ClientData.MessageSending = null;
        ClientData.JoinModeChanged -= ClientData_JoinModeChanged;

        IViewerClient viewer = role switch
        {
            GameRole.Viewer => new SimpleViewer(_client, newAccount, IsHost, LO, ClientData),
            GameRole.Player => new Player(_client, newAccount, IsHost, LO, ClientData),
            _ => new Showman(_client, newAccount, IsHost, LO, ClientData),
        };

        if (oldAccount is PersonAccount current)
        {
            current.BeReadyCommand = current.BeUnReadyCommand = null;
        }

        viewer.Avatar = Avatar;

        viewer.Init();

        await DisposeAsync();

        viewer.RecreateCommands();

        Switch?.Invoke(viewer);

        SendPicture();
    }

    private async ValueTask ProcessInfoAsync(string[] mparams)
    {
        _ = int.TryParse(mparams[1], out var numOfPlayers);
        var numOfViewers = (mparams.Length - 2) / 5 - 1 - numOfPlayers;

        var gameStarted = ClientData.Stage != GameStage.Before;

        var mIndex = 2;
        ClientData.BeginUpdatePersons($"ProcessInfo {string.Join(" ", mparams)}");

        try
        {
            ClientData.ShowMan = new PersonAccount(mparams[mIndex++], mparams[mIndex++] == "+", mparams[mIndex++] == "+", gameStarted)
            {
                IsShowman = true,
                IsHuman = mparams[mIndex++] == "+",
                Ready = mparams[mIndex++] == "+"
            };

            var newPlayers = new List<PlayerAccount>();

            for (int i = 0; i < numOfPlayers; i++)
            {
                var account = new PlayerAccount(mparams[mIndex++], mparams[mIndex++] == "+", mparams[mIndex++] == "+", gameStarted)
                {
                    IsHuman = mparams[mIndex++] == "+",
                    Ready = mparams[mIndex++] == "+"
                };

                newPlayers.Add(account);
            }

            ClientData.Players.Clear();
            ClientData.Players.AddRange(newPlayers);

            var newViewers = new List<ViewerAccount>();

            for (int i = 0; i < numOfViewers; i++)
            {
                var viewerName = mparams[mIndex++];
                var isMale = mparams[mIndex++] == "+";
                var isConnected = mparams[mIndex++] == "+";
                var isHuman = mparams[mIndex++] == "+";

                if (!isConnected)
                {
                    continue; // Viewer cannot be disconnected. There is something wrong on server side
                }

                newViewers.Add(new ViewerAccount(viewerName, isMale, isConnected)
                {
                    IsHuman = isHuman
                });

                mIndex++; // пропускаем Ready
            }

            ClientData.Viewers = newViewers;
            ClientData.IsInfoInitialized = true;

            Logic.ResetPlayers();
        }
        finally
        {
            ClientData.EndUpdatePersons();
        }

        CreateShowmanCommands();

        foreach (var account in ClientData.Players)
        {
            CreatePlayerCommands(account);
        }

        foreach (var account in MyData.MainPersons)
        {
            account.IsExtendedMode = IsHost;
        }

        if (ClientData.Me != null)
        {
            ClientData.Me.Picture = ClientData.Picture;
        }

        if (!_client.Node.IsMain)
        {
            foreach (var item in ClientData.AllPersons.Values)
            {
                if (item != ClientData.Me && item.Name != NetworkConstants.GameName)
                {
                    await _client.Node.ConnectionsLock.WithLockAsync(() =>
                    {
                        var connection = ((ISecondaryNode)_client.Node).HostServer;

                        if (connection != null)
                        {
                            lock (connection.ClientsSync)
                            {
                                if (!connection.Clients.Contains(item.Name))
                                {
                                    connection.Clients.Add(item.Name);
                                }
                            }
                        }
                    });
                }
            }
        }

        UpdateAddTableCommand();
        UpdateDeleteTableCommand();

        SendPicture();
    }

    private void UpdateDeleteTableCommand()
    {
        if (ClientData.DeleteTable == null)
        {
            return;
        }

        ClientData.DeleteTable.CanBeExecuted = IsHost && ClientData.Players.Count > 2 && (ClientData.Stage == GameStage.Before || ClientData.Players.Any(p => !p.IsConnected || !p.IsHuman));
    }

    private void UpdateAddTableCommand()
    {
        if (ClientData.AddTable == null)
        {
            return;
        }

        ClientData.AddTable.CanBeExecuted = IsHost && ClientData.Players.Count < Constants.MaxPlayers;
    }

    public void RecreateCommands()
    {
        CreateShowmanCommands();

        foreach (var item in ClientData.Players)
        {
            CreatePlayerCommands(item);
        }
    }

    private void CreateShowmanCommands()
    {
        var showman = ClientData.ShowMan;

        showman.Free = new CustomCommand(Free_Executed) { CanBeExecuted = showman.IsHuman && showman.IsConnected };
        showman.Replace = new CustomCommand(arg => Replace_Executed(showman, arg)) { CanBeExecuted = showman.IsHuman };
        showman.ChangeType = new CustomCommand(ChangeType_Executed) { CanBeExecuted = true };
    }

    private void CreatePlayerCommands(PlayerAccount player)
    {
        player.Free = new CustomCommand(Free_Executed) { CanBeExecuted = player.IsHuman && player.IsConnected };
        player.Replace = new CustomCommand(arg => Replace_Executed(player, arg)) { CanBeExecuted = true };
        player.Delete = new CustomCommand(Delete_Executed) { CanBeExecuted = ClientData.Players.Count > 2 };
        player.ChangeType = new CustomCommand(ChangeType_Executed) { CanBeExecuted = true };

        UpdateOthers(player);
    }

    private void UpdatePlayerCommands(PlayerAccount player)
    {
        player.Free.CanBeExecuted = player.IsHuman && player.IsConnected;
        UpdateOthers(player);

        player.Replace.CanBeExecuted = player.Others.Any();
    }

    private Account[] GetDefaultComputerPlayers() => MyData.DefaultComputerPlayers
        ?? StoredPersonsRegistry.GetDefaultPlayers(LO, MyData.BackLink.PhotoUri);

    private void UpdateOthers(PlayerAccount player)
    {
        player.Others = player.IsHuman ?
            MyData.AllPersons.Values.Where(p => p.IsHuman)
                .Except(new ViewerAccount[] { player })
                .ToArray()
            : GetDefaultComputerPlayers()
                .Where(a => !MyData.AllPersons.Values.Any(p => !p.IsHuman && p.Name == a.Name))
                .ToArray();
    }

    private void UpdateShowmanCommands()
    {
        var showman = MyData.ShowMan;

        if (showman == null || showman.Free == null)
        {
            return;
        }

        showman.Free.CanBeExecuted = showman.IsHuman && showman.IsConnected;

        showman.Others = showman.IsHuman ?
                MyData.AllPersons.Values.Where(p => p.IsHuman).Except(new ViewerAccount[] { showman }).ToArray()
                : Array.Empty<ViewerAccount>();

        showman.Replace.CanBeExecuted = showman.Others.Any();
    }

    private void InsertPerson(string role, Account account, int index)
    {
        ClientData.BeginUpdatePersons($"InsertPerson {role} {account.Name} {index}");

        try
        {
            switch (role)
            {
                case Constants.Showman:
                    ClientData.ShowMan = new PersonAccount(account)
                    {
                        IsHuman = true,
                        IsConnected = true,
                        Ready = false,
                        GameStarted = ClientData.Stage != GameStage.Before,
                        IsShowman = true,
                        IsExtendedMode = IsHost
                    };

                    CreateShowmanCommands();
                    break;

                case Constants.Player:
                    var playersWereUpdated = false;

                    while (index >= ClientData.Players.Count)
                    {
                        var p = new PlayerAccount(
                            Constants.FreePlace,
                            true,
                            false,
                            ClientData.Stage != GameStage.Before)
                        {
                            IsHuman = true,
                            Ready = false,
                            IsExtendedMode = IsHost
                        };

                        CreatePlayerCommands(p);
                        ClientData.Players.Add(p);
                        playersWereUpdated = true;
                    }

                    if (playersWereUpdated)
                    {
                        ClientData.UpdatePlayers();
                    }

                    var player = ClientData.Players[index];

                    if (player.Name == ClientData.Name)
                    {
                        break;
                    }

                    player.Name = account.Name;
                    player.Picture = account.Picture;
                    player.IsMale = account.IsMale;
                    player.IsHuman = true;
                    player.IsConnected = true;
                    player.Ready = false;
                    player.GameStarted = ClientData.Stage != GameStage.Before;
                    break;

                case Constants.Viewer:
                    var viewer = new ViewerAccount(account) { IsHuman = true, IsConnected = true };

                    var existingViewer = ClientData.Viewers.FirstOrDefault(v => v.Name == viewer.Name);

                    if (existingViewer != null)
                    {
                        throw new Exception($"Duplicate viewer name: \"{viewer.Name}\" ({existingViewer.IsConnected})!\n" + ClientData.PersonsUpdateHistory);
                    }

                    ClientData.Viewers.Add(viewer);
                    ClientData.UpdateViewers();
                    break;

                default:
                    throw new ArgumentException($"Unsupported role {role}");
            }
        }
        finally
        {
            ClientData.EndUpdatePersons();
        }

        if (ClientData.Stage == GameStage.Before)
        {
            UpdateShowmanCommands();

            foreach (var item in ClientData.Players)
            {
                UpdatePlayerCommands(item);
            }
        }
    }

    /// <summary>
    /// Получение сообщения
    /// </summary>
    public override async ValueTask OnMessageReceivedAsync(Message message)
    {
        if (message.IsSystem)
        {
            await OnSystemMessageReceivedAsync(message.Text.Split('\n'));
        }
        else
        {
            _logic.ReceiveText(message);
        }
    }

    /// <summary>
    /// Сказать в чат
    /// </summary>
    /// <param name="text">Текст сообщения</param>
    /// <param name="whom">Кому</param>
    /// <param name="isPrivate">Приватно ли</param>
    internal void Say(string text, string whom = NetworkConstants.Everybody, bool isPrivate = false)
    {
        if (whom != NetworkConstants.Everybody)
        {
            text = $"{whom}, {text}";
        }

        if (!isPrivate)
        {
            whom = NetworkConstants.Everybody;
        }
        else
        {
            text = $"({LO[nameof(R.Private)]}) {text}";
        }

        _client.SendMessage(text, false, whom, isPrivate);
        _logic.ReceiveText(new Message(text, _client.Name, whom, false, isPrivate));
    }

    public void Pause() => _viewerActions.SendMessage(Messages.Pause, ClientData.TInfo.Pause ? "-" : "+");

    /// <summary>
    /// Sends user avatar (file or uri) to server.
    /// </summary>
    public void SendPicture()
    {
        if (Avatar != null)
        {
            _viewerActions.SendMessage(Messages.Picture, Avatar);
            return;
        }

        if (!string.IsNullOrWhiteSpace(ClientData.Picture))
        {
            if (!Uri.TryCreate(ClientData.Picture, UriKind.RelativeOrAbsolute, out var uri))
            {
                return;
            }

            if (!uri.IsAbsoluteUri)
            {
                return;
            }

            if (uri.Scheme == "file" && !_client.Node.Contains(NetworkConstants.GameName)) // We should send local file over network
            {
                byte[]? data;

                try
                {
                    data = CoreManager.Instance.GetData(ClientData.Picture);
                }
                catch (Exception exc)
                {
                    ClientData.BackLink.SendError(exc, false);
                    return;
                }

                if (data == null)
                {
                    _logic.OnReplic(ReplicCodes.Special.ToString(), string.Format(LO[nameof(R.AvatarNotFound)], ClientData.Picture));
                    return;
                }

                _viewerActions.SendMessage(Messages.Picture, ClientData.Picture, Convert.ToBase64String(data));
            }
            else
            {
                _viewerActions.SendMessage(Messages.Picture, ClientData.Picture);
            }
        }
    }

    /// <summary>
    /// Sends game info request.
    /// </summary>
    public void GetInfo() => _viewerActions.SendMessage(Messages.Info);

    public virtual void Init() => ClientData.IsPlayer = false;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
