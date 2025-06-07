﻿using Notions;
using SICore.Clients;
using SICore.Contracts;
using SICore.Extensions;
using SICore.Models;
using SICore.Network;
using SICore.Network.Clients;
using SICore.Network.Contracts;
using SICore.PlatformSpecific;
using SICore.Results;
using SICore.Services;
using SICore.Special;
using SICore.Utils;
using SIData;
using SIPackages;
using SIPackages.Core;
using System.Text;
using R = SICore.Properties.Resources;

namespace SICore;

/// <summary>
/// Defines a game actor. Responds to all game-related messages.
/// </summary>
public sealed class Game : Actor
{
    private const string VideoAvatarUri = "https://vdo.ninja/";

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

    private readonly GameLogic _logic;

    public GameLogic Logic => _logic;

    private ILocalizer LO { get; }

    public GameData ClientData { get; }

    public Game(
        Client client,
        ILocalizer localizer,
        GameData gameData,
        GameActions gameActions,
        GameLogic gameLogic,
        ComputerAccount[] defaultPlayers,
        ComputerAccount[] defaultShowmans,
        IFileShare fileShare,
        IAvatarHelper avatarHelper)
        : base(client)
    {
        _gameActions = gameActions;
        _logic = gameLogic;
        LO = localizer;
        ClientData = gameData;

        _logic.AutoGame += AutoGame;

        _defaultPlayers = defaultPlayers;
        _defaultShowmans = defaultShowmans;

        _fileShare = fileShare;
        _avatarHelper = avatarHelper;

        Master.Unbanned += Master_Unbanned;
    }

    private void Master_Unbanned(string clientId) => _gameActions.SendMessageWithArgs(Messages.Unbanned, clientId);

    protected override void Dispose(bool disposing)
    {
        // Logic must be disposed before TaskLock
        Logic.Dispose();
        ClientData.TaskLock.Dispose();
        ClientData.TableInformStageLock.Dispose();

        base.Dispose(disposing);
    }

    /// <summary>
    /// Starts the game engine.
    /// </summary>
    public void Run()
    {
        Client.CurrentNode.SerializationError += CurrentServer_SerializationError;

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

    private void CurrentServer_SerializationError(Message message, Exception exc) => _client.Node.OnError(exc, true);

    /// <summary>
    /// Sends all current game data to the person.
    /// </summary>
    /// <param name="person">Receiver name.</param>
    private void Inform(string person = NetworkConstants.Everybody)
    {
        InformSettings(person);
        InformGameMetadata(person);
        InformPersons(person);
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

    private void OnSetOptions(Message message, string[] mparams)
    {
        if (message.Sender != ClientData.HostName)
        {
            return;
        }

        var appSettings = ClientData.Settings.AppSettings;
        var msg = new MessageBuilder(Messages.Options2, ClientData.HostName);
        var changed = false;

        for (var i = 1; i + 1 < mparams.Length; i += 2)
        {
            var optionName = mparams[i];
            var optionValue = mparams[i + 1];

            switch (optionName)
            {
                case nameof(AppSettingsCore.Oral):
                    if (bool.TryParse(optionValue, out var oral) && oral != appSettings.Oral)
                    {
                        appSettings.Oral = oral;
                        msg.Add(optionName).Add(optionValue);
                        changed = true;
                    }

                    break;

                case nameof(AppSettingsCore.Managed):
                    if (bool.TryParse(optionValue, out var managed) && managed != appSettings.Managed)
                    {
                        appSettings.Managed = managed;
                        msg.Add(optionName).Add(optionValue);
                        changed = true;
                    }

                    break;

                case nameof(AppSettingsCore.DisplayAnswerOptionsLabels):
                    if (bool.TryParse(optionValue, out var displayAnswerOptionsLabels) && displayAnswerOptionsLabels != appSettings.DisplayAnswerOptionsLabels)
                    {
                        appSettings.DisplayAnswerOptionsLabels = displayAnswerOptionsLabels;
                        msg.Add(optionName).Add(optionValue);
                        changed = true;
                    }

                    break;

                case nameof(AppSettingsCore.FalseStart):
                    if (bool.TryParse(optionValue, out var falseStart) && falseStart != appSettings.FalseStart)
                    {
                        appSettings.FalseStart = falseStart;
                        msg.Add(optionName).Add(optionValue);
                        changed = true;
                    }

                    break;

                case nameof(AppSettingsCore.ReadingSpeed):
                    if (int.TryParse(optionValue, out var readingSpeed) && readingSpeed != appSettings.ReadingSpeed)
                    {
                        appSettings.ReadingSpeed = readingSpeed;
                        msg.Add(optionName).Add(optionValue);
                        changed = true;
                    }
                    
                    break;

                case nameof(AppSettingsCore.PartialText):
                    if (bool.TryParse(optionValue, out var partialText) && partialText != appSettings.PartialText)
                    {
                        appSettings.PartialText = partialText;
                        msg.Add(optionName).Add(optionValue);
                        changed = true;
                    }

                    break;

                case nameof(AppSettingsCore.PartialImages):
                    if (bool.TryParse(optionValue, out var partialImages) && partialImages != appSettings.PartialImages)
                    {
                        appSettings.PartialImages = partialImages;
                        msg.Add(optionName).Add(optionValue);
                        changed = true;
                    }

                    break;

                case nameof(AppSettingsCore.TimeSettings.PartialImageTime):
                    if (int.TryParse(optionValue, out var value) && value != appSettings.TimeSettings.PartialImageTime)
                    {
                        appSettings.TimeSettings.PartialImageTime = value;
                        msg.Add(optionName).Add(optionValue);
                        changed = true;
                    }

                    break;

                case nameof(AppSettingsCore.UseApellations):
                    if (bool.TryParse(optionValue, out var useApellations) && useApellations != appSettings.UseApellations)
                    {
                        appSettings.UseApellations = useApellations;
                        msg.Add(optionName).Add(optionValue);
                        changed = true;
                    }
                    
                    break;

                default:
                    break;
            }
        }

        if (changed)
        {
            _gameActions.SendMessage(msg.Build());
        }
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
        _gameActions.SendMessageToWithArgs(person, Messages.Timer, 1, MessageParams.Timer_MaxTime, maxPressingTime);

        _gameActions.SendMessageToWithArgs(person, Messages.SetJoinMode, ClientData.JoinMode);

        _gameActions.SendMessageToWithArgs(
            person,
            Messages.Options,
            nameof(appSettings.Oral), appSettings.Oral,
            nameof(appSettings.Managed), appSettings.Managed,
            nameof(appSettings.DisplayAnswerOptionsLabels), appSettings.DisplayAnswerOptionsLabels,
            nameof(appSettings.FalseStart), appSettings.FalseStart,
            nameof(appSettings.ReadingSpeed), appSettings.Managed ? 0 : appSettings.ReadingSpeed,
            nameof(appSettings.PartialText), appSettings.PartialText,
            nameof(appSettings.PartialImages), appSettings.PartialImages,
            nameof(appSettings.TimeSettings.PartialImageTime), appSettings.TimeSettings.PartialImageTime,
            nameof(appSettings.UseApellations), appSettings.UseApellations,
            nameof(appSettings.TimeSettings.TimeForBlockingButton), appSettings.TimeSettings.TimeForBlockingButton);

        _gameActions.SendMessageToWithArgs(
            person,
            Messages.Options2,
            "",
            nameof(appSettings.Oral), appSettings.Oral,
            nameof(appSettings.Managed), appSettings.Managed,
            nameof(appSettings.DisplayAnswerOptionsLabels), appSettings.DisplayAnswerOptionsLabels,
            nameof(appSettings.FalseStart), appSettings.FalseStart,
            nameof(appSettings.ReadingSpeed), appSettings.Managed ? 0 : appSettings.ReadingSpeed,
            nameof(appSettings.PartialText), appSettings.PartialText,
            nameof(appSettings.PartialImages), appSettings.PartialImages,
            nameof(appSettings.TimeSettings.PartialImageTime), appSettings.TimeSettings.PartialImageTime,
            nameof(appSettings.UseApellations), appSettings.UseApellations,
            nameof(appSettings.TimeSettings.TimeForBlockingButton), appSettings.TimeSettings.TimeForBlockingButton);
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

    private void InformPersons(string person)
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
                ClientData.Host.LogWarning($"Viewer {viewer.Name} not connected\n" + ClientData.PersonsUpdateHistory);
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
        string? password,
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
                    case Messages.GameInfo: // TODO: will be deprecated after switching to SIGame 8
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

                    case Messages.Connect: // TODO: will be deprecated after switching to SIGame 8
                        await OnConnectAsync(message, args);
                        break;

                    case SystemMessages.Disconnect: // TODO: will be deprecated after switching to SIGame 8
                        OnDisconnect(args);
                        break;

                    case Messages.Info:
                        OnInfo(message.Sender);
                        break;

                    case Messages.Config:
                        OnConfig(message, args);
                        break;

                    case Messages.SetOptions:
                        OnSetOptions(message, args);
                        break;

                    case Messages.First:
                        OnFirst(message, args);
                        break;

                    case Messages.SetChooser:
                        OnSetChooser(message, args);
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

                    case Messages.Pin:
                        if (message.Sender == ClientData.HostName)
                        {
                            OnPin(message.Sender);
                        }
                        break;

                    case Messages.Avatar:
                        OnAvatar(message, args);
                        break;

                    case Messages.Moveable:
                        OnMoveable(message);
                        break;

                    case Messages.Choice:
                        OnChoice(message, args);
                        break;

                    case Messages.Toggle:
                        OnToggle(message, args);
                        break;

                    case Messages.I:
                        OnI(message.Sender, args.Length > 1 && int.TryParse(args[1], out var pressDuration) ? pressDuration : -1);
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
                        OnAtom(args);
                        break;

                    case Messages.MediaLoaded:
                        OnMediaLoaded(message);
                        break;

                    case Messages.MediaPreloaded:
                        OnMediaPreloaded(message);
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
                        if (ClientData.IsWaiting
                            && ClientData.Decision == DecisionType.QuestionAnswererSelection
                            && (ClientData.Chooser != null && message.Sender == ClientData.Chooser.Name
                                || ClientData.IsOralNow && message.Sender == ClientData.ShowMan.Name))
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
                            catch (Exception ex)
                            {
                                ClientData.Host.SendError(ex, true);
                            }

                            #endregion
                        }
                        break;

                    case Messages.SelectPlayer:
                        {
                            if (ClientData.IsWaiting
                                && ClientData.DecisionMakers.Contains(message.Sender)
                                && args.Length > 1
                                && int.TryParse(args[1], out var playerIndex)
                                && playerIndex > -1
                                && playerIndex < ClientData.Players.Count
                                && ClientData.Players[playerIndex].Flag)
                            {
                                OnSelectedPlayer(playerIndex, message.Sender);
                            }
                        }
                        break;

                    case Messages.CatCost:
                        OnCatCost(message, args);
                        break;

                    case Messages.Stake:
                        OnStake(message, args);
                        break;

                    case Messages.SetStake:
                        OnSetStake(message, args);
                        break;

                    case Messages.NextDelete:
                        OnNextDelete(message, args);
                        break;

                    case Messages.Delete:
                        OnDelete(message, args);
                        break;

                    case Messages.FinalStake:
                        if (ClientData.IsWaiting && ClientData.Decision == DecisionType.HiddenStakeMaking)
                        {
                            #region FinalStake

                            for (var i = 0; i < ClientData.Players.Count; i++)
                            {
                                var player = ClientData.Players[i];

                                if (ClientData.QuestionPlayState.AnswererIndicies.Contains(i) && player.PersonalStake == -1 && message.Sender == player.Name)
                                {
                                    if (int.TryParse(args[1], out int finalStake) && finalStake >= 1 && finalStake <= player.Sum)
                                    {
                                        player.PersonalStake = finalStake;
                                        ClientData.HiddenStakerCount--;

                                        _gameActions.SendMessageWithArgs(Messages.PersonFinalStake, i);
                                    }

                                    break;
                                }
                            }

                            if (ClientData.HiddenStakerCount == 0)
                            {
                                _logic.Stop(StopReason.Decision);
                            }

                            #endregion
                        }
                        break;

                    case Messages.Apellate:
                        OnAppellation(message, args);
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
                        if (!ClientData.CanMarkQuestion || args.Length < 3)
                        {
                            break;
                        }

                        ClientData.GameResultInfo.ComplainedQuestions.Add(new QuestionReport
                        {
                            ThemeName = ClientData.Theme.Name,
                            QuestionText = ClientData.Question?.GetText(),
                            ReportText = args[2]
                        });
                        break;

                    case Messages.Validate:
                        OnValidate(message, args);
                        break;
                }
            }
            catch (Exception exc)
            {
                _client.Node.OnError(new Exception(message.Text, exc), true);
            }
        }, 5000);

    private void OnValidate(Message message, string[] args)
    {
        if (message.Sender != ClientData.ShowMan.Name || args.Length <= 2)
        {
            return;
        }

        var answer = args[1];
        var validationStatus = args[2] == "+";
        var validationFactor = args.Length > 3 && double.TryParse(args[3], out var factor) && factor >= 0.0 ? factor : 1.0;

        if (!ClientData.QuestionPlayState.Validations.TryGetValue(answer, out var validation) || validation.HasValue)
        {
            return;
        }

        ClientData.QuestionPlayState.Validations[answer] = (validationStatus, validationFactor);
    }

    private void OnPin(string hostName)
    {
        var pin = Logic.PinHelper?.GeneratePin() ?? 0;
        _gameActions.SendMessageToWithArgs(hostName, Messages.Pin, pin);
    }

    private void OnSelectedPlayer(int playerIndex, string messageSender)
    {
        switch (ClientData.Decision)
        {
            case DecisionType.StarterChoosing:
                ClientData.ChooserIndex = playerIndex;
                _logic.Stop(StopReason.Decision);
                break;

            case DecisionType.NextPersonStakeMaking:
                ClientData.Order[ClientData.OrderIndex] = playerIndex;
                Logic.CheckOrder(ClientData.OrderIndex);
                _logic.Stop(StopReason.Decision);
                break;

            case DecisionType.NextPersonFinalThemeDeleting:
                ClientData.ThemeDeleters?.Current.SetIndex(playerIndex);
                _logic.Stop(StopReason.Decision);
                break;

            case DecisionType.QuestionAnswererSelection:
                ClientData.AnswererIndex = playerIndex;
                ClientData.QuestionPlayState.SetSingleAnswerer(playerIndex);

                if (ClientData.IsOralNow)
                {
                    _gameActions.SendMessage(
                        Messages.Cancel,
                        messageSender == ClientData.ShowMan.Name ? ClientData.Chooser.Name : ClientData.ShowMan.Name);
                }

                _logic.Stop(StopReason.Decision);
                break;

            default:
                return;
        }
    }

    private void OnChoice(Message message, string[] args)
    {
        if (!ClientData.IsWaiting
            || ClientData.Decision != DecisionType.QuestionSelection
            || args.Length != 3
            || ClientData.Chooser == null
            || message.Sender != ClientData.Chooser.Name
                && (!ClientData.IsOralNow || message.Sender != ClientData.ShowMan.Name))
        {
            return;
        }

        if (!int.TryParse(args[1], out var themeIndex) || !int.TryParse(args[2], out var questionIndex))
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

        if (!ClientData.TInfo.RoundInfo[themeIndex].Questions[questionIndex].IsActive())
        {
            return;
        }

        ClientData.ThemeIndex = themeIndex;
        ClientData.QuestionIndex = questionIndex;

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

    private void OnFirst(Message message, string[] args)
    {
        if (!ClientData.IsWaiting
            || ClientData.Decision != DecisionType.StarterChoosing
            || message.Sender != ClientData.ShowMan.Name
            || args.Length <= 1)
        {
            return;
        }

        if (!int.TryParse(args[1], out int playerIndex) || playerIndex <= -1 || playerIndex >= ClientData.Players.Count || !ClientData.Players[playerIndex].Flag)
        {
            return;
        }

        ClientData.ChooserIndex = playerIndex;
        _logic.Stop(StopReason.Decision);
    }

    private void OnSetChooser(Message message, string[] args)
    {
        if (message.Sender != ClientData.ShowMan.Name || args.Length <= 1)
        {
            return;
        }

        if (!int.TryParse(args[1], out int playerIndex) || playerIndex <= -1 || playerIndex >= ClientData.Players.Count)
        {
            return;
        }

        if (ClientData.ChooserIndex == playerIndex)
        {
            return;
        }

        var isChoosingNow = _logic.Runner.PendingTask == Tasks.WaitChoose;

        if (isChoosingNow)
        {
            _logic.StopWaiting();

            if (ClientData.IsOralNow)
            {
                _gameActions.SendMessage(Messages.Cancel, ClientData.ShowMan.Name);
            }

            if (ClientData.Chooser != null && Logic.CanPlayerAct())
            {
                _gameActions.SendMessage(Messages.Cancel, ClientData.Chooser.Name);
            }
        }

        ClientData.ChooserIndex = playerIndex;
        _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex);

        _gameActions.SpecialReplic(string.Format(LO[nameof(R.SetChooser)], ClientData.ShowMan.Name, ClientData.Chooser?.Name));

        if (isChoosingNow)
        {
            _logic.PlanExecution(Tasks.AskToChoose, 10);
        }
    }

    private void OnMoveable(Message message)
    {
        if (ClientData.AllPersons.TryGetValue(message.Sender, out var person))
        {
            person.IsMoveable = true;
        }
    }

    private void OnReport(Message message, string[] args)
    {
        if (ClientData.Decision != DecisionType.Reporting
            || !ClientData.Players.Any(player => player.Name == message.Sender)
            || ClientData.GameResultInfo.Reviews.ContainsKey(message.Sender))
        {
            return;
        }

        var review = new StringBuilder();

        for (var i = 2; i < args.Length; i++)
        {
            review.AppendLine(args[i]);
        }

        if (args[1] == MessageParams.Report_Log)
        {
            ClientData.Host.LogWarning("Player error: " + review);
            return;
        }

        ClientData.ReportsCount--;

        if (review.Length > 0)
        {
            ClientData.GameResultInfo.Reviews[message.Sender] = review.ToString();
        }

        if (ClientData.ReportsCount == 0)
        {
            _logic.RescheduleTask();
        }
    }

    private void OnMediaLoaded(Message message) => _gameActions.SendMessageToWithArgs(ClientData.ShowMan.Name, Messages.MediaLoaded, message.Sender);

    private void OnMediaPreloaded(Message message) => _gameActions.SendMessageToWithArgs(ClientData.ShowMan.Name, Messages.MediaPreloaded, message.Sender);

    private void OnToggle(Message message, string[] args)
    {
        if (ClientData.TableController == null)
        {
            return;
        }

        if (message.Sender != ClientData.ShowMan.Name || args.Length < 3)
        {
            return;
        }

        if (!int.TryParse(args[1], out var themeIndex) || !int.TryParse(args[2], out var questionIndex))
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
            if (!ClientData.TableController.RemoveQuestion(themeIndex, questionIndex))
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
        }
        else
        {
            if (!ClientData.TableController.RestoreQuestion(themeIndex, questionIndex))
            {
                return;
            }

            _gameActions.SpecialReplic(
                string.Format(
                    LO[nameof(R.QuestionRestored)],
                    message.Sender,
                    ClientData.TInfo.RoundInfo[themeIndex].Name,
                    question.Price));
        }

        // TODO: remove after all clients upgrade to 7.12.0
        ClientData.TableInformStageLock.WithLock(
            () =>
            {
                if ((ClientData.InformStages & InformStages.Table) > 0)
                {
                    _gameActions.InformRoundThemesNames(playMode: ThemesPlayMode.None);
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

        var clientName = args[1];

        if (!ClientData.AllPersons.TryGetValue(clientName, out var person))
        {
            return;
        }

        if (person.Name == message.Sender)
        {
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.Replic, ReplicCodes.Special.ToString(), GameError.CannotKickYouself);
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.UserError, ErrorCode.CannotKickYourSelf);
            return;
        }

        if (!person.IsHuman)
        {
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.Replic, ReplicCodes.Special.ToString(), GameError.CannotKickBots);
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.UserError, ErrorCode.CannotKickBots);
            return;
        }

        _gameActions.SendMessageToWithArgs(message.Sender, Messages.YouAreKicked);
        OnDisconnectRequested(clientName);

        var clientId = Master.Kick(clientName);

        if (clientId.Length > 0)
        {
            _gameActions.SendMessageWithArgs(Messages.Banned, clientId, clientName);
        }

        _gameActions.SpecialReplic(string.Format(LO[nameof(R.Kicked)], message.Sender, clientName));
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
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.Replic, ReplicCodes.Special.ToString(), GameError.CannotBanYourself);
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.UserError, ErrorCode.CannotKickYourSelf);
            return;
        }

        if (!person.IsHuman)
        {
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.Replic, ReplicCodes.Special.ToString(), GameError.CannotBanBots);
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.UserError, ErrorCode.CannotKickBots);
            return;
        }

        _gameActions.SendMessageToWithArgs(message.Sender, Messages.YouAreKicked);
        OnDisconnectRequested(clientName);

        var clientId = Master.Kick(clientName, true);

        if (clientId.Length > 0)
        {
            _gameActions.SendMessageWithArgs(Messages.Banned, clientId, person);
        }

        _gameActions.SpecialReplic(string.Format(LO[nameof(R.Banned)], message.Sender, clientName));
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
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.Replic, ReplicCodes.Special.ToString(), GameError.CannotSetHostToYourself);
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.UserError, ErrorCode.CannotSetHostToYourself);
            return;
        }

        if (!person.IsHuman)
        {
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.Replic, ReplicCodes.Special.ToString(), GameError.CannotSetHostToBots);
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.UserError, ErrorCode.CannotSetHostToBots);
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
        if (args.Length < 2)
        {
            return;
        }

        var path = args[1];
        var person = ClientData.MainPersons.FirstOrDefault(item => message.Sender == item.Name);

        if (person == null)
        {
            return;
        }

        if (args.Length > 2)
        {
            if (!ClientData.Host.AreCustomAvatarsSupported)
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

            var uri = _fileShare.CreateResourceUri(ResourceKind.Avatar, new Uri(file, UriKind.Relative));

            person.Picture = $"URI: {uri}";
        }
        else
        {
            person.Picture = path;
        }

        InformAvatar(person);
    }

    private void OnAvatar(Message message, string[] args)
    {
        if (args.Length < 3)
        {
            return;
        }

        var contentType = args[1];
        var avatarUri = args[2];
        var person = ClientData.MainPersons.FirstOrDefault(item => message.Sender == item.Name);

        if (person == null)
        {
            return;
        }

        if (contentType == ContentTypes.Image)
        {
            person.Picture = avatarUri;
            _gameActions.SendMessageWithArgs(Messages.Avatar, person.Name, contentType, avatarUri);
            _gameActions.SendMessageWithArgs(Messages.Picture, person.Name, avatarUri); // for backward compatibility
        }
        else if (contentType == ContentTypes.Video && (avatarUri.Length == 0 || avatarUri.StartsWith(VideoAvatarUri)))
        {
            person.AvatarVideoUri = avatarUri;
            _gameActions.SendMessageWithArgs(Messages.Avatar, person.Name, contentType, avatarUri);
        }
    }

    private void OnSetStake(Message message, string[] args)
    {
        if (!ClientData.IsWaiting
            || !ClientData.DecisionMakers.Contains(message.Sender)
            || args.Length <= 1
            || !Enum.TryParse<StakeModes>(args[1], out var stakeMode)
            || (ClientData.StakeModes & stakeMode) <= 0)
        {
            return;
        }

        var stakerName = message.Sender == ClientData.ShowMan.Name ? ClientData.DecisionMakers.First() : message.Sender;

        if (!ClientData.StakeLimits.TryGetValue(stakerName, out var stakeLimit))
        {
            return;
        }

        var stakeSum = 0;

        if (stakeMode == StakeModes.Stake)
        {
            if (args.Length < 2 || !int.TryParse(args[2], out stakeSum))
            {
                return;
            }

            if (stakeSum < stakeLimit.Minimum
                || stakeSum > stakeLimit.Maximum
                || stakeLimit.Step != 0 && stakeSum != stakeLimit.Maximum && (stakeSum - stakeLimit.Minimum) % stakeLimit.Step != 0)
            {
                return;
            }
        }

        switch (ClientData.Decision)
        {
            case DecisionType.StakeMaking:
                ClientData.StakeType = FromStakeMode(stakeMode);
                ClientData.StakeSum = stakeSum;
                _logic.Stop(StopReason.Decision);
                break;

            case DecisionType.QuestionPriceSelection:
                ClientData.CurPriceRight = stakeSum;
                _logic.Stop(StopReason.Decision);
                break;

            case DecisionType.HiddenStakeMaking:
                for (var i = 0; i < ClientData.Players.Count; i++)
                {
                    var player = ClientData.Players[i];

                    if (stakerName == player.Name)
                    {
                        player.PersonalStake = stakeSum;
                        ClientData.HiddenStakerCount--;
                        ClientData.StakeLimits.Remove(stakerName);

                        _gameActions.SendMessageWithArgs(Messages.PersonFinalStake, i);
                        break;
                    }
                }

                if (ClientData.HiddenStakerCount == 0)
                {
                    _logic.Stop(StopReason.Decision);
                }

                break;
        }

        if (ClientData.IsOralNow)
        {
            if (message.Sender == ClientData.ShowMan.Name)
            {
                if (Logic.CanPlayerAct())
                {
                    _gameActions.SendMessage(Messages.Cancel, stakerName);
                }
            }
            else
            {
                _gameActions.SendMessage(Messages.Cancel, ClientData.ShowMan.Name);
            }
        }
    }

    private static StakeMode? FromStakeMode(StakeModes stakeMode) =>
        stakeMode switch
        {
            StakeModes.AllIn => StakeMode.AllIn,
            StakeModes.Pass => StakeMode.Pass,
            _ => StakeMode.Sum
        };

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
            var minimum = ClientData.Stake != -1 ? ClientData.Stake + ClientData.StakeStep : ClientData.CurPriceRight + ClientData.StakeStep;
            
            // TODO: optimize
            while (minimum % ClientData.StakeStep != 0)
            {
                minimum++;
            }

            if (!int.TryParse(args[2], out var stakeSum))
            {
                ClientData.StakeType = null;
                return;
            }

            if (stakeSum < minimum || stakeSum > ClientData.ActivePlayer.Sum || stakeSum % ClientData.StakeStep != 0)
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

    private void OnInfo(string person)
    {
        Inform(person);

        foreach (var item in ClientData.MainPersons)
        {
            if (item.Ready)
            {
                _gameActions.SendMessage($"{Messages.Ready}\n{item.Name}", person);
            }
        }

        _gameActions.InformSums(person);
        InformGameStage(person);
    }

    private void InformGameStage(string person)
    {
        var roundIndex = _logic.Engine.RoundIndex;
        _gameActions.InformStageInfo(person, roundIndex);

        if (ClientData.Stage == GameStage.Round)
        {
            _gameActions.InformRound(
                ClientData.Round?.Name ?? "",
                roundIndex,
                SIEngine.Rules.QuestionSelectionStrategyType.SelectByPlayer /* does not matter */,
                person); // deprecated
        }
        else
        {
            _gameActions.InformStage(person); // deprecated
        }

        if ((ClientData.InformStages & InformStages.RoundNames) > 0)
        {
            _gameActions.InformRoundsNames(person);
        }

        if ((ClientData.InformStages & InformStages.RoundContent) > 0)
        {
            _gameActions.InformRoundContent(person);
        }

        if ((ClientData.InformStages & InformStages.RoundThemesNames) > 0)
        {
            _gameActions.InformRoundThemesNames(person);
        }

        if ((ClientData.InformStages & InformStages.RoundThemesComments) > 0)
        {
            _gameActions.InformRoundThemesComments(person);
        }

        if ((ClientData.InformStages & InformStages.Table) > 0)
        {
            _gameActions.InformTable(person);
        }

        if ((ClientData.InformStages & InformStages.Theme) > 0)
        {
            _gameActions.InformTheme(person);
        }


        if (ClientData.Stage == GameStage.Before && ClientData.Settings.IsAutomatic)
        {
            var leftTimeBeforeStart = Constants.AutomaticGameStartDuration - (int)(DateTime.UtcNow - ClientData.TimerStartTime[2]).TotalSeconds * 10;

            if (leftTimeBeforeStart > 0)
            {
                _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.Timer, 2, MessageParams.Timer_Go, leftTimeBeforeStart, -2), person);
            }
        }
    }

    private void OnDelete(Message message, string[] args)
    {
        if (!ClientData.IsWaiting ||
            ClientData.Decision != DecisionType.ThemeDeleting ||
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

            if (ClientData.Settings.AppSettings.Managed && !_logic.Runner.IsRunning)
            {
                if (_logic.StopReason == StopReason.Pause || ClientData.TInfo.Pause)
                {
                    _logic.AddHistory("Managed game pause autoremoved");
                    _logic.OnPauseCore(false);
                    return;
                }

                _logic.AddHistory("Managed game move autostarted");

                ClientData.MoveDirection = MoveDirections.Next;
                _logic.Stop(StopReason.Move);
            }
        }

        OnPersonsChanged(false, withError);
    }

    [Obsolete]
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

                if (ClientData.HostName == name) // Host is connecting
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

    private void OnAppellation(Message message, string[] args)
    {
        ClientData.IsAppelationForRightAnswer = args.Length == 1 || args[1] == "+";
        ClientData.AppellationSource = message.Sender;

        if (!ClientData.AllowAppellation)
        {
            if (ClientData.AppellationOpened && !ClientData.PendingApellation)
            {
                // TODO: save appellation request and return to it after question finish
                // Merge AppellationOpened and AllowAppellation properties into triple-state property
                ClientData.PendingApellation = true;
                _gameActions.SpecialReplic($"{ClientData.AppellationSource} {LO[nameof(R.RequestedApellation)]}");
            }

            return;
        }

        _logic.ProcessApellationRequest();
    }

    [Obsolete]
    private void OnCatCost(Message message, string[] args)
    {
        if (!ClientData.IsWaiting ||
            ClientData.Decision != DecisionType.QuestionPriceSelection ||
            (ClientData.Answerer == null || message.Sender != ClientData.Answerer.Name) &&
            (!ClientData.IsOralNow || message.Sender != ClientData.ShowMan.Name))
        {
            return;
        }

        if (int.TryParse(args[1], out var sum)
            && sum >= ClientData.StakeRange.Minimum
            && sum <= ClientData.StakeRange.Maximum
            && (ClientData.StakeRange.Step == 0 || (sum - ClientData.StakeRange.Minimum) % ClientData.StakeRange.Step == 0))
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
        var oldSum = player.Sum;
        player.Sum = sum;

        var verbEnding = ClientData.ShowMan.IsMale ? "" : LO[nameof(R.FemaleEnding)];

        _gameActions.SpecialReplic(string.Format(LO[nameof(R.ScoreChanged)], ClientData.ShowMan.Name, player.Name, oldSum, sum, verbEnding));
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
            _logic.OnPauseCore(false);

            if (moveDirection == MoveDirections.Next)
            {
                return;
            }
        }

        _logic.AddHistory($"Move started: {moveDirection}");

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

        _logic.OnPauseCore(args[1] == "+");
    }

    private void OnAtom(string[] args)
    {
        Completion? completion;
        var completions = ClientData.QuestionPlayState.MediaContentCompletions;

        if (args.Length > 2)
        {
            var contentType = args[1];
            var contentValue = args[2];

            if (!completions.TryGetValue((contentType, contentValue), out completion))
            {
                return;
            }
        }
        else if (completions.Count == 0)
        {
            return;
        }
        else
        {
            completion = completions.Values.First();
        }

        completion.Current++;

        if (!ClientData.IsPlayingMedia || ClientData.TInfo.Pause)
        {
            return;
        }

        if (completion.Current == completion.Total)
        {
            ClientData.IsPlayingMedia = false;
            _logic.RescheduleTask();
        }
        else
        {
            // Sometimes someone drops out, and the process gets delayed by 120 seconds. This is unacceptable. We'll give 3 seconds
            _logic.RescheduleTask(30 + ClientData.Settings.AppSettings.TimeSettings.TimeForMediaDelay * 10);
        }
    }

    private void OnAnswerVersion(Message message, string[] args)
    {
        if (ClientData.Decision != DecisionType.Answering || args[1].Length == 0)
        {
            return;
        }

        if (Logic.HaveMultipleAnswerers())
        {
            for (var i = 0; i < ClientData.Players.Count; i++)
            {
                if (ClientData.Players[i].Name == message.Sender && ClientData.QuestionPlayState.AnswererIndicies.Contains(i))
                {
                    ClientData.Players[i].Answer = args[1];
                    return;
                }
            }

            return;
        }

        var answerer = ClientData.Answerer;
        
        if (!ClientData.IsWaiting || answerer == null || !answerer.IsHuman || answerer.Name != message.Sender)
        {
            return;
        }

        answerer.Answer = args[1];
    }

    private void OnAnswer(Message message, string[] args)
    {
        if (ClientData.Decision != DecisionType.Answering)
        {
            return;
        }

        if (Logic.HaveMultipleAnswerers())
        {
            ClientData.AnswererIndex = -1; // TODO: do not use AnswererIndex here - update player answer directly

            for (var i = 0; i < ClientData.Players.Count; i++)
            {
                if (ClientData.Players[i].Name == message.Sender
                    && ClientData.QuestionPlayState.AnswererIndicies.Contains(i)
                    && ClientData.Players[i].Flag)
                {
                    ClientData.AnswererIndex = i;
                    ClientData.Players[i].Flag = false;
                    _gameActions.SendMessageWithArgs(Messages.PersonFinalAnswer, i);
                    _gameActions.SendMessageWithArgs(Messages.PlayerState, PlayerState.HasAnswered, i);

                    if (ClientData.QuestionPlayState.AnswerOptions == null && ClientData.Players[i].IsHuman && args[1].Length > 0)
                    {
                        var answer = args[1];

                        if (ClientData.QuestionPlayState.Validations.ContainsKey(answer))
                        {
                            break;
                        }

                        ClientData.QuestionPlayState.Validations[answer] = null;

                        _gameActions.SendMessageToWithArgs(
                            ClientData.ShowMan.Name,
                            Messages.AskValidate,
                            i,
                            answer);
                    }

                    break;
                }
            }

            if (ClientData.AnswererIndex == -1)
            {
                return;
            }
        }
        else if (!ClientData.IsWaiting
            || ClientData.Answerer != null
                && ClientData.Answerer.Name != message.Sender
                && !(ClientData.IsOralNow && message.Sender == ClientData.ShowMan.Name))
        {
            return;
        }

        if (ClientData.Answerer == null)
        {
            return;
        }

        if (!ClientData.Answerer.IsHuman)
        {
            var isSure = args.Length > 2 && args[2] == "+";

            if (args[1] == MessageParams.Answer_Right)
            {
                if (ClientData.QuestionPlayState.AnswerOptions != null)
                {
                    var rightLabel = ClientData.Question.Right.FirstOrDefault();
                    ClientData.Answerer.Answer = rightLabel;
                }
                else
                {
                    ClientData.Answerer.Answer = (ClientData.Question.Right.FirstOrDefault() ?? "(...)").GrowFirstLetter();
                    ClientData.Answerer.AnswerIsWrong = false;
                }
            }
            else if (ClientData.QuestionPlayState.AnswerOptions != null)
            {
                var rightLabel = ClientData.Question.Right.FirstOrDefault();
                var wrongOptions = ClientData.QuestionPlayState.AnswerOptions.Where(o => o.Label != rightLabel && !ClientData.QuestionPlayState.UsedAnswerOptions.Contains(o.Label)).ToArray();
                var wrong = wrongOptions[Random.Shared.Next(wrongOptions.Length)];

                ClientData.Answerer.Answer = wrong.Label;
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

                var wrongIndex = Random.Shared.Next(wrongCount);

                if (!Logic.HaveMultipleAnswerers())
                {
                    ClientData.UsedWrongVersions.Add(restwrong[wrongIndex]);
                }

                ClientData.Answerer.Answer = restwrong[wrongIndex].GrowFirstLetter();
            }
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
        }

        ClientData.AnswerCount--;

        if (ClientData.AnswerCount == 0)
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
            ClientData.Answerer.AnswerValidationFactor = args.Length > 2 && double.TryParse(args[2], out var factor) && factor >= 0.0 ? factor : 1.0;
            ClientData.ShowmanDecision = true;

            if (ClientData.Answerer != null
                && ClientData.IsOralNow
                && (ClientData.QuestionPlayState.AnswerOptions == null || !ClientData.Settings.AppSettings.OralPlayersActions))
            {
                // Cancelling Oral Answer player mode
                _gameActions.SendMessage(Messages.Cancel, ClientData.Answerer.Name);
            }

            _logic.Stop(StopReason.Decision);
            return;
        }

        if (ClientData.Decision == DecisionType.Appellation)
        {
            for (var i = 0; i < ClientData.Players.Count; i++)
            {
                if (ClientData.Players[i].AppellationFlag && ClientData.Players[i].Name == message.Sender)
                {
                    if (args[1] == "+")
                    {
                        ClientData.AppellationPositiveVoteCount++;
                    }
                    else
                    {
                        ClientData.AppellationNegativeVoteCount++;
                    }

                    ClientData.Players[i].AppellationFlag = false;
                    ClientData.AppellationAwaitedVoteCount--;
                    _gameActions.SendMessageWithArgs(Messages.PersonApellated, i);
                    _gameActions.SendMessageWithArgs(Messages.PlayerState, PlayerState.HasAnswered, i);
                    break;
                }
            }

            if (ClientData.AppellationAwaitedVoteCount == 0)
            {
                _logic.Stop(StopReason.Decision);
            }

            var halfVoteCount = ClientData.AppellationTotalVoteCount / 2;

            if (ClientData.AppellationPositiveVoteCount > halfVoteCount || ClientData.AppellationNegativeVoteCount > halfVoteCount)
            {
                SendAppellationCancellationsToActivePlayers();
                _logic.Stop(StopReason.Decision);
            }
        }
    }

    private void SendAppellationCancellationsToActivePlayers()
    {
        foreach (var player in ClientData.Players)
        {
            if (player.AppellationFlag)
            {
                _gameActions.SendMessage(Messages.Cancel, player.Name);
            }
        }
    }

    private void OnPass(Message message)
    {
        if (!ClientData.IsQuestionAskPlaying) // TODO: this condition looks ugly and unnecessary
        {
            return;
        }

        var nextTask = Logic.Runner.PendingTask;

        if (nextTask == Tasks.AskStake
            || ClientData.Decision == DecisionType.StakeMaking
            || ClientData.Decision == DecisionType.NextPersonStakeMaking)
        {
            // Sending pass in stakes in advance
            if (ClientData.Decision == DecisionType.StakeMaking && ClientData.ActivePlayer?.Name == message.Sender)
            {
                return; // Currently making stake player can pass with the standard button
            }

            // Stake making pass
            for (var i = 0; i < ClientData.Players.Count; i++)
            {
                var player = ClientData.Players[i];

                if (player.Name == message.Sender)
                {
                    if (player.StakeMaking && i != ClientData.Stakes.StakerIndex) // Current stakes winner cannot pass
                    {
                        player.StakeMaking = false;
                        
                        ClientData.OrderHistory.Append("Player passed on stakes making: ")
                            .Append(i)
                            .Append(' ')
                            .Append(ClientData.ActivePlayer?.StakeMaking)
                            .AppendLine();
                        
                        // TODO: leave only one pass message
                        _gameActions.SendMessageWithArgs(Messages.Pass, i);
                        _gameActions.SendMessageWithArgs(Messages.PersonStake, i, 2);

                        if (ClientData.ActivePlayer != null
                            && ClientData.ActivePlayer.StakeMaking
                            && (nextTask == Tasks.AskStake || ClientData.Decision == DecisionType.NextPersonStakeMaking))
                        {
                            // We do not interrupt DecisionType.StakeMaking of the current player
                            Logic.TryDetectStakesWinner(); // returning despite of the call result
                        }
                    }

                    break;
                }
            }

            return;
        }

        var canPressChanged = false;

        // Player can pass while somebody is answering
        // so accepting pass and moving to answer are separate actions
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

        if (canPressChanged
            && ClientData.Players.All(p => !p.CanPress || !p.IsConnected)
            && (ClientData.Decision == DecisionType.None || ClientData.Decision == DecisionType.Pressing) // TODO: Can this state be described in a more clear way?
            && !ClientData.TInfo.Pause
            && !ClientData.QuestionPlayState.IsAnswer)
        {
            _logic.MoveToAnswer();
            _logic.ScheduleExecution(Tasks.WaitTry, 1, force: true);
        }
    }

    /// <summary>
    /// Handles player button press.
    /// </summary>
    /// <param name="playerName">Pressed player name.</param>
    /// <param name="pressDurationMs">Player reaction time.</param>
    private void OnI(string playerName, int pressDurationMs)
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

        var buttonPressMode = ClientData.Settings.AppSettings.ButtonPressMode;

        switch (buttonPressMode)
        {
            case ButtonPressMode.FirstWins:
                ProcessAnswerer_FirstWinsServer(answererIndex);
                break;

            case ButtonPressMode.RandomWithinInterval:
                ProcessAnswerer_RandomWithinInterval(answererIndex);
                break;

            case ButtonPressMode.FirstWinsClient:
                ProcessAnswerer_FirstWinsClient(answererIndex, pressDurationMs);
                break;

            default:
                break;
        }
    }

    private void ProcessAnswerer_FirstWinsServer(int answererIndex)
    {
        ClientData.PendingAnswererIndex = answererIndex;

        if (_logic.Stop(StopReason.Answer))
        {
            ClientData.Decision = DecisionType.None;
        }
    }

    private void ProcessAnswerer_RandomWithinInterval(int answererIndex)
    {
        if (!ClientData.PendingAnswererIndicies.Contains(answererIndex))
        {
            ClientData.PendingAnswererIndicies.Add(answererIndex);
        }

        if (!ClientData.IsDeferringAnswer)
        {            
            ClientData.WaitInterval = (int)(ClientData.Host.Options.ButtonsAcceptInterval.TotalMilliseconds / 100);
            _logic.Stop(StopReason.Wait);
        }
    }

    private void ProcessAnswerer_FirstWinsClient(int answererIndex, int pressDurationMs)
    {
        if (ClientData.PendingAnswererIndex == -1 || pressDurationMs > 0 && pressDurationMs < ClientData.AnswererPressDuration)
        {
            ClientData.PendingAnswererIndex = answererIndex;
            ClientData.AnswererPressDuration = pressDurationMs > 0 ? pressDurationMs : (int)ClientData.Host.Options.ButtonsAcceptInterval.TotalMilliseconds;
        }
        else
        {
            ClientData.PendingAnswererIndicies.Add(answererIndex);
        }

        if (!ClientData.IsDeferringAnswer)
        {
            ClientData.WaitInterval = (int)(ClientData.Host.Options.ButtonsAcceptInterval.TotalMilliseconds / 100);
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

            if (player.Name == playerName)
            {
                if (!player.CanPress)
                {
                    break;
                }

                if (DateTime.UtcNow.Subtract(player.LastBadTryTime).TotalSeconds < blockingButtonTime)
                {
                    break;
                }

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
                    _gameActions.SendMessageWithArgs(Messages.WrongTry, i);
                    
                    if (player.CanPress)
                    {
                        player.LastBadTryTime = DateTime.UtcNow;
                        _gameActions.SendMessageWithArgs(Messages.PlayerState, PlayerState.Lost, i);
                    }
                }

                return;
            }
        }
    }

    private void OnDisconnectRequested(string person) => DisconnectRequested?.Invoke(person);

    /// <summary>
    /// Updates game configuration.
    /// </summary>
    private void OnConfig(Message message, string[] args)
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
                if (args.Length > 2)
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

        if (ClientData.Players.Count <= 2
            || !int.TryParse(indexStr, out int index)
            || index <= -1
            || index >= ClientData.Players.Count)
        {
            return;
        }

        var account = ClientData.Players[index];
        var isOnline = account.IsConnected;

        if (ClientData.Stage != GameStage.Before && account.IsHuman && isOnline && !account.IsMoveable)
        {
            return;
        }

        ClientData.BeginUpdatePersons("DeleteTable " + message.Text);

        try
        {
            ClientData.Players.RemoveAt(index);
            Logic.AddHistory($"Player removed at {index}; AnswererIndex: {ClientData.AnswererIndex}");

            try
            {
                DropPlayerIndex(index);
            }
            catch (Exception exc)
            {
                throw new InvalidOperationException(
                    $"DropPlayerIndex error. Persons history: {ClientData.PersonsUpdateHistory}; logic history: {Logic.PrintHistory()}; stake history: {ClientData.OrderHistory}",
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

    [Obsolete("Use Logic.PlanExecution()")]
    private void PlanExecution(Tasks task, double taskTime, int arg = 0)
    {
        Logic.AddHistory($"PlanExecution old {task} {taskTime} {arg} ({ClientData.TInfo.Pause})");

        if (Logic.Runner.IsExecutionPaused)
        {
            Logic.Runner.UpdatePausedTask(task, arg, (int)taskTime);
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
            Logic.AddHistory($"AnswererIndex reduced to {ClientData.AnswererIndex}");
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

        if (Logic.Stakes.HandlePlayerDrop(playerIndex))
        {
            Logic.AddHistory($"StakerIndex set to {ClientData.Stakes.StakerIndex}");
        }

        // TODO: move to stakes.HandlePlayerDrop()
        if (ClientData.Question != null
            && ClientData.QuestionTypeName == QuestionTypes.Stake
            && ClientData.Order.Length > 0)
        {
            DropPlayerFromStakes(playerIndex);
        }

        if (Logic.HaveMultipleAnswerers())
        {
            DropPlayerFromAnnouncing(playerIndex);
        }

        DropPlayerFromThemeDeleters(playerIndex);
        DropPlayerFromQuestionHistory(playerIndex);
        ValidatePlayers();

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
                _logic.PlanExecution(Tasks.AskFirst, 20);
                break;
        }
    }

    private void DropPlayerFromThemeDeleters(int playerIndex)
    {
        var themeDeleters = ClientData.ThemeDeleters;

        if (themeDeleters == null)
        {
            return;
        }

        Logic.AddHistory($"ThemeDeleters before remove: {themeDeleters}");
        themeDeleters.RemoveAt(playerIndex);
        Logic.AddHistory($"ThemeDeleters removed: {playerIndex}; result: {themeDeleters}");

        if (!ClientData.IsWaiting)
        {
            return;
        }

        if (ClientData.Decision == DecisionType.NextPersonFinalThemeDeleting)
        {
            if (themeDeleters.IsEmpty())
            {
                _logic.StopWaiting();
                ClientData.MoveDirection = MoveDirections.RoundNext;
                _logic.Stop(StopReason.Move);
            }
            else
            {
                var indicies = themeDeleters.Current.PossibleIndicies;
                var hasAnyFlag = false;

                for (var i = 0; i < ClientData.Players.Count; i++)
                {
                    ClientData.Players[i].Flag = indicies.Contains(i);
                    hasAnyFlag = true;
                }

                if (!hasAnyFlag)
                {
                    _logic.PlanExecution(Tasks.AskToDelete, 10);
                }
            }
        }
        else if (ClientData.Decision == DecisionType.ThemeDeleting)
        {
            if (themeDeleters.IsEmpty())
            {
                _logic.StopWaiting();
                ClientData.MoveDirection = MoveDirections.RoundNext;
                _logic.Stop(StopReason.Move);
            }
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

            var newPlayerIndex = answerResult.PlayerIndex - (answerResult.PlayerIndex > playerIndex ? 1 : 0);
            newHistory.Add(new AnswerResult(newPlayerIndex, answerResult.IsRight, answerResult.Sum));
        }

        ClientData.QuestionHistory.Clear();
        ClientData.QuestionHistory.AddRange(newHistory);
    }

    private void ValidatePlayers()
    {
        var playersAreValid = ClientData.PlayersValidator == null || ClientData.PlayersValidator();

        if (playersAreValid)
        {
            return;
        }

        _logic.StopWaiting();
        ClientData.MoveDirection = MoveDirections.RoundNext;
        _logic.Stop(StopReason.Move);
    }

    private void DropPlayerFromStakes(int playerIndex)
    {
        var currentOrder = ClientData.Order;
        var currentOrderIndex = ClientData.OrderIndex;
        var currentStaker = currentOrderIndex == -1 ? -1 : currentOrder[currentOrderIndex];

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
            ClientData.SkipQuestion?.Invoke();
            Logic.PlanExecution(Tasks.MoveNext, 20, 1);
        }
        else if (currentOrderIndex != -1 && ClientData.OrderIndex == -1
            || currentStaker != -1 && ClientData.Order[ClientData.OrderIndex] == -1)
        {
            Logic.AddHistory("Current staker dropped");

            if (ClientData.Decision == DecisionType.StakeMaking || ClientData.Decision == DecisionType.NextPersonStakeMaking)
            {
                // Staker has been deleted. We need to move game further
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

    /// <summary>
    /// Correctly removes current answerer from the game.
    /// </summary>
    private void DropCurrentAnswerer()
    {
        // Drop answerer index
        ClientData.AnswererIndex = -1;

        if (!ClientData.IsQuestionAskPlaying)
        {
            return;
        }

        var nextTask = Logic.Runner.PendingTask;

        Logic.AddHistory(
            $"AnswererIndex dropped; nextTask = {nextTask};" +
            $" ClientData.Decision = {ClientData.Decision}");

        if ((ClientData.Decision == DecisionType.Answering ||
            ClientData.Decision == DecisionType.AnswerValidating) && !Logic.HaveMultipleAnswerers())
        {
            // Answerer has been dropped. The game should be moved forward
            Logic.StopWaiting();

            if (ClientData.IsOralNow)
            {
                _gameActions.SendMessage(Messages.Cancel, ClientData.ShowMan.Name);
            }

            Logic.PlanExecution(Tasks.ContinueQuestion, 1);
        }
        else if (nextTask == Tasks.AskRight || nextTask == Tasks.WaitRight)
        {
            // Player has been removed after giving answer. But the answer has not been validated by showman yet
            if (ClientData.QuestionPlayState.AnswererIndicies.Count == 0)
            {
                Logic.PlanExecution(Tasks.ContinueQuestion, 1);
            }
            else
            {
                Logic.PlanExecution(Tasks.Announce, 15);
            }
        }
        else if (ClientData.QuestionPlayState.AnswererIndicies.Count == 0 && Logic.IsSpecialQuestion())
        {            
            ClientData.SkipQuestion?.Invoke();
            Logic.PlanExecution(Tasks.MoveNext, 20, 1);
        }
        else if (nextTask == Tasks.AnnounceStake)
        {
            Logic.PlanExecution(Tasks.Announce, 15);
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
        if (Logic.TryDetectStakesWinner())
        {
            if (ClientData.Stake == -1)
            {
                ClientData.Stake = ClientData.CurPriceRight;
            }

            return;
        }

        if (ClientData.OrderIndex > -1 && ClientData.Decision == DecisionType.NextPersonStakeMaking)
        {
            Logic.AddHistory("Rolling order index back");
            ClientData.OrderIndex--;
        }

        Logic.PlanExecution(Tasks.AskStake, 20);
    }

    private void FreeTable(Message message, string[] args, Account host)
    {
        if (args.Length <= 2)
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

        if (!account.IsConnected || !account.IsHuman || ClientData.Stage != GameStage.Before && !account.IsMoveable)
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
                InheritAccountState(ClientData.Players[index], account);
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

    private static void InheritAccountState(GamePlayerAccount gamePlayerAccount, ViewerAccount account)
    {
        if (account is not GamePlayerAccount playerAccount)
        {
            return;
        }

        gamePlayerAccount.Flag = playerAccount.Flag;
        gamePlayerAccount.AppellationFlag = playerAccount.AppellationFlag;
        gamePlayerAccount.StakeMaking = playerAccount.StakeMaking;
        gamePlayerAccount.CanPress = playerAccount.CanPress;
        gamePlayerAccount.InGame = playerAccount.InGame;
        // TODO: think about inheriting other properties
    }

    private void SetPerson(string[] args, Account host)
    {
        if (args.Length <= 4)
        {
            return;
        }

        var personType = args[2];
        var replacer = args[4];

        // Who is replaced
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

        if (ClientData.Stage != GameStage.Before && account.IsConnected && !account.IsMoveable)
        {
            return;
        }

        var oldName = account.Name;
        GamePersonAccount? newAccount;

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
            if (!SetHumanPerson(isPlayer, account, replacer, index))
            {
                return;
            }

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

    internal bool SetHumanPerson(bool isPlayer, GamePersonAccount account, string replacer, int index)
    {
        int otherIndex = -1;
        // Who replaces
        ViewerAccount? otherAccount = null;

        ClientData.BeginUpdatePersons($"SetHumanPerson {account.Name} {account.IsConnected} {replacer} {index}");

        try
        {
            if (ClientData.ShowMan.Name == replacer && ClientData.ShowMan.IsHuman)
            {
                otherAccount = ClientData.ShowMan;

                if (ClientData.Stage != GameStage.Before && otherAccount.IsConnected && !otherAccount.IsMoveable)
                {
                    return false;
                }

                ClientData.ShowMan = new GamePersonAccount(account)
                {
                    Ready = account.Ready,
                    IsConnected = account.IsConnected,
                    IsMoveable = account.IsMoveable
                };
            }
            else
            {
                for (var i = 0; i < ClientData.Players.Count; i++)
                {
                    if (ClientData.Players[i].Name == replacer && ClientData.Players[i].IsHuman)
                    {
                        otherAccount = ClientData.Players[i];

                        if (ClientData.Stage != GameStage.Before && otherAccount.IsConnected && !otherAccount.IsMoveable)
                        {
                            return false;
                        }

                        ClientData.Players[i] = new GamePlayerAccount(account)
                        {
                            Ready = account.Ready,
                            IsConnected = account.IsConnected,
                            IsMoveable = account.IsMoveable,
                            Flag = ClientData.Players[i].Flag,
                            Sum = ClientData.Players[i].Sum
                        };

                        InheritAccountState(ClientData.Players[i], otherAccount);

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

                            if (ClientData.Stage != GameStage.Before && otherAccount.IsConnected && !otherAccount.IsMoveable)
                            {
                                return false;
                            }

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

                if (otherIndex == -1 || otherAccount == null)
                {
                    return false;
                }
            }

            // Human account is replaced by another human account
            var otherPerson = otherAccount as GamePersonAccount;

            if (isPlayer)
            {
                var previousPlayer = ClientData.Players[index];

                ClientData.Players[index] = new GamePlayerAccount(otherAccount)
                {
                    IsConnected = otherAccount.IsConnected,
                    IsMoveable = otherAccount.IsMoveable,
                    Sum = previousPlayer.Sum
                };

                InheritAccountState(ClientData.Players[index], previousPlayer);

                if (otherPerson != null)
                {
                    ClientData.Players[index].Ready = otherPerson.Ready;
                }
            }
            else
            {
                ClientData.ShowMan = new GamePersonAccount(otherAccount)
                {
                    IsConnected = otherAccount.IsConnected,
                    IsMoveable = otherAccount.IsMoveable
                };

                if (otherPerson != null)
                {
                    ClientData.ShowMan.Ready = otherPerson.Ready;
                }
            }

            InformAvatar(otherAccount);
            return true;
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
            ClientData.Host.LogWarning("ChangePersonType: account == null");
            return;
        }

        if (ClientData.Stage != GameStage.Before && account.IsConnected && !account.IsMoveable)
        {
            return;
        }

        var oldName = account.Name;

        var newType = !account.IsHuman;
        string newName = "";
        bool newIsMale = true;

        ViewerAccount? newAcc = null;

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

                if (!isPlayer)
                {
                    ClientData.IsOral = ClientData.Settings.AppSettings.Oral && ClientData.ShowMan.IsHuman;
                }
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

                ClientData.IsOral = ClientData.Settings.AppSettings.Oral && ClientData.ShowMan.IsHuman;
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
            IsConnected = true,
            Flag = ClientData.Players[index].Flag
        };

        ClientData.Players[index] = newAccount;

        var playerClient = Network.Clients.Client.Create(newAccount.Name, _client.Node);
        var data = new ViewerData();
        var actions = new ViewerActions(playerClient);
        var logic = new ViewerComputerLogic(data, actions, new Intelligence(account), GameRole.Player);
        _ = new Player(playerClient, account, false, logic, actions, data);

        OnInfo(newAccount.Name);

        return newAccount;
    }

    private GamePersonAccount CreateNewComputerShowman(ComputerAccount account)
    {
        if (ClientData.Host == null)
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
        var data = new ViewerData();
        var actions = new ViewerActions(showmanClient);
        
        var logic = new ViewerComputerLogic(
            data,
            actions,
            new Intelligence(account),
            GameRole.Showman);
        
        var showman = new Showman(showmanClient, account, false, logic, actions, data);

        OnInfo(newAccount.Name);

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

            var connectedMessage = LO[account.IsMale ? nameof(R.Connected_Male) : nameof(R.Connected_Female)];
            _gameActions.SpecialReplic(string.Format(connectedMessage, name));

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

        var connectedMessage = LO[account.IsMale ? nameof(R.Connected_Male) : nameof(R.Connected_Female)];
        _gameActions.SpecialReplic(string.Format(connectedMessage, name));
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

    private void InformAvatar(ViewerAccount account)
    {
        foreach (var personName in ClientData.AllPersons.Keys)
        {
            if (account.Name != personName && personName != NetworkConstants.GameName)
            {
                InformAvatar(account, personName);
            }
        }
    }

    private void InformAvatar(ViewerAccount account, string receiver)
    {
        if (!string.IsNullOrEmpty(account.Picture))
        {
            var link = CreateUri(account.Name, account.Picture, receiver);

            if (link != null)
            {
                _gameActions.SendMessageToWithArgs(receiver, Messages.Avatar, account.Name, ContentTypes.Image, link);

                // for backward compatibility
                _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.Picture, account.Name, link), receiver);
            }
        }

        if (!string.IsNullOrEmpty(account.AvatarVideoUri))
        {
            _gameActions.SendMessageToWithArgs(receiver, Messages.Avatar, account.Name, ContentTypes.Video, account.AvatarVideoUri);
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
                if (!ClientData.Host.AreCustomAvatarsSupported)
                {
                    return null;
                }

                var complexName = $"{(personName != null ? personName + "_" : "")}{Path.GetFileName(avatarUri)}";

                if (!_avatarHelper.FileExists(complexName))
                {
                    _avatarHelper.AddFile(avatarUri, complexName);
                }

                path = _fileShare.CreateResourceUri(ResourceKind.Avatar, new Uri(complexName, UriKind.Relative));
            }

            return path;
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
