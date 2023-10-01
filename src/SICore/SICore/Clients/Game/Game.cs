using Notions;
using SICore.BusinessLogic;
using SICore.Clients;
using SICore.Contracts;
using SICore.Extensions;
using SICore.Models;
using SICore.Network;
using SICore.Network.Clients;
using SICore.Network.Contracts;
using SICore.PlatformSpecific;
using SICore.Results;
using SICore.Special;
using SICore.Utils;
using SIData;
using SIPackages;
using SIPackages.Core;
using System.Text;
using R = SICore.Properties.Resources;

namespace SICore;

// TODO: Global refactoring plan:
// extract different script steps implementations (stake making, question selecting and giving etc.)
// to separate classes (strategies)
// Extract corresponging state from GameData class

// Remove Final round logic; use only StakeAll question type which can also appear in standard round

/// <summary>
/// Defines a game actor. Responds to all game-related messages.
/// </summary>
public sealed class Game : Actor<GameData, GameLogic>
{
    public event Action<Game, bool, bool>? PersonsChanged;

    /// <summary>
    /// Informs the hosting environment that a person with provided name should be disconnected.
    /// </summary>
    public event Action<string>? DisconnectRequested;

    private readonly GameActions _gameActions;

    private IPrimaryNode Master => (IPrimaryNode)_client.Node;

    private readonly ComputerAccount[] _defaultPlayers;
    private readonly ComputerAccount[] _defaultShowmans;

    private readonly IFileShare _fileShare;
    private readonly IAvatarHelper _avatarHelper;

    public Game(
        Client client,
        string? documentPath,
        ILocalizer localizer,
        GameData gameData,
        GameActions gameActions,
        GameLogic gameLogic,
        ComputerAccount[] defaultPlayers,
        ComputerAccount[] defaultShowmans,
        IFileShare fileShare,
        IAvatarHelper avatarHelper)
        : base(client, localizer, gameData)
    {
        _gameActions = gameActions;
        _logic = gameLogic;

        _logic.AutoGame += AutoGame;

        gameData.DocumentPath = documentPath;

        _defaultPlayers = defaultPlayers ?? throw new ArgumentNullException(nameof(defaultPlayers));
        _defaultShowmans = defaultShowmans ?? throw new ArgumentNullException(nameof(defaultShowmans));

        _fileShare = fileShare;
        _avatarHelper = avatarHelper;

        Master.Unbanned += Master_Unbanned;
    }

    private void Master_Unbanned(string clientId) => _gameActions.SendMessageWithArgs(Messages.Unbanned, clientId);

    public override async ValueTask DisposeAsync(bool disposing)
    {
        // Logic must be disposed before TaskLock
        await base.DisposeAsync(disposing);

        ClientData.TaskLock.Dispose();
        ClientData.TableInformStageLock.Dispose();
    }

    /// <summary>
    /// Starts the game engine.
    /// </summary>
    public void Run()
    {
        Client.CurrentServer.SerializationError += CurrentServer_SerializationError;

        _logic.Run();

        foreach (var personName in ClientData.AllPersons.Keys)
        {
            if (personName == NetworkConstants.GameName)
            {
                continue;
            }

            Inform(personName);
        }
    }

    private void CurrentServer_SerializationError(Message message, Exception exc)
    {
        // Это случается при выводе частичного текста. Пытаемся поймать
        try
        {
            var fullText = ClientData.Text ?? "";

            var errorMessage = new StringBuilder("SerializationError: ")
                .Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(fullText)))
                .Append('\n')
                .Append(ClientData.TextLength)
                .Append('\n')
                .Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(message.Sender)))
                .Append('\n')
                .Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(message.Receiver)))
                .Append('\n')
                .Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(message.Text)))
                .Append('\n')
                .Append((message.Text ?? "").Length)
                .Append(' ').Append(ClientData.Settings.AppSettings.ReadingSpeed);

            _client.Node.OnError(new Exception(errorMessage.ToString(), exc), true);
        }
        catch (Exception e)
        {
            _client.Node.OnError(e, true);
        }
    }

    /// <summary>
    /// Sends all current game data to the person.
    /// </summary>
    /// <param name="person">Receiver name.</param>
    private void Inform(string person = NetworkConstants.Everybody)
    {
        InformSettings(person);
        InformGameMetadata(person);
        SendInfo(person);
        InformAvatars(person);
        InformBanned(person);
    }

    private void InformGameMetadata(string person)
    {
        _gameActions.SendMessageToWithArgs(
            person,
            Messages.GameMetadata,
            ClientData.GameName,
            _logic.Engine.PackageName,
            _logic.Engine.ContactUri,
            ClientData.Settings.NetworkVoiceChat);

        _gameActions.SendMessageToWithArgs(person, Messages.Hostname, ClientData.HostName ?? "");
    }

    private void InformSettings(string person)
    {
        _gameActions.SendMessageToWithArgs(
            person,
            Messages.ComputerAccounts,
            string.Join(Message.ArgsSeparator, _defaultPlayers.Select(p => p.Name)));

        var appSettings = ClientData.Settings.AppSettings;

        _gameActions.SendMessageToWithArgs(
            person,
            Messages.ReadingSpeed,
            appSettings.Managed ? 0 : appSettings.ReadingSpeed);

        _gameActions.SendMessageToWithArgs(person, Messages.FalseStart, appSettings.FalseStart ? "+" : "-");
        _gameActions.SendMessageToWithArgs(person, Messages.ButtonBlockingTime, appSettings.TimeSettings.TimeForBlockingButton);
        _gameActions.SendMessageToWithArgs(person, Messages.ApellationEnabled, appSettings.UseApellations ? '+' : '-');

        var maxPressingTime = appSettings.TimeSettings.TimeForThinkingOnQuestion * 10;
        _gameActions.SendMessageToWithArgs(person, Messages.Timer, 1, "MAXTIME", maxPressingTime);

        _gameActions.SendMessageToWithArgs(person, Messages.SetJoinMode, ClientData.JoinMode);
    }

    private void InformBanned(string person)
    {
        var banned = Master.Banned;

        if (banned.Any())
        {
            var messageBuilder = new MessageBuilder(Messages.BannedList);

            foreach (var item in banned)
            {
                messageBuilder.Add(item.Key).Add(item.Value);
            }

            _gameActions.SendMessage(messageBuilder.Build(), person);
        }
    }

    private void InformAvatars(string person)
    {
        // Send persons avatars info
        if (person != NetworkConstants.Everybody)
        {
            InformAvatar(ClientData.ShowMan, person);

            foreach (var item in ClientData.Players)
            {
                InformAvatar(item, person);
            }
        }
        else
        {
            InformAvatar(ClientData.ShowMan);

            foreach (var item in ClientData.Players)
            {
                InformAvatar(item);
            }
        }
    }

    private void SendInfo(string person)
    {
        var info = new StringBuilder(Messages.Info2)
            .Append(Message.ArgsSeparatorChar)
            .Append(ClientData.Players.Count)
            .Append(Message.ArgsSeparatorChar);

        AppendAccountExt(ClientData.ShowMan, info);

        info.Append(Message.ArgsSeparatorChar);

        foreach (var player in ClientData.Players)
        {
            AppendAccountExt(player, info);

            info.Append(Message.ArgsSeparatorChar);
        }

        foreach (var viewer in ClientData.Viewers)
        {
            if (!viewer.IsConnected)
            {
                ClientData.BackLink.LogWarning($"Viewer {viewer.Name} not connected\n" + ClientData.PersonsUpdateHistory);
                continue;
            }

            AppendAccountExt(viewer, info);
            info.Append(Message.ArgsSeparatorChar);
        }

        var msg = info.ToString()[..(info.Length - 1)];

        _gameActions.SendMessage(msg, person);
    }

    private static void AppendAccountExt(ViewerAccount account, StringBuilder info)
    {
        info.Append(account.Name);
        info.Append(Message.ArgsSeparatorChar);
        info.Append(account.IsMale ? '+' : '-');
        info.Append(Message.ArgsSeparatorChar);
        info.Append(account.IsConnected ? '+' : '-');
        info.Append(Message.ArgsSeparatorChar);
        info.Append(account.IsHuman ? '+' : '-');
        info.Append(Message.ArgsSeparatorChar);

        info.Append(account is GamePersonAccount person && person.Ready ? '+' : '-');
    }

    public string GetSums()
    {
        var s = new StringBuilder();
        var total = ClientData.Players.Count;

        for (int i = 0; i < total; i++)
        {
            if (s.Length > 0)
            {
                s.Append(", ");
            }

            s.AppendFormat("{0}: {1}", ClientData.Players[i].Name, ClientData.Players[i].Sum);
        }

        return s.ToString();
    }

    public ConnectionPersonData[] GetInfo()
    {
        var result = new List<ConnectionPersonData>
        {
            new ConnectionPersonData { Name = ClientData.ShowMan.Name, Role = GameRole.Showman, IsOnline = ClientData.ShowMan.IsConnected }
        };

        for (int i = 0; i < ClientData.Players.Count; i++)
        {
            result.Add(new ConnectionPersonData
            {
                Name = ClientData.Players[i].Name,
                Role = GameRole.Player,
                IsOnline = ClientData.Players[i].IsConnected
            });
        }

        for (int i = 0; i < ClientData.Viewers.Count; i++)
        {
            result.Add(new ConnectionPersonData
            {
                Name = ClientData.Viewers[i].Name,
                Role = GameRole.Viewer,
                IsOnline = ClientData.Viewers[i].IsConnected
            });
        }

        return result.ToArray();
    }

    /// <summary>
    /// Adds person to the game.
    /// </summary>
    public (bool, string) Join(
        string name,
        bool isMale,
        GameRole role,
        string password,
        Action connectionAuthenticator) =>
        ClientData.TaskLock.WithLock(() =>
        {
            if (ClientData.JoinMode == JoinMode.Forbidden)
            {
                return (false, LO[nameof(R.JoinForbidden)]);
            }

            if (ClientData.JoinMode == JoinMode.OnlyViewer && role != GameRole.Viewer)
            {
                return (false, LO[nameof(R.JoinRoleForbidden)]);
            }

            if (!string.IsNullOrEmpty(ClientData.Settings.NetworkGamePassword)
                && ClientData.Settings.NetworkGamePassword != password)
            {
                return (false, LO[nameof(R.WrongPassword)]);
            }

            if (ClientData.AllPersons.ContainsKey(name))
            {
                return (false, string.Format(LO[nameof(R.PersonWithSuchNameIsAlreadyInGame)], name));
            }

            var index = -1;
            IEnumerable<ViewerAccount>? accountsToSearch = null;

            switch (role)
            {
                case GameRole.Showman:
                    accountsToSearch = new ViewerAccount[1] { ClientData.ShowMan };
                    break;

                case GameRole.Player:
                    accountsToSearch = ClientData.Players;

                    if (ClientData.HostName == name) // Host is joining
                    {
                        var players = ClientData.Players;

                        for (var i = 0; i < players.Count; i++)
                        {
                            if (players[i].Name == name)
                            {
                                index = i;
                                break;
                            }
                        }

                        if (index < 0)
                        {
                            return (false, LO[nameof(R.PositionNotFoundByIndex)]);
                        }
                    }

                    break;

                default: // Viewer
                    accountsToSearch = ClientData.Viewers.Concat(
                        new ViewerAccount[] { new ViewerAccount(Constants.FreePlace, false, false) { IsHuman = true } });

                    break;
            }

            var found = false;

            if (index > -1)
            {
                var accounts = accountsToSearch.ToArray();

                var result = CheckAccountNew(
                    role.ToString().ToLower(),
                    name,
                    isMale ? "m" : "f",
                    ref found,
                    index,
                    accounts[index],
                    connectionAuthenticator);

                if (result.HasValue)
                {
                    if (!result.Value)
                    {
                        return (false, LO[nameof(R.PlaceIsOccupied)]);
                    }
                    else
                    {
                        found = true;
                    }
                }
            }
            else
            {
                foreach (var item in accountsToSearch)
                {
                    index++;

                    var result = CheckAccountNew(
                        role.ToString().ToLower(),
                        name,
                        isMale ? "m" : "f",
                        ref found,
                        index,
                        item,
                        connectionAuthenticator);

                    if (result.HasValue)
                    {
                        if (!result.Value)
                        {
                            return (false, LO[nameof(R.PlaceIsOccupied)]);
                        }
                        else
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }

            if (!found)
            {
                return (false, LO[nameof(R.NoFreePlaceForName)]);
            }

            return (true, "");
        },
        5000);

    /// <summary>
    /// Processed received message.
    /// </summary>
    /// <param name="message">Received message.</param>
    public override ValueTask OnMessageReceivedAsync(Message message) =>
        ClientData.TaskLock.WithLockAsync(async () =>
        {
            if (string.IsNullOrEmpty(message.Text))
            {
                return;
            }

            Logic.AddHistory($"[{message.Text}@{message.Sender}]");

            var args = message.Text.Split(Message.ArgsSeparatorChar);

            try
            {
                // Action according to protocol
                switch (args[0])
                {
                    case Messages.GameInfo:
                        #region GameInfo

                        // Информация о текущей игре для подключающихся по сети
                        var res = new StringBuilder();
                        
                        res.Append(Messages.GameInfo);
                        res.Append(Message.ArgsSeparatorChar).Append(ClientData.Settings.NetworkGameName);
                        res.Append(Message.ArgsSeparatorChar).Append(ClientData.HostName);
                        res.Append(Message.ArgsSeparatorChar).Append(ClientData.Players.Count);

                        res.Append(Message.ArgsSeparatorChar).Append(ClientData.ShowMan.Name);
                        res.Append(Message.ArgsSeparatorChar).Append(ClientData.ShowMan.IsConnected ? '+' : '-');
                        res.Append(Message.ArgsSeparatorChar).Append('-');

                        for (int i = 0; i < ClientData.Players.Count; i++)
                        {
                            res.Append(Message.ArgsSeparatorChar).Append(ClientData.Players[i].Name);
                            res.Append(Message.ArgsSeparatorChar).Append(ClientData.Players[i].IsConnected ? '+' : '-');
                            res.Append(Message.ArgsSeparatorChar).Append('-');
                        }

                        for (int i = 0; i < ClientData.Viewers.Count; i++)
                        {
                            res.Append(Message.ArgsSeparatorChar).Append(ClientData.Viewers[i].Name);
                            res.Append(Message.ArgsSeparatorChar).Append(ClientData.Viewers[i].IsConnected ? '+' : '-');
                            res.Append(Message.ArgsSeparatorChar).Append('-');
                        }

                        _gameActions.SendMessage(res.ToString(), message.Sender);

                        #endregion
                        break;

                    case Messages.Connect:
                        await OnConnectAsync(message, args);
                        break;

                    case SystemMessages.Disconnect:
                        OnDisconnect(args);
                        break;

                    case Messages.Info:
                        OnInfo(message);
                        break;

                    case Messages.Config:
                        ProcessConfig(message, args);
                        break;

                    case Messages.First:
                        if (ClientData.IsWaiting &&
                            ClientData.Decision == DecisionType.StarterChoosing &&
                            message.Sender == ClientData.ShowMan.Name &&
                            args.Length > 1)
                        {
                            #region First
                            // Ведущий прислал номер того, кто начнёт игру
                            if (int.TryParse(args[1], out int playerIndex) && playerIndex > -1 && playerIndex < ClientData.Players.Count && ClientData.Players[playerIndex].Flag)
                            {
                                ClientData.ChooserIndex = playerIndex;
                                _logic.Stop(StopReason.Decision);
                            }
                            #endregion
                        }
                        break;

                    case Messages.SetChooser:
                        if (message.Sender == ClientData.ShowMan.Name && args.Length > 1)
                        {
                            if (int.TryParse(args[1], out int playerIndex) && playerIndex > -1 && playerIndex < ClientData.Players.Count)
                            {
                                ClientData.ChooserIndex = playerIndex;
                                _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex);

                                _gameActions.SpecialReplic(string.Format(LO[nameof(R.SetChooser)], ClientData.ShowMan.Name, ClientData.Chooser?.Name));
                            }
                        }
                        break;

                    case Messages.SetJoinMode:
                        if (message.Sender == ClientData.HostName && args.Length > 1)
                        {
                            if (Enum.TryParse<JoinMode>(args[1], out var joinMode))
                            {
                                ClientData.JoinMode = joinMode;
                                _gameActions.SendMessageWithArgs(Messages.SetJoinMode, args[1]);

                                var replic = joinMode switch
                                {
                                    JoinMode.AnyRole => LO[nameof(R.JoinModeSwitchedToAny)],
                                    JoinMode.OnlyViewer => LO[nameof(R.JoinModeSwitchedToViewers)],
                                    _ => LO[nameof(R.JoinModeSwitchedToForbidden)]
                                };

                                _gameActions.SpecialReplic(string.Format(replic, ClientData.HostName));
                            }
                        }
                        break;

                    case Messages.Pause:
                        OnPause(message, args);
                        break;

                    case Messages.Start:
                        if (message.Sender == ClientData.HostName && ClientData.Stage == GameStage.Before)
                        {
                            StartGame();
                        }
                        break;

                    case Messages.Ready:
                        OnReady(message, args);
                        break;

                    case Messages.Picture:
                        OnPicture(message, args);
                        break;

                    case Messages.Choice:
                        if (ClientData.IsWaiting &&
                            ClientData.Decision == DecisionType.QuestionSelection &&
                            args.Length == 3 &&
                            ClientData.Chooser != null &&
                                (message.Sender == ClientData.Chooser.Name ||
                                ClientData.IsOralNow && message.Sender == ClientData.ShowMan.Name))
                        {
                            #region Choice

                            if (!int.TryParse(args[1], out int i) || !int.TryParse(args[2], out int j))
                            {
                                break;
                            }

                            if (i < 0 || i >= ClientData.TInfo.RoundInfo.Count)
                            {
                                break;
                            }

                            if (j < 0 || j >= ClientData.TInfo.RoundInfo[i].Questions.Count)
                            {
                                break;
                            }

                            if (ClientData.TInfo.RoundInfo[i].Questions[j].IsActive())
                            {
                                lock (ClientData.ChoiceLock)
                                {
                                    ClientData.ThemeIndex = i;
                                    ClientData.QuestionIndex = j;
                                }

                                if (ClientData.IsOralNow)
                                {
                                    _gameActions.SendMessage(Messages.Cancel, ClientData.ShowMan.Name);
                                }

                                if (Logic.CanPlayerAct())
                                {
                                    _gameActions.SendMessage(Messages.Cancel, ClientData.Chooser.Name);
                                }

                                _logic.Stop(StopReason.Decision);
                            }

                            #endregion
                        }
                        break;

                    case Messages.Toggle:
                        OnToggle(message, args);
                        break;

                    case Messages.I:
                        OnI(message.Sender);
                        break;

                    case Messages.Pass:
                        OnPass(message);
                        break;

                    case Messages.AnswerVersion:
                        OnAnswerVersion(message, args);
                        break;

                    case Messages.Answer:
                        OnAnswer(message, args);
                        break;

                    case Messages.Atom:
                        OnAtom();
                        break;

                    case Messages.MediaLoaded:
                        OnMediaLoaded(message);
                        break;

                    case Messages.Report:
                        OnReport(message, args);
                        break;

                    case Messages.IsRight:
                        OnIsRight(message, args);
                        break;

                    case Messages.Next:
                        if (ClientData.IsWaiting &&
                            ClientData.Decision == DecisionType.NextPersonStakeMaking &&
                            message.Sender == ClientData.ShowMan.Name)
                        {
                            #region Next

                            if (args.Length > 1 && int.TryParse(args[1], out int n) && n > -1 && n < ClientData.Players.Count)
                            {
                                if (ClientData.Players[n].Flag)
                                {
                                    ClientData.Order[ClientData.OrderIndex] = n;
                                    Logic.CheckOrder(ClientData.OrderIndex);
                                    _logic.Stop(StopReason.Decision);
                                }
                            }

                            #endregion
                        }
                        break;

                    case Messages.Cat:
                        if (ClientData.IsWaiting &&
                            ClientData.Decision == DecisionType.QuestionAnswererSelection &&
                            (ClientData.Chooser != null && message.Sender == ClientData.Chooser.Name ||
                            ClientData.IsOralNow && message.Sender == ClientData.ShowMan.Name))
                        {
                            #region Cat

                            try
                            {
                                if (int.TryParse(args[1], out int index) && index > -1 && index < ClientData.Players.Count && ClientData.Players[index].Flag)
                                {
                                    ClientData.AnswererIndex = index;
                                    ClientData.QuestionPlayState.SetSingleAnswerer(index);

                                    if (ClientData.IsOralNow)
                                    {
                                        _gameActions.SendMessage(
                                            Messages.Cancel,
                                            message.Sender == ClientData.ShowMan.Name ? ClientData.Chooser.Name : ClientData.ShowMan.Name);
                                    }

                                    _logic.Stop(StopReason.Decision);
                                }
                            }
                            catch (Exception) { }

                            #endregion
                        }
                        break;

                    case Messages.CatCost:
                        OnCatCost(message, args);
                        break;

                    case Messages.Stake:
                        OnStake(message, args);
                        break;

                    case Messages.NextDelete:
                        OnNextDelete(message, args);
                        break;

                    case Messages.Delete:
                        OnDelete(message, args);
                        break;

                    case Messages.FinalStake:
                        if (ClientData.IsWaiting && ClientData.Decision == DecisionType.FinalStakeMaking)
                        {
                            #region FinalStake

                            for (var i = 0; i < ClientData.Players.Count; i++)
                            {
                                var player = ClientData.Players[i];

                                if (ClientData.QuestionPlayState.AnswererIndicies.Contains(i) && player.FinalStake == -1 && message.Sender == player.Name)
                                {
                                    if (int.TryParse(args[1], out int finalStake) && finalStake >= 1 && finalStake <= player.Sum)
                                    {
                                        player.FinalStake = finalStake;
                                        ClientData.NumOfStakers--;

                                        _gameActions.SendMessageWithArgs(Messages.PersonFinalStake, i);
                                    }

                                    break;
                                }
                            }

                            if (ClientData.NumOfStakers == 0)
                            {
                                _logic.Stop(StopReason.Decision);
                            }

                            #endregion
                        }
                        break;

                    case Messages.Apellate:
                        OnApellation(message, args);
                        break;

                    case Messages.Change:
                        OnChanged(message, args);
                        break;

                    case Messages.Move:
                        OnMove(message, args);
                        break;

                    case Messages.Kick:
                        OnKick(message, args);
                        break;

                    case Messages.Ban:
                        OnBan(message, args);
                        break;

                    case Messages.Unban:
                        OnUnban(message, args);
                        break;

                    case Messages.SetHost:
                        OnSetHost(message, args);
                        break;

                    case Messages.Mark:
                        if (!ClientData.CanMarkQuestion)
                        {
                            break;
                        }

                        ClientData.GameResultInfo.ComplainedQuestions.Add(new QuestionReport
                        {
                            ThemeName = ClientData.Theme.Name,
                            QuestionText = ClientData.Question?.GetText(),
                            ReportText = args.Length > 1 ? args[1] : ""
                        });
                        break;
                }
            }
            catch (Exception exc)
            {
                _client.Node.OnError(new Exception(message.Text, exc), true);
            }
        }, 5000);

    private void OnReport(Message message, string[] args)
    {
        if (ClientData.Decision != DecisionType.Reporting
            || !ClientData.Players.Any(player => player.Name == message.Sender)
            || ClientData.GameResultInfo.Reviews.ContainsKey(message.Sender))
        {
            return;
        }

        ClientData.ReportsCount--;

        ClientData.GameResultInfo.Reviews[message.Sender] = args.Length > 2 ? args[2] : "";

        if (ClientData.ReportsCount == 0)
        {
            _logic.ExecuteImmediate();
        }
    }

    private void OnMediaLoaded(Message message) => _gameActions.SendMessageToWithArgs(ClientData.ShowMan.Name, Messages.MediaLoaded, message.Sender);

    private void OnToggle(Message message, string[] args)
    {
        if (message.Sender != ClientData.ShowMan.Name || args.Length < 3)
        {
            return;
        }

        if (!int.TryParse(args[1], out int themeIndex) || !int.TryParse(args[2], out int questionIndex))
        {
            return;
        }

        if (themeIndex < 0 || themeIndex >= ClientData.TInfo.RoundInfo.Count)
        {
            return;
        }

        if (questionIndex < 0 || questionIndex >= ClientData.TInfo.RoundInfo[themeIndex].Questions.Count)
        {
            return;
        }

        var question = ClientData.TInfo.RoundInfo[themeIndex].Questions[questionIndex];

        if (question.IsActive())
        {
            if (!_logic.Engine.RemoveQuestion(themeIndex, questionIndex))
            {
                return;
            }

            var oldPrice = question.Price;
            question.Price = Question.InvalidPrice;
            _gameActions.SendMessageWithArgs(Messages.Toggle, themeIndex, questionIndex, Question.InvalidPrice);

            _gameActions.SpecialReplic(
                string.Format(
                    LO[nameof(R.QuestionRemoved)],
                    message.Sender,
                    ClientData.TInfo.RoundInfo[themeIndex].Name,
                    oldPrice));

            var nextTask = (Tasks)Logic.PendingTask;

            if ((nextTask == Tasks.AskToChoose || nextTask == Tasks.WaitChoose || nextTask == Tasks.AskFirst || nextTask == Tasks.WaitFirst) && _logic.Engine.LeftQuestionsCount == 0)
            {
                // Round is empty
                PlanExecution(Tasks.EndRound, 10);
            }
        }
        else
        {
            var restoredPrice = _logic.Engine.RestoreQuestion(themeIndex, questionIndex);

            if (!restoredPrice.HasValue)
            {
                return;
            }

            question.Price = restoredPrice.Value;
            _gameActions.SendMessageWithArgs(Messages.Toggle, themeIndex, questionIndex, restoredPrice.Value);

            _gameActions.SpecialReplic(
                string.Format(
                    LO[nameof(R.QuestionRestored)],
                    message.Sender,
                    ClientData.TInfo.RoundInfo[themeIndex].Name,
                    restoredPrice.Value));
        }

        // TODO: remove after all clients upgrade to 7.9.5
        ClientData.TableInformStageLock.WithLock(
            () =>
            {
                if (ClientData.TableInformStage > 1)
                {
                    _gameActions.InformRoundThemes(play: false);
                    _gameActions.InformTable();
                }
            },
            5000);
    }

    private void OnKick(Message message, string[] args)
    {
        if (message.Sender != ClientData.HostName || args.Length <= 1)
        {
            return;
        }

        var person = args[1];

        if (!ClientData.AllPersons.TryGetValue(person, out var per))
        {
            return;
        }

        if (per.Name == message.Sender)
        {
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.Replic, ReplicCodes.Special.ToString(), LO[nameof(R.CannotKickYouself)]);
            return;
        }

        if (!per.IsHuman)
        {
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.Replic, ReplicCodes.Special.ToString(), LO[nameof(R.CannotKickBots)]);
            return;
        }

        var clientId = Master.Kick(person);

        if (clientId.Length > 0)
        {
            _gameActions.SendMessageWithArgs(Messages.Banned, clientId, person);
        }

        _gameActions.SpecialReplic(string.Format(LO[nameof(R.Kicked)], message.Sender, person));
        OnDisconnectRequested(person);
    }

    private void OnBan(Message message, string[] args)
    {
        if (message.Sender != ClientData.HostName || args.Length <= 1)
        {
            return;
        }

        var clientName = args[1];

        if (!ClientData.AllPersons.TryGetValue(clientName, out var person))
        {
            return;
        }

        if (person.Name == message.Sender)
        {
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.Replic, ReplicCodes.Special.ToString(), LO[nameof(R.CannotBanYourself)]);
            return;
        }

        if (!person.IsHuman)
        {
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.Replic, ReplicCodes.Special.ToString(), LO[nameof(R.CannotBanBots)]);
            return;
        }

        var clientId = Master.Kick(clientName, true);

        if (clientId.Length > 0)
        {
            _gameActions.SendMessageWithArgs(Messages.Banned, clientId, person);
        }

        _gameActions.SpecialReplic(string.Format(LO[nameof(R.Banned)], message.Sender, clientName));
        OnDisconnectRequested(clientName);
    }

    private void OnSetHost(Message message, string[] args)
    {
        if (message.Sender != ClientData.HostName || args.Length <= 1)
        {
            return;
        }

        var clientName = args[1];

        if (!ClientData.AllPersons.TryGetValue(clientName, out var person))
        {
            return;
        }

        if (person.Name == message.Sender)
        {
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.Replic, ReplicCodes.Special.ToString(), LO[nameof(R.CannotSetHostToYourself)]);
            return;
        }

        if (!person.IsHuman)
        {
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.Replic, ReplicCodes.Special.ToString(), LO[nameof(R.CannotSetHostToBot)]);
            return;
        }

        UpdateHostName(person.Name, message.Sender);
    }

    private void OnUnban(Message message, string[] args)
    {
        if (message.Sender != ClientData.HostName || args.Length <= 1)
        {
            return;
        }

        var clientId = args[1];
        Master.Unban(clientId);
    }

    private void OnNextDelete(Message message, string[] args)
    {
        if (!ClientData.IsWaiting ||
            ClientData.Decision != DecisionType.NextPersonFinalThemeDeleting ||
            message.Sender != ClientData.ShowMan.Name ||
            args.Length <= 1 ||
            !int.TryParse(args[1], out int playerIndex) ||
            playerIndex <= -1 ||
            playerIndex >= ClientData.Players.Count ||
            !ClientData.Players[playerIndex].Flag)
        {
            return;
        }

        try
        {
            ClientData.ThemeDeleters?.Current.SetIndex(playerIndex);
        }
        catch (Exception exc)
        {
            throw new InvalidOperationException(
                $"SetIndex error. Person history: {ClientData.PersonsUpdateHistory}; Logic history: {Logic.PrintHistory()}",
                exc);
        }

        _logic.Stop(StopReason.Decision);
    }

    private void OnPicture(Message message, string[] args)
    {
        var path = args[1];
        var person = ClientData.MainPersons.FirstOrDefault(item => message.Sender == item.Name);

        if (person == null)
        {
            return;
        }

        if (args.Length > 2)
        {
            if (!ClientData.BackLink.AreCustomAvatarsSupported)
            {
                return;
            }

            var file = $"{message.Sender}_{Path.GetFileName(path)}";

            if (!_avatarHelper.FileExists(file))
            {
                var error = _avatarHelper.ExtractAvatarData(args[2], file);

                if (error != null)
                {
                    _gameActions.SendMessageToWithArgs(message.Sender, Messages.Replic, ReplicCodes.Special.ToString(), error);
                    return;
                }
            }

            var uri = _fileShare.CreateResourceUri(Clients.ResourceKind.Avatar, new Uri(file, UriKind.Relative));

            person.Picture = $"URI: {uri}";
        }
        else
        {
            person.Picture = path;
        }

        InformAvatar(person);
    }

    private void OnStake(Message message, string[] args)
    {
        if (!ClientData.IsWaiting ||
            ClientData.Decision != DecisionType.StakeMaking ||
            (ClientData.ActivePlayer == null || message.Sender != ClientData.ActivePlayer.Name)
            && (!ClientData.IsOralNow || message.Sender != ClientData.ShowMan.Name))
        {
            return;
        }

        if (!int.TryParse(args[1], out var stakeType) || stakeType < 0 || stakeType > 3)
        {
            return;
        }

        ClientData.StakeType = (StakeMode)stakeType;

        if (!ClientData.StakeVariants[(int)ClientData.StakeType])
        {
            ClientData.StakeType = null;
        }
        else if (ClientData.StakeType == StakeMode.Sum)
        {
            var minimum = ClientData.Stake != -1 ? ClientData.Stake + 100 : ClientData.CurPriceRight + 100;
            
            // TODO: optimize
            while (minimum % 100 != 0)
            {
                minimum++;
            }

            if (!int.TryParse(args[2], out var stakeSum))
            {
                ClientData.StakeType = null;
                return;
            }

            if (stakeSum < minimum || stakeSum > ClientData.ActivePlayer.Sum || stakeSum % 100 != 0)
            {
                ClientData.StakeType = null;
                return;
            }

            ClientData.StakeSum = stakeSum;
        }

        if (ClientData.IsOralNow)
        {
            if (message.Sender == ClientData.ShowMan.Name)
            {
                if (ClientData.ActivePlayer != null)
                {
                    _gameActions.SendMessage(Messages.Cancel, ClientData.ActivePlayer.Name);
                }
            }
            else
            {
                _gameActions.SendMessage(Messages.Cancel, ClientData.ShowMan.Name);
            }
        }

        _logic.Stop(StopReason.Decision);
    }

    private void OnInfo(Message message)
    {
        Inform(message.Sender);

        foreach (var item in ClientData.MainPersons)
        {
            if (item.Ready)
            {
                _gameActions.SendMessage($"{Messages.Ready}\n{item.Name}", message.Sender);
            }
        }

        _gameActions.InformStage(message.Sender);
        _gameActions.InformSums(message.Sender);

        if (ClientData.Stage != GameStage.Before)
        {
            _gameActions.InformRoundsNames(message.Sender);
        }

        if (ClientData.Stage == GameStage.Round)
        {
            ClientData.TableInformStageLock.WithLock(() =>
            {
                if (ClientData.TableInformStage > 0)
                {
                    _gameActions.InformRoundThemes(message.Sender, false);

                    if (ClientData.TableInformStage > 1)
                    {
                        _gameActions.InformTable(message.Sender);
                    }
                }
            },
            5000);

            _gameActions.InformRoundContent(message.Sender);
        }
        else if (ClientData.Stage == GameStage.Before && ClientData.Settings.IsAutomatic)
        {
            var leftTimeBeforeStart = Constants.AutomaticGameStartDuration - (int)(DateTime.UtcNow - ClientData.TimerStartTime[2]).TotalSeconds * 10;

            if (leftTimeBeforeStart > 0)
            {
                _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.Timer, 2, MessageParams.Timer_Go, leftTimeBeforeStart, -2), message.Sender);
            }
        }
    }

    private void OnDelete(Message message, string[] args)
    {
        if (!ClientData.IsWaiting ||
            ClientData.Decision != DecisionType.FinalThemeDeleting ||
            ClientData.ActivePlayer == null ||
            message.Sender != ClientData.ActivePlayer.Name && (!ClientData.IsOralNow || message.Sender != ClientData.ShowMan.Name))
        {
            return;
        }

        if (!int.TryParse(args[1], out int themeIndex) || themeIndex <= -1 || themeIndex >= ClientData.TInfo.RoundInfo.Count)
        {
            return;
        }

        if (ClientData.TInfo.RoundInfo[themeIndex].Name == QuestionHelper.InvalidThemeName)
        {
            return;
        }

        ClientData.ThemeIndexToDelete = themeIndex;

        if (ClientData.IsOralNow)
        {
            _gameActions.SendMessage(Messages.Cancel, ClientData.ShowMan.Name);
        }

        if (Logic.CanPlayerAct())
        {
            _gameActions.SendMessage(Messages.Cancel, ClientData.ActivePlayer.Name);
        }

        _logic.Stop(StopReason.Decision);
    }

    private void OnDisconnect(string[] args)
    {
        if (args.Length < 3 || !ClientData.AllPersons.TryGetValue(args[1], out var account))
        {
            return;
        }

        var withError = args[2] == "+";

        var res = new StringBuilder()
            .Append(LO[account.IsMale ? nameof(R.Disconnected_Male) : nameof(R.Disconnected_Female)])
            .Append(' ')
            .Append(account.Name);

        _gameActions.SpecialReplic(res.ToString());
        _gameActions.SendMessageWithArgs(Messages.Disconnected, account.Name);

        ClientData.BeginUpdatePersons($"Disconnected {account.Name}");

        try
        {
            account.IsConnected = false;

            if (ClientData.Viewers.Contains(account))
            {
                ClientData.Viewers.Remove(account);
            }
            else
            {
                var isBefore = ClientData.Stage == GameStage.Before;

                if (account is GamePersonAccount person)
                {
                    person.Name = Constants.FreePlace;
                    person.Picture = "";

                    if (isBefore)
                    {
                        person.Ready = false;
                    }
                }
            }
        }
        finally
        {
            ClientData.EndUpdatePersons();
        }

        if (args[1] == ClientData.HostName)
        {
            // A new host must be assigned if possible.
            // The host is assigned randomly

            SelectNewHost();

            if (ClientData.Settings.AppSettings.Managed && !_logic.IsRunning)
            {
                if (_logic.StopReason == StopReason.Pause || ClientData.TInfo.Pause)
                {
                    _logic.AddHistory($"Managed game pause autoremoved.");
                    OnPauseCore(false);
                    return;
                }

                _logic.AddHistory($"Managed game move autostarted.");

                ClientData.MoveDirection = MoveDirections.Next;
                _logic.Stop(StopReason.Move);
            }
        }

        OnPersonsChanged(false, withError);
    }

    private async ValueTask OnConnectAsync(Message message, string[] args)
    {
        if (args.Length < 4)
        {
            _gameActions.SendMessage(SystemMessages.Refuse + Message.ArgsSeparatorChar + LO[nameof(R.WrongConnectionParameters)], message.Sender);
            return;
        }

        var role = args[1];
        var name = args[2];
        var sex = args[3];

        if (ClientData.JoinMode == JoinMode.Forbidden)
        {
            _gameActions.SendMessage(SystemMessages.Refuse + Message.ArgsSeparatorChar + LO[nameof(R.JoinForbidden)], message.Sender);
            return;
        }

        if (ClientData.JoinMode == JoinMode.OnlyViewer && role != Constants.Viewer)
        {
            _gameActions.SendMessage(SystemMessages.Refuse + Message.ArgsSeparatorChar + LO[nameof(R.JoinRoleForbidden)], message.Sender);
            return;
        }

        if (!string.IsNullOrEmpty(ClientData.Settings.NetworkGamePassword) && (args.Length < 6 || ClientData.Settings.NetworkGamePassword != args[5]))
        {
            _gameActions.SendMessage(SystemMessages.Refuse + Message.ArgsSeparatorChar + LO[nameof(R.WrongPassword)], message.Sender);
            return;
        }

        if (ClientData.AllPersons.ContainsKey(name))
        {
            _gameActions.SendMessage(SystemMessages.Refuse + Message.ArgsSeparatorChar + string.Format(LO[nameof(R.PersonWithSuchNameIsAlreadyInGame)], name), message.Sender);
            return;
        }

        var index = -1;
        IEnumerable<ViewerAccount> accountsToSearch;

        switch (role)
        {
            case Constants.Showman:
                accountsToSearch = new ViewerAccount[1] { ClientData.ShowMan };
                break;

            case Constants.Player:
                accountsToSearch = ClientData.Players;

                if (ClientData.HostName == name) // Подключение организатора
                {
                    var defaultPlayers = ClientData.Settings.Players;
                    for (var i = 0; i < defaultPlayers.Length; i++)
                    {
                        if (defaultPlayers[i].Name == name)
                        {
                            index = i;
                            break;
                        }
                    }

                    if (index < 0 || index >= ClientData.Players.Count)
                    {
                        _gameActions.SendMessage(string.Join(Message.ArgsSeparator, SystemMessages.Refuse, LO[nameof(R.PositionNotFoundByIndex)]), message.Sender);
                        return;
                    }
                }

                break;

            default:
                accountsToSearch = ClientData.Viewers.Concat(new ViewerAccount[] { new ViewerAccount(Constants.FreePlace, false, false) { IsHuman = true } });
                break;
        }

        var found = false;

        if (index > -1)
        {
            var accounts = accountsToSearch.ToArray();

            var (result, foundLocal) = await CheckAccountAsync(message, role, name, sex, index, accounts[index]);
            if (result.HasValue)
            {
                if (!result.Value)
                {
                    return;
                }
            }

            found |= foundLocal;
        }
        else
        {
            foreach (var item in accountsToSearch)
            {
                index++;
                var (result, foundLocal) = await CheckAccountAsync(message, role, name, sex, index, item);

                found |= foundLocal;

                if (result.HasValue)
                {
                    if (!result.Value)
                    {
                        return;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        if (!found)
        {
            _gameActions.SendMessage($"{SystemMessages.Refuse}{Message.ArgsSeparatorChar}{LO[nameof(R.NoFreePlaceForName)]}", message.Sender);
        }
    }

    private void SelectNewHost()
    {
        static bool canBeHost(ViewerAccount account) => account.IsHuman && account.IsConnected;

        string? newHostName = null;

        if (canBeHost(ClientData.ShowMan))
        {
            newHostName = ClientData.ShowMan.Name;
        }
        else
        {
            var availablePlayers = ClientData.Players.Where(canBeHost).ToArray();

            if (availablePlayers.Length > 0)
            {
                var index = Random.Shared.Next(availablePlayers.Length);
                newHostName = availablePlayers[index].Name;
            }
            else
            {
                var availableViewers = ClientData.Viewers.Where(canBeHost).ToArray();

                if (availableViewers.Length > 0)
                {
                    var index = Random.Shared.Next(availableViewers.Length);
                    newHostName = availableViewers[index].Name;
                }
            }
        }

        UpdateHostName(newHostName);
    }

    private void UpdateHostName(string? newHostName, string source = "")
    {
        ClientData.HostName = newHostName;
        _gameActions.SendMessageWithArgs(Messages.Hostname, newHostName ?? "", source);
    }

    private void OnApellation(Message message, string[] args)
    {
        if (!ClientData.AllowAppellation)
        {
            if (ClientData.AppellationOpened)
            {
                // TODO: save appellation request and return to it after question finish
                // Merge AppellationOpened and AllowAppellation properties into triple-state property
            }

            return;
        }

        ClientData.IsAppelationForRightAnswer = args.Length == 1 || args[1] == "+";
        ClientData.AppellationSource = message.Sender;

        ClientData.AppellationCallerIndex = -1;
        ClientData.AppelaerIndex = -1;

        if (ClientData.IsAppelationForRightAnswer)
        {
            for (var i = 0; i < ClientData.Players.Count; i++)
            {
                if (ClientData.Players[i].Name == message.Sender)
                {
                    for (var j = 0; j < ClientData.QuestionHistory.Count; j++)
                    {
                        var index = ClientData.QuestionHistory[j].PlayerIndex;

                        if (index == i)
                        {
                            if (!ClientData.QuestionHistory[j].IsRight)
                            {
                                ClientData.AppelaerIndex = index;
                            }

                            break;
                        }
                    }

                    break;
                }
            }
        }
        else
        {
            if (ClientData.Players.Count <= 3)
            {
                // If there are 2 or 3 players, there are already 2 positive votes for the answer
                // from answered player and showman. And only 1 or 2 votes left.
                // So there is no chance to win a vote against the answer
                _gameActions.SpecialReplic(string.Format(LO[nameof(R.FailedToAppellateForWrongAnswer)], message.Sender));
                return;
            }

            for (var i = 0; i < ClientData.Players.Count; i++)
            {
                if (ClientData.Players[i].Name == message.Sender)
                {
                    ClientData.AppellationCallerIndex = i;
                    break;
                }
            }

            if (ClientData.AppellationCallerIndex == -1)
            {
                // Only players can appellate
                return;
            }

            // Last person has right answer and is responsible for appellation
            var count = ClientData.QuestionHistory.Count;

            if (count > 0 && ClientData.QuestionHistory[count - 1].IsRight)
            {
                ClientData.AppelaerIndex = ClientData.QuestionHistory[count - 1].PlayerIndex;
            }
        }

        if (ClientData.AppelaerIndex != -1)
        {
            // Appellation started
            ClientData.AllowAppellation = false;
            _logic.Stop(StopReason.Appellation);
        }
    }

    private void OnCatCost(Message message, string[] args)
    {
        if (!ClientData.IsWaiting ||
            ClientData.Decision != DecisionType.QuestionPriceSelection ||
            (ClientData.Answerer == null || message.Sender != ClientData.Answerer.Name) &&
            (!ClientData.IsOralNow || message.Sender != ClientData.ShowMan.Name))
        {
            return;
        }

        if (int.TryParse(args[1], out int sum)
            && sum >= ClientData.CatInfo.Minimum
            && sum <= ClientData.CatInfo.Maximum
            && (sum - ClientData.CatInfo.Minimum) % ClientData.CatInfo.Step == 0)
        {
            ClientData.CurPriceRight = sum;
        }

        if (ClientData.IsOralNow)
        {
            _gameActions.SendMessage(Messages.Cancel, ClientData.ShowMan.Name);
        }

        if (Logic.CanPlayerAct() && ClientData.Answerer != null)
        {
            _gameActions.SendMessage(Messages.Cancel, ClientData.Answerer.Name);
        }

        _logic.Stop(StopReason.Decision);
    }

    private void OnChanged(Message message, string[] args)
    {
        if (message.Sender != ClientData.ShowMan.Name || args.Length != 3)
        {
            return;
        }

        if (!int.TryParse(args[1], out var playerIndex) ||
            !int.TryParse(args[2], out var sum) ||
            playerIndex < 1 ||
            playerIndex > ClientData.Players.Count)
        {
            return;
        }

        var player = ClientData.Players[playerIndex - 1];
        player.Sum = sum;

        _gameActions.SpecialReplic($"{ClientData.ShowMan.Name} {LO[nameof(R.Change1)]} {player.Name}{LO[nameof(R.Change3)]} {Notion.FormatNumber(player.Sum)}");
        _gameActions.InformSums();

        _logic.AddHistory($"Sum change: {playerIndex - 1} = {sum}");
    }

    private void OnMove(Message message, string[] args)
    {
        if (message.Sender != ClientData.HostName && message.Sender != ClientData.ShowMan.Name || args.Length <= 1)
        {
            return;
        }

        if (!int.TryParse(args[1], out int direction))
        {
            return;
        }

        var moveDirection = (MoveDirections)direction;

        if (moveDirection < MoveDirections.RoundBack || moveDirection > MoveDirections.Round)
        {
            return;
        }

        switch (moveDirection)
        {
            case MoveDirections.RoundBack:
                if (!_logic.Engine.CanMoveBackRound)
                {
                    return;
                }

                break;

            case MoveDirections.Back:
                if (!_logic.Engine.CanMoveBack)
                {
                    return;
                }

                break;

            case MoveDirections.Next:
                if (ClientData.MoveNextBlocked)
                {
                    return;
                }

                break;

            case MoveDirections.RoundNext:
                if (!_logic.Engine.CanMoveNextRound)
                {
                    return;
                }

                break;

            case MoveDirections.Round:
                if (!_logic.Engine.CanMoveNextRound && !_logic.Engine.CanMoveBackRound ||
                    ClientData.Package == null ||
                    args.Length <= 2 ||
                    !int.TryParse(args[2], out int roundIndex) ||
                    roundIndex < 0 ||
                    roundIndex >= ClientData.Rounds.Length ||
                    ClientData.Rounds[roundIndex].Index == _logic.Engine.RoundIndex)
                {
                    return;
                }

                ClientData.TargetRoundIndex = ClientData.Rounds[roundIndex].Index;
                break;
        }

        // Resume paused game
        if (ClientData.TInfo.Pause)
        {
            OnPauseCore(false);
            return;
        }

        _logic.AddHistory($"Move started: {ClientData.MoveDirection}");

        ClientData.MoveDirection = moveDirection;
        _logic.Stop(StopReason.Move);
    }

    private void OnReady(Message message, string[] args)
    {
        if (ClientData.Stage != GameStage.Before)
        {
            return;
        }

        var res = new StringBuilder();

        // Player or showman is ready to start the game
        res.Append(Messages.Ready).Append(Message.ArgsSeparatorChar);

        var readyAll = true;
        var found = false;
        var toReady = args.Length == 1 || args[1] == "+";

        foreach (var item in ClientData.MainPersons)
        {
            if (message.Sender == item.Name && (toReady && !item.Ready || !toReady && item.Ready))
            {
                item.Ready = toReady;
                res.Append(message.Sender).Append(Message.ArgsSeparatorChar).Append(toReady ? "+" : "-");
                found = true;
            }

            readyAll = readyAll && item.Ready;
        }

        if (found)
        {
            _gameActions.SendMessage(res.ToString());
        }

        if (readyAll)
        {
            StartGame();
        }
        else if (ClientData.Settings.IsAutomatic)
        {
            if (ClientData.Players.All(player => player.IsConnected))
            {
                StartGame();
            }
        }
    }

    private void OnPause(Message message, string[] args)
    {
        if (message.Sender != ClientData.HostName && message.Sender != ClientData.ShowMan.Name || args.Length <= 1)
        {
            return;
        }

        OnPauseCore(args[1] == "+");
    }

    private void OnPauseCore(bool isPauseEnabled)
    {
        // Game host or showman requested a game pause

        if (isPauseEnabled)
        {
            if (ClientData.TInfo.Pause)
            {
                return;
            }

            if (_logic.Stop(StopReason.Pause))
            {
                ClientData.TInfo.Pause = true;
                Logic.AddHistory("Pause activated");
            }

            return;
        }

        if (_logic.StopReason == StopReason.Pause)
        {
            // We are currently moving into pause mode. Resuming
            ClientData.TInfo.Pause = false;
            _logic.AddHistory("Immediate pause resume");
            _logic.CancelStop();
            return;
        }

        if (!ClientData.TInfo.Pause)
        {
            return;
        }

        ClientData.TInfo.Pause = false;

        var pauseDuration = DateTime.UtcNow.Subtract(ClientData.PauseStartTime);

        var times = new int[Constants.TimersCount];

        for (var i = 0; i < Constants.TimersCount; i++)
        {
            times[i] = (int)(ClientData.PauseStartTime.Subtract(ClientData.TimerStartTime[i]).TotalMilliseconds / 100);
            ClientData.TimerStartTime[i] = ClientData.TimerStartTime[i].Add(pauseDuration);
        }

        if (ClientData.IsPlayingMediaPaused)
        {
            ClientData.IsPlayingMediaPaused = false;
            ClientData.IsPlayingMedia = true;
        }

        if (ClientData.IsThinkingPaused)
        {
            ClientData.IsThinkingPaused = false;
            ClientData.IsThinking = true;
        }

        _logic.AddHistory($"Pause resumed ({_logic.PrintOldTasks()} {_logic.StopReason})");

        try
        {
            var maxPressingTime = ClientData.Settings.AppSettings.TimeSettings.TimeForThinkingOnQuestion * 10;
            times[1] = maxPressingTime - _logic.ResumeExecution();
        }
        catch (Exception exc)
        {
            throw new Exception($"Resume execution error: {_logic.PrintHistory()}", exc);
        }

        if (_logic.StopReason == StopReason.Decision)
        {
            _logic.ExecuteImmediate(); // Decision could be ready
        }

        _gameActions.SpecialReplic(LO[nameof(R.GameResumed)]);
        _gameActions.SendMessageWithArgs(Messages.Pause, isPauseEnabled ? '+' : '-', times[0], times[1], times[2]);
    }

    private void OnAtom()
    {
        ClientData.HaveViewedAtom--;

        if (!ClientData.IsPlayingMedia || ClientData.TInfo.Pause)
        {
            return;
        }

        if (ClientData.HaveViewedAtom <= 0)
        {
            ClientData.IsPlayingMedia = false;

            _logic.ExecuteImmediate();
        }
        else
        {
            // Иногда кто-то отваливается, и процесс затягивается на 60 секунд. Это недопустимо. Дадим 3 секунды
            _logic.ScheduleExecution(Tasks.MoveNext, 30 + ClientData.Settings.AppSettings.TimeSettings.TimeForMediaDelay * 10, force: true);
        }
    }

    private void OnAnswerVersion(Message message, string[] args)
    {
        if (ClientData.Decision != DecisionType.Answering || args[1].Length == 0)
        {
            return;
        }

        if (ClientData.Round != null && ClientData.Round.Type == RoundTypes.Final)
        {
            ClientData.AnswererIndex = -1;

            for (var i = 0; i < ClientData.Players.Count; i++)
            {
                if (ClientData.Players[i].Name == message.Sender && ClientData.QuestionPlayState.AnswererIndicies.Contains(i))
                {
                    ClientData.AnswererIndex = i;
                    break;
                }
            }

            if (ClientData.AnswererIndex == -1)
            {
                return;
            }
        }
        else if (!ClientData.IsWaiting || ClientData.Answerer != null && ClientData.Answerer.Name != message.Sender)
        {
            return;
        }

        if (ClientData.Answerer == null || !ClientData.Answerer.IsHuman)
        {
            return;
        }

        ClientData.Answerer.Answer = args[1];
    }

    private void OnAnswer(Message message, string[] args)
    {
        if (ClientData.Decision != DecisionType.Answering)
        {
            return;
        }

        if (ClientData.Round != null && ClientData.Round.Type == RoundTypes.Final)
        {
            ClientData.AnswererIndex = -1;

            for (var i = 0; i < ClientData.Players.Count; i++)
            {
                if (ClientData.Players[i].Name == message.Sender && ClientData.QuestionPlayState.AnswererIndicies.Contains(i))
                {
                    ClientData.AnswererIndex = i;

                    _gameActions.SendMessageWithArgs(Messages.PersonFinalAnswer, i);
                    break;
                }
            }

            if (ClientData.AnswererIndex == -1)
            {
                return;
            }
        }
        else if (!ClientData.IsWaiting || ClientData.Answerer != null && ClientData.Answerer.Name != message.Sender)
        {
            return;
        }

        if (ClientData.Answerer == null)
        {
            return;
        }

        if (!ClientData.Answerer.IsHuman)
        {
            if (args[1] == MessageParams.Answer_Right)
            {
                ClientData.Answerer.Answer = args[2].Replace(Constants.AnswerPlaceholder, ClientData.Question.Right.FirstOrDefault() ?? "(...)");
                ClientData.Answerer.AnswerIsWrong = false;
            }
            else
            {
                ClientData.Answerer.AnswerIsWrong = true;

                var restwrong = new List<string>();

                foreach (var wrong in ClientData.Question.Wrong)
                {
                    if (!ClientData.UsedWrongVersions.Contains(wrong))
                    {
                        restwrong.Add(wrong);
                    }
                }

                var wrongAnswers = LO[nameof(R.WrongAnswer)].Split(';');
                var wrongCount = restwrong.Count;

                if (wrongCount == 0)
                {
                    for (int i = 0; i < wrongAnswers.Length; i++)
                    {
                        if (!ClientData.UsedWrongVersions.Contains(wrongAnswers[i]))
                        {
                            restwrong.Add(wrongAnswers[i]);
                        }
                    }

                    if (!ClientData.UsedWrongVersions.Contains(LO[nameof(R.NoAnswer)]))
                    {
                        restwrong.Add(LO[nameof(R.NoAnswer)]);
                    }
                }

                wrongCount = restwrong.Count;

                if (wrongCount == 0)
                {
                    restwrong.Add(wrongAnswers[0]);
                    wrongCount = 1;
                }

                int wrongIndex = Random.Shared.Next(wrongCount);

                ClientData.UsedWrongVersions.Add(restwrong[wrongIndex]);
                ClientData.Answerer.Answer = args[2].Replace("#", restwrong[wrongIndex]);
            }

            ClientData.Answerer.Answer = ClientData.Answerer.Answer.GrowFirstLetter();
        }
        else
        {
            if (args[1].Length > 0)
            {
                ClientData.Answerer.Answer = args[1];
                ClientData.Answerer.AnswerIsWrong = false;
            }
            else
            {
                ClientData.Answerer.Answer = LO[nameof(R.IDontKnow)];
                ClientData.Answerer.AnswerIsWrong = true;
            }
        }

        if (ClientData.Round.Type != RoundTypes.Final)
        {
            _logic.Stop(StopReason.Decision);
        }
    }

    private void OnIsRight(Message message, string[] args)
    {
        if (!ClientData.IsWaiting || args.Length <= 1)
        {
            return;
        }

        if ((ClientData.Decision == DecisionType.AnswerValidating || ClientData.IsOralNow && ClientData.Decision == DecisionType.Answering) &&
            ClientData.ShowMan != null &&
            message.Sender == ClientData.ShowMan.Name &&
            ClientData.Answerer != null)
        {
            ClientData.Decision = DecisionType.AnswerValidating;
            ClientData.Answerer.AnswerIsRight = args[1] == "+";
            ClientData.Answerer.AnswerIsRightFactor = args.Length > 2 && double.TryParse(args[2], out var factor) && factor > 0.0 ? factor : 1.0;
            ClientData.ShowmanDecision = true;

            _logic.Stop(StopReason.Decision);
            return;
        }

        if (ClientData.Decision == DecisionType.AppellationDecision)
        {
            for (var i = 0; i < ClientData.Players.Count; i++)
            {
                if (ClientData.Players[i].Flag && ClientData.Players[i].Name == message.Sender)
                {
                    if (args[1] == "+")
                    {
                        ClientData.AppellationPositiveVoteCount++;
                    }
                    else
                    {
                        ClientData.AppellationNegativeVoteCount++;
                    }

                    ClientData.Players[i].Flag = false;
                    ClientData.AppellationAwaitedVoteCount--;
                    _gameActions.SendMessageWithArgs(Messages.PersonApellated, i);
                    break;
                }
            }

            if (ClientData.AppellationAwaitedVoteCount == 0)
            {
                _logic.Stop(StopReason.Decision);
            }

            var halfVotesCount = ClientData.AppellationTotalVoteCount / 2;

            if (ClientData.AppellationPositiveVoteCount > halfVotesCount || ClientData.AppellationNegativeVoteCount > halfVotesCount)
            {
                SendCancellationsToActivePlayers();
                _logic.Stop(StopReason.Decision);
            }
        }
    }

    private void SendCancellationsToActivePlayers()
    {
        foreach (var player in ClientData.Players)
        {
            if (player.Flag)
            {
                _gameActions.SendMessage(Messages.Cancel, player.Name);
            }
        }
    }

    private void OnPass(Message message)
    {
        if (!ClientData.IsQuestionPlaying)
        {
            return;
        }

        var canPressChanged = false;

        for (var i = 0; i < ClientData.Players.Count; i++)
        {
            var player = ClientData.Players[i];

            if (player.Name == message.Sender && player.CanPress)
            {
                player.CanPress = false;
                _gameActions.SendMessageWithArgs(Messages.Pass, i);
                canPressChanged = true;
                break;
            }
        }

        if (canPressChanged && ClientData.Players.All(p => !p.CanPress) && ClientData.Decision == DecisionType.Pressing && !ClientData.TInfo.Pause)
        {
            if (!ClientData.IsAnswer)
            {
                if (!ClientData.IsQuestionFinished)
                {
                    _logic.Engine.MoveToAnswer();
                }

                _logic.ExecuteImmediate();
            }
        }
    }

    /// <summary>
    /// Handles player button press.
    /// </summary>
    /// <param name="playerName">Pressed player name.</param>
    private void OnI(string playerName)
    {
        if (ClientData.TInfo.Pause)
        {
            return;
        }

        if (ClientData.Decision != DecisionType.Pressing)
        {
            // Just show that the player has misfired the button
            HandlePlayerMisfire(playerName);
            return;
        }

        // Detect possible answerer
        var answererIndex = DetectAnswererIndex(playerName);

        if (answererIndex == -1)
        {
            return;
        }

        if (!ClientData.Settings.AppSettings.UsePingPenalty) // Default mode without penalties
        {
            ClientData.PendingAnswererIndex = answererIndex;

            if (_logic.Stop(StopReason.Answer))
            {
                ClientData.Decision = DecisionType.None;
            }

            return;
        }

        // Special mode when answerer with penalty waits a little bit while other players with less penalty could try to press
        ProcessPenalizedAnswerer(answererIndex);
    }

    private void ProcessPenalizedAnswerer(int answererIndex)
    {
        var penalty = ClientData.Players[answererIndex].PingPenalty;
        var penaltyStartTime = DateTime.UtcNow;

        if (ClientData.IsDeferringAnswer)
        {
            var futureTime = penaltyStartTime.AddMilliseconds(penalty * 100);
            var currentFutureTime = ClientData.PenaltyStartTime.AddMilliseconds(ClientData.Penalty * 100);

            if (futureTime >= currentFutureTime) // New answerer candidate has bigger penalized time so he looses the hit
            {
                return;
            }
        }

        ClientData.PendingAnswererIndex = answererIndex;

        if (penalty == 0) // Act like in mode without penalty
        {
            if (_logic.Stop(StopReason.Answer))
            {
                ClientData.Decision = DecisionType.None;
            }
        }
        else
        {
            ClientData.PenaltyStartTime = penaltyStartTime;
            ClientData.Penalty = penalty;

            _logic.Stop(StopReason.Wait);
        }
    }

    private int DetectAnswererIndex(string playerName)
    {
        var answererIndex = -1;
        var blockingButtonTime = ClientData.Settings.AppSettings.TimeSettings.TimeForBlockingButton;

        for (var i = 0; i < ClientData.Players.Count; i++)
        {
            var player = ClientData.Players[i];

            if (player.Name == playerName &&
                player.CanPress &&
                DateTime.UtcNow.Subtract(player.LastBadTryTime).TotalSeconds >= blockingButtonTime)
            {
                answererIndex = i;
                break;
            }
        }

        return answererIndex;
    }

    private void HandlePlayerMisfire(string playerName)
    {
        for (var i = 0; i < ClientData.Players.Count; i++)
        {
            var player = ClientData.Players[i];

            if (player.Name == playerName)
            {
                if (ClientData.Answerer != player)
                {
                    player.LastBadTryTime = DateTime.UtcNow;
                    _gameActions.SendMessageWithArgs(Messages.WrongTry, i);
                }

                return;
            }
        }
    }

    private void OnDisconnectRequested(string person) => DisconnectRequested?.Invoke(person);

    /// <summary>
    /// Изменить конфигурацию игры
    /// </summary>
    /// <param name="message"></param>
    /// <param name="args"></param>
    private void ProcessConfig(Message message, string[] args)
    {
        if (message.Sender != ClientData.HostName || args.Length <= 1)
        {
            return;
        }

        if (ClientData.HostName == null || !ClientData.AllPersons.TryGetValue(ClientData.HostName, out var host))
        {
            return;
        }

        switch (args[1])
        {
            case MessageParams.Config_AddTable:
                AddTable(message, host);
                break;

            case MessageParams.Config_DeleteTable:
                DeleteTable(message, args, host);
                break;

            case MessageParams.Config_Free:
                FreeTable(message, args, host);
                break;

            case MessageParams.Config_Set:
                SetPerson(args, host);
                break;

            case MessageParams.Config_ChangeType:
                if (ClientData.Stage == GameStage.Before && args.Length > 2)
                {
                    ChangePersonType(args[2], args.Length < 4 ? "" : args[3], host);
                }
                break;
        }
    }

    private void AddTable(Message message, Account host)
    {
        if (ClientData.Players.Count >= Constants.MaxPlayers)
        {
            return;
        }

        var newAccount = new ViewerAccount(Constants.FreePlace, false, false) { IsHuman = true };

        ClientData.BeginUpdatePersons("AddTable " + message.Text);

        try
        {
            ClientData.Players.Add(new GamePlayerAccount(newAccount));
            Logic.AddHistory($"Player added (total: {ClientData.Players.Count})");
        }
        finally
        {
            ClientData.EndUpdatePersons();
        }

        var info = new StringBuilder(Messages.Config).Append(Message.ArgsSeparatorChar)
            .Append(MessageParams.Config_AddTable).Append(Message.ArgsSeparatorChar);

        AppendAccountExt(newAccount, info);

        _gameActions.SendMessage(info.ToString());
        _gameActions.SpecialReplic($"{ClientData.HostName} {ResourceHelper.GetSexString(LO[nameof(R.Sex_Added)], host.IsMale)} {LO[nameof(R.NewGameTable)]}");
        OnPersonsChanged();
    }

    private void DeleteTable(Message message, string[] args, Account host)
    {
        if (args.Length <= 2)
        {
            return;
        }

        var indexStr = args[2];
        if (ClientData.Players.Count <= 2 || !int.TryParse(indexStr, out int index) || index <= -1
            || index >= ClientData.Players.Count)
        {
            return;
        }

        var account = ClientData.Players[index];
        var isOnline = account.IsConnected;

        if (ClientData.Stage != GameStage.Before && account.IsHuman && isOnline)
        {
            return;
        }

        ClientData.BeginUpdatePersons("DeleteTable " + message.Text);

        try
        {
            ClientData.Players.RemoveAt(index);
            Logic.AddHistory($"Player removed at {index}");

            try
            {
                DropPlayerIndex(index);
            }
            catch (Exception exc)
            {
                throw new InvalidOperationException(
                    $"DropPlayerIndex error. Persons history: {ClientData.PersonsUpdateHistory}; logic history: {Logic.PrintHistory()}",
                    exc);
            }

            if (isOnline && account.IsHuman)
            {
                ClientData.Viewers.Add(account);
            }
        }
        finally
        {
            ClientData.EndUpdatePersons();
        }

        if (!account.IsHuman)
        {
            // Удалить клиента компьютерного игрока
            if (!_client.Node.DeleteClient(account.Name))
            {
                _client.Node.OnError(new Exception($"Cannot delete client {account.Name}"), true);
            }
            else if (_client.Node.Contains(account.Name))
            {
                _client.Node.OnError(new Exception($"Client {account.Name} was deleted but is still present on the server!"), true);
            }
        }

        _gameActions.SendMessageWithArgs(Messages.Config, MessageParams.Config_DeleteTable, index);
        _gameActions.SpecialReplic($"{ClientData.HostName} {ResourceHelper.GetSexString(LO[nameof(R.Sex_Deleted)], host.IsMale)} {LO[nameof(R.GameTableNumber)]} {index + 1}");

        if (ClientData.Stage == GameStage.Before)
        {
            var readyAll = ClientData.MainPersons.All(p => p.Ready);

            if (readyAll)
            {
                StartGame();
            }
        }

        OnPersonsChanged();
    }

    private void PlanExecution(Tasks task, double taskTime, int arg = 0)
    {
        Logic.AddHistory($"PlanExecution {task} {taskTime} {arg} ({ClientData.TInfo.Pause})");

        if (Logic.IsExecutionPaused)
        {
            Logic.UpdatePausedTask((int)task, arg, (int)taskTime);
        }
        else
        {
            Logic.ScheduleExecution(task, taskTime, arg);
        }
    }

    /// <summary>
    /// Correctly removes player from the game adjusting game state.
    /// </summary>
    /// <param name="playerIndex">Index of the player to remove.</param>
    private void DropPlayerIndex(int playerIndex)
    {
        if (ClientData.ChooserIndex > playerIndex)
        {
            ClientData.ChooserIndex--;
        }
        else if (ClientData.ChooserIndex == playerIndex)
        {
            DropCurrentChooser();
        }

        ClientData.QuestionPlayState.RemovePlayer(playerIndex);

        if (ClientData.AnswererIndex > playerIndex)
        {
            ClientData.AnswererIndex--;
        }
        else if (ClientData.AnswererIndex == playerIndex)
        {
            DropCurrentAnswerer();
        }

        if (ClientData.AppelaerIndex > playerIndex)
        {
            ClientData.AppelaerIndex--;
        }
        else if (ClientData.AppelaerIndex == playerIndex)
        {
            DropCurrentAppelaer();
        }

        if (ClientData.StakerIndex > playerIndex)
        {
            ClientData.StakerIndex--;
        }
        else if (ClientData.StakerIndex == playerIndex)
        {
            DropCurrentStaker();
        }

        if (ClientData.Question != null &&
            ((ClientData.Question.TypeName ?? ClientData.Type?.Name) == QuestionTypes.Auction
            || (ClientData.Question.TypeName ?? ClientData.Type?.Name) == QuestionTypes.Stake))
        {
            DropPlayerFromStakes(playerIndex);
        }

        if (Logic.IsFinalRound())
        {
            DropPlayerFromAnnouncing(playerIndex);
            DropPlayerFromFinalRound(playerIndex);
        }

        DropPlayerFromQuestionHistory(playerIndex);

        if (!ClientData.IsWaiting)
        {
            return;
        }

        switch (ClientData.Decision)
        {
            case DecisionType.StarterChoosing:
                // Asking again
                _gameActions.SendMessage(Messages.Cancel, ClientData.ShowMan.Name);
                _logic.StopWaiting();
                PlanExecution(Tasks.AskFirst, 20);
                break;
        }
    }

    private void DropPlayerFromQuestionHistory(int playerIndex)
    {
        var newHistory = new List<AnswerResult>();

        for (var i = 0; i < ClientData.QuestionHistory.Count; i++)
        {
            var answerResult = ClientData.QuestionHistory[i];

            if (answerResult.PlayerIndex == playerIndex)
            {
                continue;
            }

            newHistory.Add(new AnswerResult
            {
                IsRight = answerResult.IsRight,
                PlayerIndex = answerResult.PlayerIndex - (answerResult.PlayerIndex > playerIndex ? 1 : 0)
            });
        }

        ClientData.QuestionHistory.Clear();
        ClientData.QuestionHistory.AddRange(newHistory);
    }

    private void DropPlayerFromFinalRound(int playerIndex)
    {
        bool noPlayersLeft;

        if (ClientData.ThemeDeleters != null)
        {
            ClientData.ThemeDeleters.RemoveAt(playerIndex);
            noPlayersLeft = ClientData.ThemeDeleters.IsEmpty();
        }
        else
        {
            noPlayersLeft = ClientData.Players.All(p => !p.InGame);
        }

        if (noPlayersLeft)
        {
            ClientData.Decision = DecisionType.None;

            // All players that could play are removed
            if (Logic.Engine.CanMoveNextRound)
            {
                Logic.Engine.MoveNextRound(); // Finishing current round
            }
            else
            {
                // TODO: it is better to provide a correct command to game engine
                PlanExecution(Tasks.Winner, 10); // This is the last round. Finishing game
            }
        }
        else if (ClientData.Decision == DecisionType.NextPersonFinalThemeDeleting && ClientData.ThemeDeleters != null)
        {
            var indicies = ClientData.ThemeDeleters.Current.PossibleIndicies;

            for (var i = 0; i < ClientData.Players.Count; i++)
            {
                ClientData.Players[i].Flag = indicies.Contains(i);
            }
        }
    }

    private void DropPlayerFromStakes(int playerIndex)
    {
        var currentOrder = ClientData.Order;

        ClientData.OrderHistory
            .Append("DropPlayerFromStakes. Before ")
            .Append(playerIndex)
            .Append(' ')
            .Append(string.Join(",", currentOrder))
            .AppendFormat(" {0}", ClientData.OrderIndex)
            .AppendLine();

        var newOrder = new int[ClientData.Players.Count];

        for (int i = 0, j = 0; i < currentOrder.Length; i++)
        {
            if (currentOrder[i] == playerIndex)
            {
                if (ClientData.OrderIndex >= i)
                {
                    ClientData.OrderIndex--; // -1 - OK
                }
            }
            else
            {
                newOrder[j++] = currentOrder[i] - (currentOrder[i] > playerIndex ? 1 : 0);

                if (j == newOrder.Length)
                {
                    break;
                }
            }
        }

        if (ClientData.OrderIndex == currentOrder.Length - 1)
        {
            ClientData.OrderIndex = newOrder.Length - 1;
        }

        ClientData.Order = newOrder;

        ClientData.OrderHistory
            .Append("DropPlayerFromStakes. After ")
            .Append(string.Join(",", newOrder))
            .AppendFormat(" {0}", ClientData.OrderIndex)
            .AppendLine();

        if (!ClientData.Players.Any(p => p.StakeMaking))
        {
            Logic.AddHistory("Last staker dropped");
            Logic.Engine.SkipQuestion();
            PlanExecution(Tasks.MoveNext, 20, 1);
        }
        else if (ClientData.OrderIndex == -1 || ClientData.Order[ClientData.OrderIndex] == -1)
        {
            Logic.AddHistory("Current staker dropped");

            if (ClientData.Decision == DecisionType.StakeMaking || ClientData.Decision == DecisionType.NextPersonStakeMaking)
            {
                // Staker has been deleted. We need to move game futher
                Logic.StopWaiting();

                if (ClientData.IsOralNow || ClientData.Decision == DecisionType.NextPersonStakeMaking)
                {
                    _gameActions.SendMessage(Messages.Cancel, ClientData.ShowMan.Name);
                }

                ContinueMakingStakes();
            }
        }
        else if (ClientData.Decision == DecisionType.NextPersonStakeMaking)
        {
            Logic.StopWaiting();
            _gameActions.SendMessage(Messages.Cancel, ClientData.ShowMan.Name);

            ContinueMakingStakes();
        }
    }

    private void DropCurrentChooser()
    {
        // Give turn to player with least score
        // TODO: MinBy in .NET 7
        var minSum = ClientData.Players.Min(p => p.Sum);
        ClientData.ChooserIndex = ClientData.Players.TakeWhile(p => p.Sum != minSum).Count();
    }

    private void DropCurrentAppelaer()
    {
        ClientData.AppelaerIndex = -1;
        Logic.AddHistory($"AppelaerIndex dropped");
    }

    private void DropCurrentStaker()
    {
        var stakersCount = ClientData.Players.Count(p => p.StakeMaking);

        if (stakersCount == 1)
        {
            for (int i = 0; i < ClientData.Players.Count; i++)
            {
                if (ClientData.Players[i].StakeMaking)
                {
                    ClientData.StakerIndex = i;
                    Logic.AddHistory($"StakerIndex set to {i}");
                    break;
                }
            }
        }
        else
        {
            ClientData.StakerIndex = -1;
            Logic.AddHistory("StakerIndex dropped");
        }
    }

    /// <summary>
    /// Correctly removes current answerer from the game.
    /// </summary>
    private void DropCurrentAnswerer()
    {
        // Drop answerer index
        ClientData.AnswererIndex = -1;

        var nextTask = (Tasks)Logic.PendingTask;

        Logic.AddHistory(
            $"AnswererIndex dropped; nextTask = {nextTask};" +
            $" ClientData.Decision = {ClientData.Decision}; Logic.IsFinalRound() = {Logic.IsFinalRound()}");

        if ((ClientData.Decision == DecisionType.Answering ||
            ClientData.Decision == DecisionType.AnswerValidating) && !Logic.IsFinalRound())
        {
            // Answerer has been dropped. The game should be moved forward
            Logic.StopWaiting();

            if (ClientData.IsOralNow)
            {
                _gameActions.SendMessage(Messages.Cancel, ClientData.ShowMan.Name);
            }

            PlanExecution(Tasks.ContinueQuestion, 1);
        }
        else if (nextTask == Tasks.AskRight)
        {
            // Player has been removed after giving answer. But the answer has not been validated by showman yet
            if (ClientData.QuestionPlayState.AnswererIndicies.Count == 0)
            {
                PlanExecution(Tasks.ContinueQuestion, 1);
            }
            else
            {
                PlanExecution(Tasks.Announce, 15);
            }
        }
        else if (ClientData.QuestionPlayState.AnswererIndicies.Count == 0
            && ClientData.Question?.TypeName != QuestionTypes.Simple)
        {
            Logic.Engine.SkipQuestion();
            PlanExecution(Tasks.MoveNext, 20, 1);
        }
        else if (nextTask == Tasks.AnnounceStake)
        {
            PlanExecution(Tasks.Announce, 15);
        }
    }

    private void DropPlayerFromAnnouncing(int index)
    {
        if (ClientData.AnnouncedAnswerersEnumerator == null)
        {
            return;
        }

        Logic.AddHistory($"AnnouncedAnswerersEnumerator before update: {ClientData.AnnouncedAnswerersEnumerator}");
        ClientData.AnnouncedAnswerersEnumerator.Update(CustomEnumeratorUpdaters.RemoveByIndex(index));
        Logic.AddHistory($"AnnouncedAnswerersEnumerator after update: {ClientData.AnnouncedAnswerersEnumerator}");
    }

    private void ContinueMakingStakes()
    {
        if (ClientData.Players.Count(p => p.StakeMaking) == 1)
        {
            for (var i = 0; i < ClientData.Players.Count; i++)
            {
                if (ClientData.Players[i].StakeMaking)
                {
                    ClientData.StakerIndex = i;
                }
            }

            if (ClientData.Stake == -1)
            {
                ClientData.Stake = ClientData.CurPriceRight;
            }

            PlanExecution(Tasks.PrintAuctPlayer, 10);
        }
        else
        {
            if (ClientData.OrderIndex > -1 && ClientData.Decision == DecisionType.NextPersonStakeMaking)
            {
                Logic.AddHistory("Rolling order index back");
                ClientData.OrderIndex--;
            }

            PlanExecution(Tasks.AskStake, 20);
        }
    }

    private void FreeTable(Message message, string[] args, Account host)
    {
        if (ClientData.Stage != GameStage.Before || args.Length <= 2)
        {
            return;
        }

        var personType = args[2];

        GamePersonAccount account;
        int index = -1;
        var isPlayer = personType == Constants.Player;

        if (isPlayer)
        {
            if (args.Length < 4)
            {
                return;
            }

            var indexStr = args[3];

            if (!int.TryParse(indexStr, out index) || index < 0 || index >= ClientData.Players.Count)
            {
                return;
            }

            account = ClientData.Players[index];
        }
        else
        {
            account = ClientData.ShowMan;
        }

        if (!account.IsConnected || !account.IsHuman)
        {
            return;
        }

        var newAccount = new Account { IsHuman = true, Name = Constants.FreePlace };

        ClientData.BeginUpdatePersons("FreeTable " + message.Text);

        try
        {
            if (isPlayer)
            {
                ClientData.Players[index] = new GamePlayerAccount(newAccount);
            }
            else
            {
                ClientData.ShowMan = new GamePersonAccount(newAccount);
            }

            account.Ready = false;

            ClientData.Viewers.Add(account);
        }
        finally
        {
            ClientData.EndUpdatePersons();
        }

        foreach (var item in ClientData.MainPersons)
        {
            if (item.Ready)
            {
                _gameActions.SendMessage($"{Messages.Ready}\n{item.Name}", message.Sender);
            }
        }

        _gameActions.SendMessageWithArgs(Messages.Config, MessageParams.Config_Free, args[2], args[3]);
        _gameActions.SpecialReplic($"{ClientData.HostName} {ResourceHelper.GetSexString(LO[nameof(R.Sex_Free)], host.IsMale)} {account.Name} {LO[nameof(R.FromTable)]}");

        OnPersonsChanged();
    }

    private void SetPerson(string[] args, Account host)
    {
        if (ClientData.Stage != GameStage.Before || args.Length <= 4)
        {
            return;
        }

        var personType = args[2];
        var replacer = args[4];

        // Кого заменяем
        GamePersonAccount account;
        int index = -1;

        var isPlayer = personType == Constants.Player;

        if (isPlayer)
        {
            var indexStr = args[3];

            if (!int.TryParse(indexStr, out index) || index < 0 || index >= ClientData.Players.Count)
            {
                return;
            }

            account = ClientData.Players[index];
        }
        else
        {
            account = ClientData.ShowMan;
        }

        var oldName = account.Name;
        GamePersonAccount newAccount;

        if (!account.IsHuman)
        {
            if (ClientData.AllPersons.ContainsKey(replacer))
            {
                _gameActions.SpecialReplic(string.Format(LO[nameof(R.PersonAlreadyExists)], replacer));
                return;
            }

            ClientData.BeginUpdatePersons($"SetComputerPerson {account.Name} {account.IsConnected} {replacer} {index}");

            try
            {
                newAccount = isPlayer
                    ? ReplaceComputerPlayer(index, account.Name, replacer)
                    : ReplaceComputerShowman(account.Name, replacer);
            }
            finally
            {
                ClientData.EndUpdatePersons();
            }

            if (newAccount == null)
            {
                return;
            }
        }
        else
        {
            SetHumanPerson(isPlayer, account, replacer, index);
            newAccount = account;
        }

        _gameActions.SendMessageWithArgs(Messages.Config, MessageParams.Config_Set, args[2], args[3], args[4], account.IsMale ? '+' : '-');
        _gameActions.SpecialReplic($"{ClientData.HostName} {ResourceHelper.GetSexString(LO[nameof(R.Sex_Replaced)], host.IsMale)} {oldName} {LO[nameof(R.To)]} {replacer}");

        InformAvatar(newAccount);
        OnPersonsChanged();
    }

    internal GamePersonAccount? ReplaceComputerShowman(string oldName, string replacer)
    {
        for (var j = 0; j < _defaultShowmans.Length; j++)
        {
            if (_defaultShowmans[j].Name == replacer)
            {
                _client.Node.DeleteClient(oldName);

                return CreateNewComputerShowman(_defaultShowmans[j]);
            }
        }

        _client.Node.OnError(new Exception($"Default showman with name {replacer} not found"), true);
        return null;
    }

    internal GamePlayerAccount? ReplaceComputerPlayer(int index, string oldName, string replacer)
    {
        for (var j = 0; j < _defaultPlayers.Length; j++)
        {
            if (_defaultPlayers[j].Name == replacer)
            {
                _client.Node.DeleteClient(oldName);

                return CreateNewComputerPlayer(index, _defaultPlayers[j]);
            }
        }

        _client.Node.OnError(new Exception($"Default player with name {replacer} not found"), true);
        return null;
    }

    internal void SetHumanPerson(bool isPlayer, GamePersonAccount account, string replacer, int index)
    {
        int otherIndex = -1;
        // На кого заменяем
        ViewerAccount otherAccount = null;

        ClientData.BeginUpdatePersons($"SetHumanPerson {account.Name} {account.IsConnected} {replacer} {index}");

        try
        {
            if (ClientData.ShowMan.Name == replacer && ClientData.ShowMan.IsHuman)
            {
                otherAccount = ClientData.ShowMan;

                ClientData.ShowMan = new GamePersonAccount(account)
                {
                    Ready = account.Ready,
                    IsConnected = account.IsConnected
                };
            }
            else
            {
                for (var i = 0; i < ClientData.Players.Count; i++)
                {
                    if (ClientData.Players[i].Name == replacer && ClientData.Players[i].IsHuman)
                    {
                        otherAccount = ClientData.Players[i];

                        ClientData.Players[i] = new GamePlayerAccount(account)
                        {
                            Ready = account.Ready,
                            IsConnected = account.IsConnected
                        };

                        otherIndex = i;
                        break;
                    }
                }

                if (otherIndex == -1)
                {
                    for (var i = 0; i < ClientData.Viewers.Count; i++)
                    {
                        if (ClientData.Viewers[i].Name == replacer) // always IsHuman
                        {
                            otherAccount = ClientData.Viewers[i];
                            otherIndex = i;

                            if (account.IsConnected)
                            {
                                ClientData.Viewers[i] = new ViewerAccount(account) { IsConnected = true };
                            }
                            else
                            {
                                ClientData.Viewers.RemoveAt(i);
                            }

                            break;
                        }
                    }
                }

                if (otherIndex == -1)
                {
                    return;
                }
            }

            // Живой персонаж меняется на другого живого
            var otherPerson = otherAccount as GamePersonAccount;

            if (isPlayer)
            {
                ClientData.Players[index] = new GamePlayerAccount(otherAccount) { IsConnected = otherAccount.IsConnected };

                if (otherPerson != null)
                {
                    ClientData.Players[index].Ready = otherPerson.Ready;
                }
            }
            else
            {
                ClientData.ShowMan = new GamePersonAccount(otherAccount) { IsConnected = otherAccount.IsConnected };

                if (otherPerson != null)
                {
                    ClientData.ShowMan.Ready = otherPerson.Ready;
                }
            }

            InformAvatar(otherAccount);
        }
        finally
        {
            ClientData.EndUpdatePersons();
        }
    }

    internal void ChangePersonType(string personType, string indexStr, ViewerAccount? responsePerson)
    {
        GamePersonAccount account;
        int index = -1;

        var isPlayer = personType == Constants.Player;

        if (isPlayer)
        {
            if (!int.TryParse(indexStr, out index) || index < 0 || index >= ClientData.Players.Count)
            {
                return;
            }

            account = ClientData.Players[index];
        }
        else
        {
            account = ClientData.ShowMan;
        }

        if (account == null)
        {
            ClientData.BackLink.LogWarning("ChangePersonType: account == null");
            return;
        }

        var oldName = account.Name;

        var newType = !account.IsHuman;
        string newName = "";
        bool newIsMale = true;

        Account? newAcc = null;

        ClientData.BeginUpdatePersons($"ChangePersonType {personType} {indexStr}");

        try
        {
            if (account.IsConnected && account.IsHuman)
            {
                ClientData.Viewers.Add(account);
            }

            if (!account.IsHuman)
            {
                if (!_client.Node.DeleteClient(account.Name))
                {
                    _client.Node.OnError(new Exception($"Cannot delete client {account.Name}"), true);
                }
                else if (_client.Node.Contains(account.Name))
                {
                    _client.Node.OnError(new Exception($"Client {account.Name} was deleted but is still present on the server!"), true);
                }

                account.IsHuman = true;
                newName = account.Name = Constants.FreePlace;
                account.Picture = "";
                account.Ready = false;
                account.IsConnected = false;
            }
            else if (isPlayer)
            {
                if (_defaultPlayers == null)
                {
                    return;
                }

                var visited = new List<int>();

                for (var i = 0; i < ClientData.Players.Count; i++)
                {
                    if (i != index && ClientData.Players[i].IsConnected)
                    {
                        for (var j = 0; j < _defaultPlayers.Length; j++)
                        {
                            if (_defaultPlayers[j].Name == ClientData.Players[i].Name)
                            {
                                visited.Add(j);
                                break;
                            }
                        }
                    }
                }

                var rand = Random.Shared.Next(_defaultPlayers.Length - visited.Count - 1);

                while (visited.Contains(rand))
                {
                    rand++;
                }

                var compPlayer = _defaultPlayers[rand];
                newAcc = CreateNewComputerPlayer(index, compPlayer);
                newName = newAcc.Name;
                newIsMale = newAcc.IsMale;
            }
            else
            {
                var showman = new ComputerAccount(_defaultShowmans[0]);
                var name = showman.Name;
                var nameIndex = 0;

                while (nameIndex < Constants.MaxPlayers && ClientData.AllPersons.ContainsKey(name))
                {
                    name = $"{showman.Name} {nameIndex++}";
                }

                showman.Name = name;

                newAcc = CreateNewComputerShowman(showman);
                newName = newAcc.Name;
                newIsMale = newAcc.IsMale;
            }
        }
        finally
        {
            ClientData.EndUpdatePersons();
        }

        foreach (var item in ClientData.MainPersons)
        {
            if (item.Ready)
            {
                _gameActions.SendMessage($"{Messages.Ready}\n{item.Name}");
            }
        }

        _gameActions.SendMessageWithArgs(Messages.Config, MessageParams.Config_ChangeType, personType, index, newType ? '+' : '-', newName, newIsMale ? '+' : '-');

        if (responsePerson != null)
        {
            var newTypeString = newType ? LO[nameof(R.Human)] : LO[nameof(R.Computer)];
            _gameActions.SpecialReplic($"{ClientData.HostName} {ResourceHelper.GetSexString(LO[nameof(R.Sex_Changed)], responsePerson.IsMale)} {LO[nameof(R.PersonType)]} {oldName} {LO[nameof(R.To)]} \"{newTypeString}\"");
        }

        if (newAcc != null)
        {
            InformAvatar(newAcc);
        }

        OnPersonsChanged();
    }

    private GamePlayerAccount CreateNewComputerPlayer(int index, ComputerAccount account)
    {
        var newAccount = new GamePlayerAccount
        {
            IsHuman = false,
            Name = account.Name,
            IsMale = account.IsMale,
            Picture = account.Picture,
            IsConnected = true
        };

        ClientData.Players[index] = newAccount;

        var playerClient = Network.Clients.Client.Create(newAccount.Name, _client.Node);
        _ = new Player(playerClient, account, false, LO, new ViewerData(ClientData.BackLink));
        Inform(newAccount.Name);

        return newAccount;
    }

    private GamePersonAccount CreateNewComputerShowman(ComputerAccount account)
    {
        if (ClientData.BackLink == null)
        {
            throw new InvalidOperationException($"{nameof(CreateNewComputerShowman)}: this.ClientData.BackLink == null");
        }

        var newAccount = new GamePersonAccount
        {
            IsHuman = false,
            Name = account.Name,
            IsMale = account.IsMale,
            Picture = account.Picture,
            IsConnected = true
        };

        ClientData.ShowMan = newAccount;

        var showmanClient = Network.Clients.Client.Create(newAccount.Name, _client.Node);
        var showman = new Showman(showmanClient, account, false, LO, new ViewerData(ClientData.BackLink));

        Inform(newAccount.Name);

        return newAccount;
    }

    internal void StartGame()
    {
        ClientData.Stage = GameStage.Begin;
        ClientData.GameResultInfo.StartTime = DateTimeOffset.UtcNow;

        _logic.OnStageChanged(GameStages.Started, LO[nameof(R.GameBeginning)]);
        _gameActions.InformStage();

        ClientData.IsOral = ClientData.Settings.AppSettings.Oral && ClientData.ShowMan.IsHuman;

        _logic.ScheduleExecution(Tasks.StartGame, 1, 1);
    }

    private async Task<(bool? Result, bool Found)> CheckAccountAsync(
        Message message,
        string role,
        string name,
        string sex,
        int index,
        ViewerAccount account)
    {
        if (account.IsConnected)
        {
            return (null, false);
        }

        if (account.Name == name || account.Name == Constants.FreePlace)
        {
            var connectionFound = await _client.Node.ConnectionsLock.WithLockAsync(() =>
            {
                var connection = Master.Connections.Where(conn => conn.Id == message.Sender[1..]).FirstOrDefault();
                
                if (connection == null)
                {
                    return false;
                }

                lock (connection.ClientsSync)
                {
                    connection.Clients.Add(name);
                }

                connection.IsAuthenticated = true;
                connection.UserName = name;

                return true;
            });

            if (!connectionFound)
            {
                return (false, true);
            }

            ClientData.BeginUpdatePersons($"Connected {name} as {role} as {index}");

            try
            {
                var append = role == "viewer" && account.Name == Constants.FreePlace;
                account.Name = name;
                account.IsMale = sex == "m";
                account.Picture = "";
                account.IsConnected = true;

                if (append)
                {
                    ClientData.Viewers.Add(new ViewerAccount(account) { IsConnected = account.IsConnected });
                }
            }
            finally
            {
                ClientData.EndUpdatePersons();
            }

            _gameActions.SpecialReplic($"{LO[account.IsMale ? nameof(R.Connected_Male) : nameof(R.Connected_Female)]} {name}");

            _gameActions.SendMessage(Messages.Accepted, name);
            _gameActions.SendMessageWithArgs(Messages.Connected, role, index, name, sex, "");

            if (ClientData.HostName == null && !ClientData.Settings.IsAutomatic)
            {
                UpdateHostName(name);
            }

            OnPersonsChanged();
        }

        return (true, true);
    }

    private bool? CheckAccountNew(
        string role,
        string name,
        string sex,
        ref bool found,
        int index,
        ViewerAccount account,
        Action connectionAuthenticator)
    {
        if (account.IsConnected)
        {
            return account.Name == name ? false : (bool?)null;
        }

        found = true;

        ClientData.BeginUpdatePersons($"Connected {name} as {role} as {index}");

        try
        {
            var append = role == "viewer" && account.Name == Constants.FreePlace;

            account.Name = name;
            account.IsMale = sex == "m";
            account.Picture = "";
            account.IsConnected = true;

            if (append)
            {
                ClientData.Viewers.Add(new ViewerAccount(account) { IsConnected = account.IsConnected });
            }
        }
        finally
        {
            ClientData.EndUpdatePersons();
        }

        _gameActions.SpecialReplic($"{LO[account.IsMale ? nameof(R.Connected_Male) : nameof(R.Connected_Female)]} {name}");
        _gameActions.SendMessageWithArgs(Messages.Connected, role, index, name, sex, "");

        if (ClientData.HostName == null && !ClientData.Settings.IsAutomatic)
        {
            UpdateHostName(name);
        }

        connectionAuthenticator();

        OnPersonsChanged();

        return true;
    }

    private void OnPersonsChanged(bool joined = true, bool withError = false) => PersonsChanged?.Invoke(this, joined, withError);

    private void InformAvatar(Account account)
    {
        foreach (var personName in ClientData.AllPersons.Keys)
        {
            if (account.Name != personName && personName != NetworkConstants.GameName)
            {
                InformAvatar(account, personName);
            }
        }
    }

    private void InformAvatar(Account account, string receiver)
    {
        if (string.IsNullOrEmpty(account.Picture))
        {
            return;
        }

        var link = CreateUri(account.Name, account.Picture, receiver);

        if (link != null)
        {
            _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.Picture, account.Name, link), receiver);
        }
    }

    private string? CreateUri(string personName, string avatarUri, string receiver)
    {
        var local = _client.Node.Contains(receiver);

        if (!Uri.TryCreate(avatarUri, UriKind.RelativeOrAbsolute, out var uri))
        {
            return null;
        }

        if (!uri.IsAbsoluteUri || uri.Scheme == "file" && !CoreManager.Instance.FileExists(avatarUri))
        {
            return null;
        }

        var remote = !local && uri.Scheme == "file";
        var isUri = avatarUri.StartsWith("URI: ");

        if (isUri || remote)
        {
            string path;

            if (isUri)
            {
                path = avatarUri[5..];
            }
            else
            {
                if (!ClientData.BackLink.AreCustomAvatarsSupported)
                {
                    return null;
                }

                var complexName = $"{(personName != null ? personName + "_" : "")}{Path.GetFileName(avatarUri)}";

                if (!_avatarHelper.FileExists(complexName))
                {
                    _avatarHelper.AddFile(avatarUri, complexName);
                }

                path = _fileShare.CreateResourceUri(ResourceKind.Avatar, new Uri(complexName, UriKind.Relative)).ToString();
            }

            return local ? path : path.Replace("http://localhost", "http://" + Constants.GameHost);
        }
        else
        {
            return avatarUri;
        }
    }

    /// <summary>
    /// Начать игру даже при отсутствии участников (заполнив пустые слоты ботами)
    /// </summary>
    internal void AutoGame()
    {
        // Заполняем пустые слоты ботами
        for (var i = 0; i < ClientData.Players.Count; i++)
        {
            if (!ClientData.Players[i].IsConnected)
            {
                ChangePersonType(Constants.Player, i.ToString(), null);
            }
        }

        StartGame();
    }
}
