using Notions;
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
public sealed class Game : MessageHandler
{
    private const string VideoAvatarUri = "https://vdo.ninja/";

    public event Action<Game, bool, bool>? PersonsChanged;

    /// <summary>
    /// Informs the hosting environment that a person with provided name should be disconnected.
    /// </summary>
    public event Action<Game, string>? DisconnectRequested;
    private readonly GameActions _gameActions;

    private IPrimaryNode Master => (IPrimaryNode)_client.Node;

    private readonly ComputerAccount[] _defaultPlayers;
    private readonly ComputerAccount[] _defaultShowmans;

    private readonly IFileShare _fileShare;
    private readonly IAvatarHelper _avatarHelper;

    private readonly GameLogic _controller;

    public GameLogic Logic => _controller;

    private ILocalizer LO { get; }

    private readonly GameData _state;

    public GameData ClientData => _state;

    public Game(
        Client client,
        ILocalizer localizer,
        GameData state,
        GameActions actions,
        GameLogic controller,
        ComputerAccount[] defaultPlayers,
        ComputerAccount[] defaultShowmans,
        IFileShare fileShare,
        IAvatarHelper avatarHelper)
        : base(client)
    {
        _gameActions = actions;
        _controller = controller;
        LO = localizer;
        _state = state;

        _controller.AutoGame += AutoGame;

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
        _state.TaskLock.Dispose();
        _state.TableInformStageLock.Dispose();

        base.Dispose(disposing);
    }

    /// <summary>
    /// Starts the game engine.
    /// </summary>
    public void Run()
    {
        Client.CurrentNode.SerializationError += CurrentServer_SerializationError;

        _controller.Run();

        foreach (var personName in _state.AllPersons.Keys)
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
            _state.GameName,
            _controller.Engine.PackageName,
            _controller.Engine.ContactUri,
            _state.Settings.NetworkVoiceChat);

        _gameActions.SendMessageToWithArgs(person, Messages.Hostname, _state.HostName ?? "");
    }

    private void OnSetOptions(Message message, string[] mparams)
    {
        if (message.Sender != _state.HostName)
        {
            return;
        }

        var appSettings = _state.Settings.AppSettings;
        var msg = new MessageBuilder(Messages.Options2, _state.HostName);
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
                        _state.IsOral = appSettings.Oral && _state.ShowMan.IsHuman;
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

        var appSettings = _state.Settings.AppSettings;

        _gameActions.SendMessageToWithArgs(
            person,
            Messages.ReadingSpeed,
            appSettings.Managed ? 0 : appSettings.ReadingSpeed);

        _gameActions.SendMessageToWithArgs(person, Messages.FalseStart, appSettings.FalseStart ? "+" : "-");
        _gameActions.SendMessageToWithArgs(person, Messages.ButtonBlockingTime, appSettings.TimeSettings.TimeForBlockingButton);
        _gameActions.SendMessageToWithArgs(person, Messages.ApellationEnabled, appSettings.UseApellations ? '+' : '-');

        var maxPressingTime = _state.TimeSettings.ButtonPressing * 10;
        _gameActions.SendMessageToWithArgs(person, Messages.Timer, 1, MessageParams.Timer_MaxTime, maxPressingTime);

        _gameActions.SendMessageToWithArgs(person, Messages.SetJoinMode, _state.JoinMode);

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
            nameof(appSettings.TimeSettings.TimeForBlockingButton), appSettings.TimeSettings.TimeForBlockingButton,
            nameof(_state.TimeSettings.ButtonBlocking), _state.TimeSettings.ButtonBlocking,
            nameof(_state.TimeSettings.PartialImage), _state.TimeSettings.PartialImage);
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
            InformAvatar(_state.ShowMan, person);

            foreach (var item in _state.Players)
            {
                InformAvatar(item, person);
            }
        }
        else
        {
            InformAvatar(_state.ShowMan);

            foreach (var item in _state.Players)
            {
                InformAvatar(item);
            }
        }
    }

    private void InformPersons(string person)
    {
        var info = new StringBuilder(Messages.Info2)
            .Append(Message.ArgsSeparatorChar)
            .Append(_state.Players.Count)
            .Append(Message.ArgsSeparatorChar);

        AppendAccountExt(_state.ShowMan, info);

        info.Append(Message.ArgsSeparatorChar);

        foreach (var player in _state.Players)
        {
            AppendAccountExt(player, info);

            info.Append(Message.ArgsSeparatorChar);
        }

        foreach (var viewer in _state.Viewers)
        {
            if (!viewer.IsConnected)
            {
                _state.Host.LogWarning($"Viewer {viewer.Name} not connected\n" + _state.PersonsUpdateHistory);
                continue;
            }

            if (_state.Players.Any(p => p.Name == viewer.Name)
                || _state.ShowMan.Name == viewer.Name)
            {
                // Need to catch that state
                _state.Host.LogWarning($"Viewer {viewer.Name} is already in players or showman\n" + _state.PersonsUpdateHistory);
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
        var total = _state.Players.Count;

        for (int i = 0; i < total; i++)
        {
            if (s.Length > 0)
            {
                s.Append(", ");
            }

            s.AppendFormat("{0}: {1}", _state.Players[i].Name, _state.Players[i].Sum);
        }

        return s.ToString();
    }

    public ConnectionPersonData[] GetInfo()
    {
        var result = new List<ConnectionPersonData>
        {
            new() { Name = _state.ShowMan.Name, Role = GameRole.Showman, IsOnline = _state.ShowMan.IsConnected }
        };

        for (int i = 0; i < _state.Players.Count; i++)
        {
            result.Add(new ConnectionPersonData
            {
                Name = _state.Players[i].Name,
                Role = GameRole.Player,
                IsOnline = _state.Players[i].IsConnected
            });
        }

        for (int i = 0; i < _state.Viewers.Count; i++)
        {
            result.Add(new ConnectionPersonData
            {
                Name = _state.Viewers[i].Name,
                Role = GameRole.Viewer,
                IsOnline = _state.Viewers[i].IsConnected
            });
        }

        return result.ToArray();
    }

    private AuthenticationResult AuthenticateCore(
        string name,
        bool isMale,
        GameRole role,
        string? password)
    {
        if (_state.JoinMode == JoinMode.Forbidden)
        {
            return AuthenticationResult.Forbidden;
        }

        if (_state.JoinMode == JoinMode.OnlyViewer && role != GameRole.Viewer)
        {
            return AuthenticationResult.ForbiddenRole;
        }

        if (!string.IsNullOrEmpty(_state.Settings.NetworkGamePassword)
            && _state.Settings.NetworkGamePassword != password)
        {
            return AuthenticationResult.WrongPassword;
        }

        if (_state.AllPersons.ContainsKey(name))
        {
            return AuthenticationResult.NameIsOccupied;
        }

        var index = -1;
        IEnumerable<ViewerAccount>? accountsToSearch;

        switch (role)
        {
            case GameRole.Showman:
                accountsToSearch = new ViewerAccount[1] { _state.ShowMan };
                break;

            case GameRole.Player:
                accountsToSearch = _state.Players;

                if (_state.HostName == name) // Host is joining
                {
                    var players = _state.Players;

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
                        return AuthenticationResult.PositionNotFound;
                    }
                }

                break;

            default: // Viewer
                accountsToSearch = _state.Viewers.Concat(
                    new ViewerAccount[] { new(Constants.FreePlace, false, false) { IsHuman = true } });

                break;
        }

        var found = false;

        if (index > -1)
        {
            var accounts = accountsToSearch.ToArray();

            var result = TryAuthenticateAccount(role, name, isMale, index, accounts[index]);

            if (result.HasValue)
            {
                if (!result.Value)
                {
                    return AuthenticationResult.PlaceIsOccupied;
                }
                else
                {
                    found = true;
                }
            }
        }
        else
        {
            foreach (var account in accountsToSearch)
            {
                index++;

                var result = TryAuthenticateAccount(role, name, isMale, index, account);

                if (result.HasValue)
                {
                    if (!result.Value)
                    {
                        return AuthenticationResult.PlaceIsOccupied;
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
            return AuthenticationResult.FreePlaceNotFound;
        }

        return AuthenticationResult.Ok;
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
        _state.TaskLock.WithLock(() =>
        {
            var result = AuthenticateCore(name, isMale, role, password);

            if (result != AuthenticationResult.Ok)
            {
                return (false, GetAuthenticationErrorMessage(result));
            }

            connectionAuthenticator();

            return (true, "");
        },
        5000);

    /// <summary>
    /// Authenticates person in the game.
    /// </summary>
    public AuthenticationResult Authenticate(
        string name,
        bool isMale,
        GameRole role,
        string? password) =>
        _state.TaskLock.WithLock(() => AuthenticateCore(name, isMale, role, password), 5000);

    private string GetAuthenticationErrorMessage(AuthenticationResult result) =>
        result switch
        {
            AuthenticationResult.Forbidden => LO[nameof(R.JoinForbidden)],
            AuthenticationResult.ForbiddenRole => LO[nameof(R.JoinRoleForbidden)],
            AuthenticationResult.WrongPassword => LO[nameof(R.WrongPassword)],
            AuthenticationResult.NameIsOccupied => LO[nameof(R.PersonWithSuchNameIsAlreadyInGame)],
            AuthenticationResult.PositionNotFound => LO[nameof(R.PositionNotFoundByIndex)],
            AuthenticationResult.PlaceIsOccupied => LO[nameof(R.PlaceIsOccupied)],
            AuthenticationResult.FreePlaceNotFound => LO[nameof(R.NoFreePlaceForName)],
            _ => "",
        };

    /// <summary>
    /// Processed received message.
    /// </summary>
    /// <param name="message">Received message.</param>
    public override ValueTask OnMessageReceivedAsync(Message message) =>
        _state.TaskLock.WithLockAsync(() =>
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
                        res.Append(Message.ArgsSeparatorChar).Append(_state.Settings.NetworkGameName);
                        res.Append(Message.ArgsSeparatorChar).Append(_state.HostName);
                        res.Append(Message.ArgsSeparatorChar).Append(_state.Players.Count);

                        res.Append(Message.ArgsSeparatorChar).Append(_state.ShowMan.Name);
                        res.Append(Message.ArgsSeparatorChar).Append(_state.ShowMan.IsConnected ? '+' : '-');
                        res.Append(Message.ArgsSeparatorChar).Append('-');

                        for (int i = 0; i < _state.Players.Count; i++)
                        {
                            res.Append(Message.ArgsSeparatorChar).Append(_state.Players[i].Name);
                            res.Append(Message.ArgsSeparatorChar).Append(_state.Players[i].IsConnected ? '+' : '-');
                            res.Append(Message.ArgsSeparatorChar).Append('-');
                        }

                        for (int i = 0; i < _state.Viewers.Count; i++)
                        {
                            res.Append(Message.ArgsSeparatorChar).Append(_state.Viewers[i].Name);
                            res.Append(Message.ArgsSeparatorChar).Append(_state.Viewers[i].IsConnected ? '+' : '-');
                            res.Append(Message.ArgsSeparatorChar).Append('-');
                        }

                        _gameActions.SendMessage(res.ToString(), message.Sender);

                        #endregion
                        break;

                    case Messages.Connect: // TODO: will be deprecated after switching to SIGame 8
                        OnConnect(message, args);
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
                        if (message.Sender == _state.HostName && args.Length > 1)
                        {
                            if (Enum.TryParse<JoinMode>(args[1], out var joinMode))
                            {
                                _state.JoinMode = joinMode;
                                _gameActions.SendMessageWithArgs(Messages.SetJoinMode, args[1], "+");
                            }
                        }
                        break;

                    case Messages.Pause:
                        OnPause(message, args);
                        break;

                    case Messages.Start:
                        if (message.Sender == _state.HostName && _state.Stage == GameStage.Before)
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
                        if (message.Sender == _state.HostName)
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

                    case Messages.MediaPreloadProgress:
                        OnMediaPreloadProgress(message, args);
                        break;

                    case Messages.Report:
                        OnReport(message, args);
                        break;

                    case Messages.IsRight:
                        OnIsRight(message, args);
                        break;

                    case Messages.Next:
                        if (_state.IsWaiting &&
                            _state.Decision == DecisionType.NextPersonStakeMaking &&
                            message.Sender == _state.ShowMan.Name)
                        {
                            #region Next

                            if (args.Length > 1 && int.TryParse(args[1], out int n) && n > -1 && n < _state.Players.Count)
                            {
                                if (_state.Players[n].Flag)
                                {
                                    _state.Order[_state.OrderIndex] = n;
                                    Logic.CheckOrder(_state.OrderIndex);
                                    _controller.Stop(StopReason.Decision);
                                }
                            }

                            #endregion
                        }
                        break;

                    case Messages.Cat:
                        if (_state.IsWaiting
                            && _state.Decision == DecisionType.QuestionAnswererSelection
                            && (_state.Chooser != null && message.Sender == _state.Chooser.Name
                                || _state.IsOralNow && message.Sender == _state.ShowMan.Name))
                        {
                            #region Cat

                            try
                            {
                                if (int.TryParse(args[1], out int index) && index > -1 && index < _state.Players.Count && _state.Players[index].Flag)
                                {
                                    _state.AnswererIndex = index;
                                    _state.QuestionPlay.SetSingleAnswerer(index);

                                    if (_state.IsOralNow)
                                    {
                                        _gameActions.SendMessage(
                                            Messages.Cancel,
                                            message.Sender == _state.ShowMan.Name ? _state.Chooser.Name : _state.ShowMan.Name);
                                    }

                                    _controller.Stop(StopReason.Decision);
                                }
                            }
                            catch (Exception ex)
                            {
                                _state.Host.SendError(ex, true);
                            }

                            #endregion
                        }
                        break;

                    case Messages.SelectPlayer:
                        {
                            if (_state.IsWaiting
                                && _state.DecisionMakers.Contains(message.Sender)
                                && args.Length > 1
                                && int.TryParse(args[1], out var playerIndex)
                                && playerIndex > -1
                                && playerIndex < _state.Players.Count
                                && _state.Players[playerIndex].Flag)
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
                        if (_state.IsWaiting && _state.Decision == DecisionType.HiddenStakeMaking)
                        {
                            #region FinalStake

                            for (var i = 0; i < _state.Players.Count; i++)
                            {
                                var player = _state.Players[i];

                                if (_state.QuestionPlay.AnswererIndicies.Contains(i) && player.PersonalStake == -1 && message.Sender == player.Name)
                                {
                                    if (int.TryParse(args[1], out int finalStake) && finalStake >= 1 && finalStake <= player.Sum)
                                    {
                                        player.PersonalStake = finalStake;
                                        _state.HiddenStakerCount--;

                                        _gameActions.SendMessageWithArgs(Messages.PersonFinalStake, i);
                                    }

                                    break;
                                }
                            }

                            if (_state.HiddenStakerCount == 0)
                            {
                                _controller.Stop(StopReason.Decision);
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
                        if (!_state.CanMarkQuestion || args.Length < 3)
                        {
                            break;
                        }

                        _state.GameResultInfo.ComplainedQuestions.Add(new QuestionReport
                        {
                            ThemeName = _state.Theme.Name,
                            QuestionText = _state.Question?.GetText(),
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
        if (message.Sender != _state.ShowMan.Name || args.Length <= 2)
        {
            return;
        }

        var answer = args[1];
        var validationStatus = args[2] == "+";
        var validationFactor = args.Length > 3 && double.TryParse(args[3], out var factor) && factor >= 0.0 ? factor : 1.0;

        if (!_state.QuestionPlay.Validations.TryGetValue(answer, out var validation) || validation.HasValue)
        {
            return;
        }

        _state.QuestionPlay.Validations[answer] = (validationStatus, validationFactor);
    }

    private void OnPin(string hostName)
    {
        var pin = Logic.PinHelper?.GeneratePin() ?? 0;
        _gameActions.SendMessageToWithArgs(hostName, Messages.Pin, pin);
    }

    private void OnSelectedPlayer(int playerIndex, string messageSender)
    {
        switch (_state.Decision)
        {
            case DecisionType.StarterChoosing:
                _state.ChooserIndex = playerIndex;
                _controller.Stop(StopReason.Decision);
                break;

            case DecisionType.NextPersonStakeMaking:
                _state.Order[_state.OrderIndex] = playerIndex;
                Logic.CheckOrder(_state.OrderIndex);
                _controller.Stop(StopReason.Decision);
                break;

            case DecisionType.NextPersonFinalThemeDeleting:
                _state.ThemeDeleters?.Current.SetIndex(playerIndex);
                _controller.Stop(StopReason.Decision);
                break;

            case DecisionType.QuestionAnswererSelection:
                _state.AnswererIndex = playerIndex;
                _state.QuestionPlay.SetSingleAnswerer(playerIndex);

                if (_state.IsOralNow)
                {
                    _gameActions.SendMessage(
                        Messages.Cancel,
                        messageSender == _state.ShowMan.Name ? _state.Chooser.Name : _state.ShowMan.Name);
                }

                _controller.Stop(StopReason.Decision);
                break;

            default:
                return;
        }
    }

    private void OnChoice(Message message, string[] args)
    {
        if (!_state.IsWaiting
            || _state.Decision != DecisionType.QuestionSelection
            || args.Length != 3
            || _state.Chooser == null
            || message.Sender != _state.Chooser.Name
                && (!_state.IsOralNow || message.Sender != _state.ShowMan.Name))
        {
            return;
        }

        if (!int.TryParse(args[1], out var themeIndex) || !int.TryParse(args[2], out var questionIndex))
        {
            return;
        }

        if (themeIndex < 0 || themeIndex >= _state.TInfo.RoundInfo.Count)
        {
            return;
        }

        if (questionIndex < 0 || questionIndex >= _state.TInfo.RoundInfo[themeIndex].Questions.Count)
        {
            return;
        }

        if (!_state.TInfo.RoundInfo[themeIndex].Questions[questionIndex].IsActive())
        {
            return;
        }

        _state.ThemeIndex = themeIndex;
        _state.QuestionIndex = questionIndex;

        if (_state.IsOralNow)
        {
            _gameActions.SendMessage(Messages.Cancel, _state.ShowMan.Name);
        }

        if (Logic.CanPlayerAct())
        {
            _gameActions.SendMessage(Messages.Cancel, _state.Chooser.Name);
        }

        _controller.Stop(StopReason.Decision);
    }

    private void OnFirst(Message message, string[] args)
    {
        if (!_state.IsWaiting
            || _state.Decision != DecisionType.StarterChoosing
            || message.Sender != _state.ShowMan.Name
            || args.Length <= 1)
        {
            return;
        }

        if (!int.TryParse(args[1], out int playerIndex) || playerIndex <= -1 || playerIndex >= _state.Players.Count || !_state.Players[playerIndex].Flag)
        {
            return;
        }

        _state.ChooserIndex = playerIndex;
        _controller.Stop(StopReason.Decision);
    }

    private void OnSetChooser(Message message, string[] args)
    {
        if (message.Sender != _state.ShowMan.Name || args.Length <= 1)
        {
            return;
        }

        if (!int.TryParse(args[1], out int playerIndex) || playerIndex <= -1 || playerIndex >= _state.Players.Count)
        {
            return;
        }

        if (_state.ChooserIndex == playerIndex)
        {
            return;
        }

        var isChoosingNow = _controller.Runner.PendingTask == Tasks.WaitChoose;

        if (isChoosingNow)
        {
            _controller.StopWaiting();

            if (_state.IsOralNow)
            {
                _gameActions.SendMessage(Messages.Cancel, _state.ShowMan.Name);
            }

            if (_state.Chooser != null && Logic.CanPlayerAct())
            {
                _gameActions.SendMessage(Messages.Cancel, _state.Chooser.Name);
            }
        }

        _state.ChooserIndex = playerIndex;
        _gameActions.SendMessageWithArgs(Messages.SetChooser, _state.ChooserIndex, "-", "+");

        if (isChoosingNow)
        {
            _controller.PlanExecution(Tasks.AskToSelectQuestion, 10);
        }
    }

    private void OnMoveable(Message message)
    {
        if (_state.AllPersons.TryGetValue(message.Sender, out var person))
        {
            person.IsMoveable = true;
        }
    }

    private void OnReport(Message message, string[] args)
    {
        if (_state.Decision != DecisionType.Reporting
            || !_state.Players.Any(player => player.Name == message.Sender)
            || _state.GameResultInfo.Reviews.ContainsKey(message.Sender))
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
            _state.Host.LogWarning("Player error: " + review);
            return;
        }

        _state.ReportsCount--;

        if (review.Length > 0)
        {
            _state.GameResultInfo.Reviews[message.Sender] = review.ToString();
        }

        if (_state.ReportsCount == 0)
        {
            _controller.RescheduleTask();
        }
    }

    private void OnMediaLoaded(Message message) => _gameActions.SendMessageToWithArgs(_state.ShowMan.Name, Messages.MediaLoaded, message.Sender);

    private void OnMediaPreloaded(Message message) => _gameActions.SendMessageToWithArgs(_state.ShowMan.Name, Messages.MediaPreloaded, message.Sender);

    private void OnMediaPreloadProgress(Message message, string[] args)
    {
        if (args.Length < 2 || !int.TryParse(args[1], out var progress) || progress < 0 || progress > 100)
        {
            return;
        }
        
        _gameActions.SendMessageToWithArgs(_state.ShowMan.Name, Messages.MediaPreloadProgress, message.Sender, progress);
    }

    private void OnToggle(Message message, string[] args)
    {
        if (_state.TableController == null)
        {
            return;
        }

        if (message.Sender != _state.ShowMan.Name || args.Length < 3)
        {
            return;
        }

        if (!int.TryParse(args[1], out var themeIndex) || !int.TryParse(args[2], out var questionIndex))
        {
            return;
        }

        if (themeIndex < 0 || themeIndex >= _state.TInfo.RoundInfo.Count)
        {
            return;
        }

        if (questionIndex < 0 || questionIndex >= _state.TInfo.RoundInfo[themeIndex].Questions.Count)
        {
            return;
        }

        var question = _state.TInfo.RoundInfo[themeIndex].Questions[questionIndex];

        if (question.IsActive())
        {
            if (!_state.TableController.RemoveQuestion(themeIndex, questionIndex))
            {
                return;
            }

            var oldPrice = question.Price;
            question.Price = Question.InvalidPrice;
            _gameActions.SendMessageWithArgs(Messages.Toggle, themeIndex, questionIndex, Question.InvalidPrice);
        }
        else
        {
            if (!_state.TableController.RestoreQuestion(themeIndex, questionIndex))
            {
                return;
            }
        }
    }

    private void OnKick(Message message, string[] args)
    {
        if (message.Sender != _state.HostName || args.Length <= 1)
        {
            return;
        }

        var clientName = args[1];

        if (!_state.AllPersons.TryGetValue(clientName, out var person))
        {
            return;
        }

        if (person.Name == message.Sender)
        {
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.Replic, ReplicCodes.Special.ToString(), GameError.CannotKickYouself); // TODO: REMOVE: replaced by USER_ERROR message
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.UserError, ErrorCode.CannotKickYourSelf);
            return;
        }

        if (!person.IsHuman)
        {
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.Replic, ReplicCodes.Special.ToString(), GameError.CannotKickBots); // TODO: REMOVE: replaced by USER_ERROR message
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.UserError, ErrorCode.CannotKickBots);
            return;
        }

        _gameActions.SendMessageToWithArgs(clientName, Messages.YouAreKicked);
        OnDisconnectRequested(clientName);

        var clientId = Master.Kick(clientName);

        if (clientId.Length > 0)
        {
            _gameActions.SendMessageWithArgs(Messages.Banned, clientId, clientName);
        }
    }

    private void OnBan(Message message, string[] args)
    {
        if (message.Sender != _state.HostName || args.Length <= 1)
        {
            return;
        }

        var clientName = args[1];

        if (!_state.AllPersons.TryGetValue(clientName, out var person))
        {
            return;
        }

        if (person.Name == message.Sender)
        {
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.Replic, ReplicCodes.Special.ToString(), GameError.CannotBanYourself); // TODO: REMOVE: replaced by USER_ERROR message
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.UserError, ErrorCode.CannotKickYourSelf);
            return;
        }

        if (!person.IsHuman)
        {
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.Replic, ReplicCodes.Special.ToString(), GameError.CannotBanBots); // TODO: REMOVE: replaced by USER_ERROR message
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.UserError, ErrorCode.CannotKickBots);
            return;
        }

        _gameActions.SendMessageToWithArgs(clientName, Messages.YouAreKicked);
        OnDisconnectRequested(clientName);

        var clientId = Master.Kick(clientName, true);

        if (clientId.Length > 0)
        {
            _gameActions.SendMessageWithArgs(Messages.Banned, clientId, clientName);
        }
    }

    private void OnSetHost(Message message, string[] args)
    {
        if (message.Sender != _state.HostName || args.Length <= 1)
        {
            return;
        }

        var clientName = args[1];

        if (!_state.AllPersons.TryGetValue(clientName, out var person))
        {
            return;
        }

        if (person.Name == message.Sender)
        {
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.Replic, ReplicCodes.Special.ToString(), GameError.CannotSetHostToYourself); // TODO: REMOVE: replaced by USER_ERROR message
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.UserError, ErrorCode.CannotSetHostToYourself);
            return;
        }

        if (!person.IsHuman)
        {
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.Replic, ReplicCodes.Special.ToString(), GameError.CannotSetHostToBots); // TODO: REMOVE: replaced by USER_ERROR message
            _gameActions.SendMessageToWithArgs(message.Sender, Messages.UserError, ErrorCode.CannotSetHostToBots);
            return;
        }

        UpdateHostName(person.Name, message.Sender);
    }

    private void OnUnban(Message message, string[] args)
    {
        if (message.Sender != _state.HostName || args.Length <= 1)
        {
            return;
        }

        var clientId = args[1];
        Master.Unban(clientId);
    }

    private void OnNextDelete(Message message, string[] args)
    {
        if (!_state.IsWaiting ||
            _state.Decision != DecisionType.NextPersonFinalThemeDeleting ||
            message.Sender != _state.ShowMan.Name ||
            args.Length <= 1 ||
            !int.TryParse(args[1], out int playerIndex) ||
            playerIndex <= -1 ||
            playerIndex >= _state.Players.Count ||
            !_state.Players[playerIndex].Flag)
        {
            return;
        }

        try
        {
            _state.ThemeDeleters?.Current.SetIndex(playerIndex);
        }
        catch (Exception exc)
        {
            throw new InvalidOperationException(
                $"SetIndex error. Person history: {_state.PersonsUpdateHistory}; Logic history: {Logic.PrintHistory()}",
                exc);
        }

        _controller.Stop(StopReason.Decision);
    }

    private void OnPicture(Message message, string[] args)
    {
        if (args.Length < 2)
        {
            return;
        }

        var path = args[1];
        var person = _state.MainPersons.FirstOrDefault(item => message.Sender == item.Name);

        if (person == null)
        {
            return;
        }

        if (args.Length > 2)
        {
            if (!_state.Host.AreCustomAvatarsSupported)
            {
                return;
            }

            var file = $"{message.Sender}_{Path.GetFileName(path)}";

            if (!_avatarHelper.FileExists(file))
            {
                var error = _avatarHelper.ExtractAvatarData(args[2], file);

                if (error != null)
                {
                    _gameActions.SendMessageToWithArgs(message.Sender, Messages.Replic, ReplicCodes.Special.ToString(), error.Value.Item2); // TODO: REMOVE: replaced by USER_ERROR message
                    _gameActions.SendMessageWithArgs(Messages.UserError, error.Value.Item1);
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
        var person = _state.MainPersons.FirstOrDefault(item => message.Sender == item.Name);

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
        if (!_state.IsWaiting
            || !_state.DecisionMakers.Contains(message.Sender)
            || args.Length <= 1
            || !Enum.TryParse<StakeModes>(args[1], out var stakeMode)
            || (_state.StakeModes & stakeMode) <= 0)
        {
            return;
        }

        var stakerName = message.Sender == _state.ShowMan.Name ? _state.DecisionMakers.First() : message.Sender;

        if (!_state.StakeLimits.TryGetValue(stakerName, out var stakeLimit))
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

        switch (_state.Decision)
        {
            case DecisionType.StakeMaking:
                _state.StakeType = FromStakeMode(stakeMode);
                _state.StakeSum = stakeSum;
                _controller.Stop(StopReason.Decision);
                break;

            case DecisionType.QuestionPriceSelection:
                _state.CurPriceRight = stakeSum;
                _controller.Stop(StopReason.Decision);
                break;

            case DecisionType.HiddenStakeMaking:
                for (var i = 0; i < _state.Players.Count; i++)
                {
                    var player = _state.Players[i];

                    if (stakerName == player.Name)
                    {
                        player.PersonalStake = stakeSum;
                        _state.HiddenStakerCount--;
                        _state.StakeLimits.Remove(stakerName);

                        _gameActions.SendMessageWithArgs(Messages.PersonFinalStake, i);
                        break;
                    }
                }

                if (_state.HiddenStakerCount == 0)
                {
                    _controller.Stop(StopReason.Decision);
                }

                break;
        }

        if (_state.IsOralNow)
        {
            if (message.Sender == _state.ShowMan.Name)
            {
                if (Logic.CanPlayerAct())
                {
                    _gameActions.SendMessage(Messages.Cancel, stakerName);
                }
            }
            else
            {
                _gameActions.SendMessage(Messages.Cancel, _state.ShowMan.Name);
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

    [Obsolete]
    private void OnStake(Message message, string[] args)
    {
        if (!_state.IsWaiting ||
            _state.Decision != DecisionType.StakeMaking ||
            (_state.ActivePlayer == null || message.Sender != _state.ActivePlayer.Name)
            && (!_state.IsOralNow || message.Sender != _state.ShowMan.Name))
        {
            return;
        }

        if (!int.TryParse(args[1], out var stakeType) || stakeType < 0 || stakeType > 3)
        {
            return;
        }

        _state.StakeType = (StakeMode)stakeType;

        if (!_state.StakeVariants[(int)_state.StakeType])
        {
            _state.StakeType = null;
        }
        else if (_state.StakeType == StakeMode.Sum)
        {
            var minimum = _state.Stake != -1 ? _state.Stake + _state.StakeStep : _state.CurPriceRight + _state.StakeStep;
            
            // TODO: optimize
            while (minimum % _state.StakeStep != 0)
            {
                minimum++;
            }

            if (!int.TryParse(args[2], out var stakeSum))
            {
                _state.StakeType = null;
                return;
            }

            if (stakeSum < minimum || stakeSum > _state.ActivePlayer.Sum || stakeSum % _state.StakeStep != 0)
            {
                _state.StakeType = null;
                return;
            }

            _state.StakeSum = stakeSum;
        }

        if (_state.IsOralNow)
        {
            if (message.Sender == _state.ShowMan.Name)
            {
                if (_state.ActivePlayer != null)
                {
                    _gameActions.SendMessage(Messages.Cancel, _state.ActivePlayer.Name);
                }
            }
            else
            {
                _gameActions.SendMessage(Messages.Cancel, _state.ShowMan.Name);
            }
        }

        _controller.Stop(StopReason.Decision);
    }

    private void OnInfo(string person)
    {
        Inform(person);

        foreach (var item in _state.MainPersons)
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
        var roundIndex = _controller.Engine.RoundIndex;
        _gameActions.InformStageInfo(person, roundIndex);

        if ((_state.InformStages & InformStages.RoundNames) > 0)
        {
            _gameActions.InformRoundsNames(person);
        }

        // To save traffic, do not send round content info on reconnection
        //if ((ClientData.InformStages & InformStages.RoundContent) > 0)
        //{
        //    _gameActions.InformRoundContent(person);
        //}

        if ((_state.InformStages & InformStages.RoundThemesNames) > 0)
        {
            _gameActions.InformRoundThemesNames(person, _state.ThemesPlayMode);
        }

        if ((_state.InformStages & InformStages.RoundThemesComments) > 0)
        {
            _gameActions.InformRoundThemesComments(person);
        }

        if ((_state.InformStages & InformStages.Table) > 0)
        {
            _gameActions.InformTable(person);
        }

        if ((_state.InformStages & InformStages.Theme) > 0)
        {
            _gameActions.InformTheme(person);
        }

        if ((_state.InformStages & InformStages.Question) > 0 && _state.Question != null)
        {
            _gameActions.SendMessageToWithArgs(person, Messages.Choice, _state.ThemeIndex, _state.QuestionIndex, _state.Question.Price);
        }

        if ((_state.InformStages & InformStages.Layout) > 0)
        {
            _gameActions.InformLayout(person);
            // TODO: send already displayed answer options and their state
        }

        if ((_state.InformStages & InformStages.ContentShape) > 0)
        {
            _gameActions.SendContentShape(person);
            // TODO: send already displayed content part
        }

        if (_state.Stage == GameStage.Before && _state.Settings.IsAutomatic)
        {
            var leftTimeBeforeStart = Constants.AutomaticGameStartDuration - (int)(DateTime.UtcNow - _state.TimerStartTime[2]).TotalSeconds * 10;

            if (leftTimeBeforeStart > 0)
            {
                _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.Timer, 2, MessageParams.Timer_Go, leftTimeBeforeStart, -2), person);
            }
        }

        // Send last visual message to restore table state for reconnected player
        if (_state.ComplexVisualState != null)
        {
            foreach (var visualMessageList in _state.ComplexVisualState)
            {
                if (visualMessageList == null)
                {
                    continue;
                }

                foreach (var visualMessage in visualMessageList)
                {
                    _gameActions.SendMessage(visualMessage, person);
                }
            }
        }
        else if (!string.IsNullOrEmpty(_state.LastVisualMessage))
        {
            _gameActions.SendMessage(_state.LastVisualMessage, person);
        }

        if (_state.Stage == GameStage.Round) // TODO: keep all timers state on server and send it to client on reconnection
        {
            _gameActions.SendMessageWithArgs(Messages.Timer, 0, MessageParams.Timer_Go, _state.TimeSettings.Round * 10);
        }

        _gameActions.SendMessageToWithArgs(person, Messages.Pause, _state.TInfo.Pause ? '+' : '-', 0, 0, 0); // TODO: fill time values

        if (_state.Decision == DecisionType.Appellation)
        {
            _gameActions.SendMessageToWithArgs(person, Messages.Appellation, '+');
        }
        else if (_state.Decision == DecisionType.Pressing)
        {
            _gameActions.SendMessageToWithArgs(person, Messages.Try);
        }

        if (_state.ChooserIndex != -1)
        {
            _gameActions.SendMessageToWithArgs(person, Messages.SetChooser, _state.ChooserIndex, "-", "INITIAL");
        }
    }

    private void OnDelete(Message message, string[] args)
    {
        if (!_state.IsWaiting ||
            _state.Decision != DecisionType.ThemeDeleting ||
            _state.ActivePlayer == null ||
            message.Sender != _state.ActivePlayer.Name && (!_state.IsOralNow || message.Sender != _state.ShowMan.Name))
        {
            return;
        }

        if (!int.TryParse(args[1], out int themeIndex) || themeIndex <= -1 || themeIndex >= _state.TInfo.RoundInfo.Count)
        {
            return;
        }

        if (_state.TInfo.RoundInfo[themeIndex].Name == QuestionHelper.InvalidThemeName)
        {
            return;
        }

        _state.ThemeIndexToDelete = themeIndex;

        if (_state.IsOralNow)
        {
            _gameActions.SendMessage(Messages.Cancel, _state.ShowMan.Name);
        }

        if (Logic.CanPlayerAct())
        {
            _gameActions.SendMessage(Messages.Cancel, _state.ActivePlayer.Name);
        }

        _controller.Stop(StopReason.Decision);
    }

    private void OnDisconnect(string[] args)
    {
        if (args.Length < 3 || !_state.AllPersons.TryGetValue(args[1], out var account))
        {
            return;
        }

        var withError = args[2] == "+";

        var res = new StringBuilder()
            .Append(LO[account.IsMale ? nameof(R.Disconnected_Male) : nameof(R.Disconnected_Female)])
            .Append(' ')
            .Append(account.Name);

        _gameActions.SendMessageWithArgs(Messages.Disconnected, account.Name);

        _state.BeginUpdatePersons($"Disconnected {account.Name}");

        try
        {
            account.IsConnected = false;

            if (_state.Viewers.Contains(account))
            {
                _state.Viewers.Remove(account);
            }
            else
            {
                var isBefore = _state.Stage == GameStage.Before;

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
            _state.EndUpdatePersons();
        }

        if (args[1] == _state.HostName)
        {
            // A new host must be assigned if possible.
            // The host is assigned randomly

            SelectNewHost();

            if (_state.Settings.AppSettings.Managed && !_controller.Runner.IsRunning)
            {
                if (_controller.StopReason == StopReason.Pause || _state.TInfo.Pause)
                {
                    _controller.AddHistory("Managed game pause autoremoved");
                    _controller.OnPauseCore(false);
                    return;
                }

                _controller.AddHistory("Managed game move autostarted");

                _state.MoveDirection = MoveDirections.Next;
                _controller.Stop(StopReason.Move);
            }
        }

        OnPersonsChanged(false, withError);
    }

    [Obsolete]
    private void OnConnect(Message message, string[] args)
    {
        if (args.Length < 4)
        {
            _gameActions.SendMessage(SystemMessages.Refuse + Message.ArgsSeparatorChar + LO[nameof(R.WrongConnectionParameters)], message.Sender);
            return;
        }

        var role = args[1] == Constants.Showman ? GameRole.Showman :
                   args[1] == Constants.Player ? GameRole.Player : GameRole.Viewer;
        
        var name = args[2];
        var isMale = args[3] == "m";

        var result = AuthenticateCore(name, isMale, role, null);

        if (result != AuthenticationResult.Ok)
        {
            var msg = new MessageBuilder(SystemMessages.Refuse, GetAuthenticationErrorMessage(result)).Build();
            _gameActions.SendMessage(msg, message.Sender);
            return;
        }

        Master.AuthenticateConnection(message.Sender[1..], name);
        _gameActions.SendMessage(Messages.Accepted, message.Sender);
    }

    private void SelectNewHost()
    {
        static bool canBeHost(ViewerAccount account) => account.IsHuman && account.IsConnected;

        string? newHostName = null;

        if (canBeHost(_state.ShowMan))
        {
            newHostName = _state.ShowMan.Name;
        }
        else
        {
            var availablePlayers = _state.Players.Where(canBeHost).ToArray();

            if (availablePlayers.Length > 0)
            {
                var index = Random.Shared.Next(availablePlayers.Length);
                newHostName = availablePlayers[index].Name;
            }
            else
            {
                var availableViewers = _state.Viewers.Where(canBeHost).ToArray();

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
        _state.HostName = newHostName;
        _gameActions.SendMessageWithArgs(Messages.Hostname, newHostName ?? "", source);
    }

    private void OnAppellation(Message message, string[] args)
    {
        if (_state.QuestionPlay.AppellationState == AppellationState.None)
        {
            return;
        }

        var isAppellationForRightAnswer = args.Length == 1 || args[1] == "+";
        var appellationSource = message.Sender;

        if (isAppellationForRightAnswer)
        {
            var found = false;

            foreach (var answerResult in _state.QuestionHistory)
            {
                if (answerResult.PlayerIndex < 0 || answerResult.PlayerIndex >= _state.Players.Count)
                {
                    continue;
                }

                var player = _state.Players[answerResult.PlayerIndex];

                if (player.Name == appellationSource && !answerResult.IsRight)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                // Appellation for right answer, but no wrong answers found
                return;
            }
        }
        else
        {
            if (_controller.HaveMultipleAnswerers())
            {
                return; // Appellation for wrong answer not allowed when multiple answerers
            }

            var found = false;

            for (var i = 0; i < _state.Players.Count; i++)
            {
                if (_state.Players[i].Name == appellationSource)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                // Appellation for wrong answer, but player not found
                return;
            }
        }

        if (_state.QuestionPlay.Appellations.Any(a => a.Item1 == appellationSource || !a.Item2))
        {
            // Appellation already exists for this source or appellation for wrong answer already given
            return;
        }

        _state.QuestionPlay.Appellations.Add((appellationSource, isAppellationForRightAnswer));

        _gameActions.SendMessageWithArgs(Messages.PlayerAppellating, appellationSource);

        if (_state.QuestionPlay.AppellationState == AppellationState.Collecting)
        {
            return;
        }

        _controller.ProcessNextAppellationRequest(true);
    }

    [Obsolete]
    private void OnCatCost(Message message, string[] args)
    {
        if (!_state.IsWaiting ||
            _state.Decision != DecisionType.QuestionPriceSelection ||
            (_state.Answerer == null || message.Sender != _state.Answerer.Name) &&
            (!_state.IsOralNow || message.Sender != _state.ShowMan.Name))
        {
            return;
        }

        if (int.TryParse(args[1], out var sum)
            && sum >= _state.StakeRange.Minimum
            && sum <= _state.StakeRange.Maximum
            && (_state.StakeRange.Step == 0 || (sum - _state.StakeRange.Minimum) % _state.StakeRange.Step == 0))
        {
            _state.CurPriceRight = sum;
        }

        if (_state.IsOralNow)
        {
            _gameActions.SendMessage(Messages.Cancel, _state.ShowMan.Name);
        }

        if (Logic.CanPlayerAct() && _state.Answerer != null)
        {
            _gameActions.SendMessage(Messages.Cancel, _state.Answerer.Name);
        }

        _controller.Stop(StopReason.Decision);
    }

    private void OnChanged(Message message, string[] args)
    {
        if (message.Sender != _state.ShowMan.Name || args.Length != 3)
        {
            return;
        }

        if (!int.TryParse(args[1], out var playerIndex) ||
            !int.TryParse(args[2], out var sum) ||
            playerIndex < 1 ||
            playerIndex > _state.Players.Count)
        {
            return;
        }

        var player = _state.Players[playerIndex - 1];
        var oldSum = player.Sum;
        player.Sum = sum;

        var verbEnding = _state.ShowMan.IsMale ? "" : LO[nameof(R.FemaleEnding)];

        _gameActions.SendMessageWithArgs(Messages.PlayerScoreChanged, playerIndex - 1, sum);
        _gameActions.InformSums();

        _controller.AddHistory($"Sum change: {playerIndex - 1} = {sum}");
    }

    private void OnMove(Message message, string[] args)
    {
        if (message.Sender != _state.HostName && message.Sender != _state.ShowMan.Name || args.Length <= 1)
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
                if (!_controller.Engine.CanMoveBackRound)
                {
                    return;
                }

                break;

            case MoveDirections.Back:
                if (!_controller.Engine.CanMoveBack)
                {
                    return;
                }

                break;

            case MoveDirections.Next:
                if (_state.MoveNextBlocked)
                {
                    return;
                }

                break;

            case MoveDirections.RoundNext:
                if (!_controller.Engine.CanMoveNextRound)
                {
                    return;
                }

                break;

            case MoveDirections.Round:
                if (!_controller.Engine.CanMoveNextRound && !_controller.Engine.CanMoveBackRound ||
                    _state.Package == null ||
                    args.Length <= 2 ||
                    !int.TryParse(args[2], out int roundIndex) ||
                    roundIndex < 0 ||
                    roundIndex >= _state.Rounds.Length ||
                    _state.Rounds[roundIndex].Index == _controller.Engine.RoundIndex)
                {
                    return;
                }

                _state.TargetRoundIndex = _state.Rounds[roundIndex].Index;
                break;
        }

        // Resume paused game
        if (_state.TInfo.Pause)
        {
            _controller.OnPauseCore(false);

            if (moveDirection == MoveDirections.Next)
            {
                return;
            }
        }

        _controller.AddHistory($"Move started: {moveDirection}");

        _state.MoveDirection = moveDirection;
        _controller.Stop(StopReason.Move);
    }

    private void OnReady(Message message, string[] args)
    {
        if (_state.Stage != GameStage.Before)
        {
            return;
        }

        var res = new StringBuilder();

        // Player or showman is ready to start the game
        res.Append(Messages.Ready).Append(Message.ArgsSeparatorChar);

        var readyAll = true;
        var found = false;
        var toReady = args.Length == 1 || args[1] == "+";

        foreach (var item in _state.MainPersons)
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
        else if (_state.Settings.IsAutomatic)
        {
            if (_state.Players.All(player => player.IsConnected))
            {
                StartGame();
            }
        }
    }

    private void OnPause(Message message, string[] args)
    {
        if (message.Sender != _state.HostName && message.Sender != _state.ShowMan.Name || args.Length <= 1)
        {
            return;
        }

        _controller.OnPauseCore(args[1] == "+");
    }

    private void OnAtom(string[] args)
    {
        if (!_state.QuestionPlay.CollectMediaCompletions)
        {
            return;
        }

        Completion? completion;
        var completions = _state.QuestionPlay.MediaContentCompletions;

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

        if (!_state.IsPlayingMedia || _state.TInfo.Pause)
        {
            return;
        }

        if (completion.Current == completion.Total)
        {
            _state.IsPlayingMedia = false;
            _controller.RescheduleTask();
        }
        else
        {
            // Sometimes someone drops out, and the process gets delayed by 120 seconds. This is unacceptable. We'll give 3 seconds
            _controller.RescheduleTask(30);
        }
    }

    private void OnAnswerVersion(Message message, string[] args)
    {
        if (_state.Decision != DecisionType.Answering || args[1].Length == 0)
        {
            return;
        }

        if (Logic.HaveMultipleAnswerers())
        {
            for (var i = 0; i < _state.Players.Count; i++)
            {
                if (_state.Players[i].Name == message.Sender && _state.QuestionPlay.AnswererIndicies.Contains(i))
                {
                    _state.Players[i].Answer = args[1];
                    return;
                }
            }

            return;
        }

        var answerer = _state.Answerer;
        
        if (!_state.IsWaiting || answerer == null || !answerer.IsHuman || answerer.Name != message.Sender)
        {
            return;
        }

        answerer.Answer = args[1];
    }

    private void OnAnswer(Message message, string[] args)
    {
        if (_state.Decision != DecisionType.Answering)
        {
            return;
        }

        if (Logic.HaveMultipleAnswerers())
        {
            _state.AnswererIndex = -1; // TODO: do not use AnswererIndex here - update player answer directly

            for (var i = 0; i < _state.Players.Count; i++)
            {
                if (_state.Players[i].Name == message.Sender
                    && _state.QuestionPlay.AnswererIndicies.Contains(i)
                    && _state.Players[i].Flag)
                {
                    _state.AnswererIndex = i;
                    _state.Players[i].Flag = false;
                    _gameActions.SendMessageWithArgs(Messages.PersonFinalAnswer, i);
                    _gameActions.SendMessageWithArgs(Messages.PlayerState, PlayerState.HasAnswered, i);

                    if (_state.QuestionPlay.AnswerOptions == null && _state.Players[i].IsHuman && args[1].Length > 0)
                    {
                        var answer = args[1];

                        if (_state.QuestionPlay.Validations.ContainsKey(answer))
                        {
                            break;
                        }

                        _state.QuestionPlay.Validations[answer] = null;

                        _gameActions.SendMessageToWithArgs(
                            _state.ShowMan.Name,
                            Messages.AskValidate,
                            i,
                            answer);
                    }

                    break;
                }
            }

            if (_state.AnswererIndex == -1)
            {
                return;
            }
        }
        else if (!_state.IsWaiting
            || _state.Answerer != null
                && _state.Answerer.Name != message.Sender
                && !(_state.IsOralNow && message.Sender == _state.ShowMan.Name))
        {
            return;
        }

        if (_state.Answerer == null)
        {
            return;
        }

        if (!_state.Answerer.IsHuman)
        {
            var isSure = args.Length > 2 && args[2] == "+";

            if (args[1] == MessageParams.Answer_Right)
            {
                if (_state.QuestionPlay.AnswerOptions != null)
                {
                    var rightLabel = _state.Question.Right.FirstOrDefault();
                    _state.Answerer.Answer = rightLabel;
                }
                else
                {
                    _state.Answerer.Answer = (_state.Question.Right.FirstOrDefault() ?? "(...)").GrowFirstLetter();
                    _state.Answerer.AnswerIsWrong = false;
                }
            }
            else if (_state.QuestionPlay.AnswerOptions != null)
            {
                var rightLabel = _state.Question.Right.FirstOrDefault();
                var wrongOptions = _state.QuestionPlay.AnswerOptions.Where(o => o.Label != rightLabel && !_state.QuestionPlay.UsedAnswerOptions.Contains(o.Label)).ToArray();
                var wrong = wrongOptions[Random.Shared.Next(wrongOptions.Length)];

                _state.Answerer.Answer = wrong.Label;
            }
            else
            {
                _state.Answerer.AnswerIsWrong = true;

                var restwrong = new List<string>();

                foreach (var wrong in _state.Question.Wrong)
                {
                    if (!_state.UsedWrongVersions.Contains(wrong))
                    {
                        restwrong.Add(wrong);
                    }
                }

                if (restwrong.Count == 0 && _state.PackageStatistisProvider != null)
                {
                    var rejectedAnswers = _state.PackageStatistisProvider.GetRejectedAnswers(Logic.Engine.RoundIndex, _state.ThemeIndex, _state.QuestionIndex);

                    foreach (var wrong in rejectedAnswers)
                    {
                        if (!_state.UsedWrongVersions.Contains(wrong))
                        {
                            restwrong.Add(wrong);
                        }
                    }
                }

                var wrongAnswers = LO[nameof(R.WrongAnswer)].Split(';');

                if (restwrong.Count == 0)
                {
                    for (int i = 0; i < wrongAnswers.Length; i++)
                    {
                        if (!_state.UsedWrongVersions.Contains(wrongAnswers[i]))
                        {
                            restwrong.Add(wrongAnswers[i]);
                        }
                    }

                    if (!_state.UsedWrongVersions.Contains(LO[nameof(R.NoAnswer)]))
                    {
                        restwrong.Add(LO[nameof(R.NoAnswer)]);
                    }
                }

                var wrongCount = restwrong.Count;

                if (wrongCount == 0)
                {
                    restwrong.Add(wrongAnswers[0]);
                    wrongCount = 1;
                }

                var wrongIndex = Random.Shared.Next(wrongCount);

                if (!Logic.HaveMultipleAnswerers())
                {
                    _state.UsedWrongVersions.Add(restwrong[wrongIndex]);
                }

                _state.Answerer.Answer = restwrong[wrongIndex].GrowFirstLetter();
            }
        }
        else
        {
            if (args[1].Length > 0)
            {
                _state.Answerer.Answer = args[1];
                _state.Answerer.AnswerIsWrong = false;
            }
            else
            {
                _state.Answerer.Answer = LO[nameof(R.IDontKnow)];
                _state.Answerer.AnswerIsWrong = true;
            }

            if (_state.IsOralNow)
            {
                if (message.Sender == _state.ShowMan.Name)
                {
                    if (_state.Answerer != null)
                    {
                        _gameActions.SendMessage(Messages.Cancel, _state.Answerer.Name);
                    }
                }
                else
                {
                    _gameActions.SendMessage(Messages.Cancel, _state.ShowMan.Name);
                }
            }
        }

        _state.AnswerCount--;

        if (_state.AnswerCount == 0)
        {
            _controller.Stop(StopReason.Decision);
        }
    }

    private void OnIsRight(Message message, string[] args)
    {
        if (!_state.IsWaiting || args.Length <= 1)
        {
            return;
        }

        if ((_state.Decision == DecisionType.AnswerValidating || _state.IsOralNow && _state.Decision == DecisionType.Answering) &&
            _state.ShowMan != null &&
            message.Sender == _state.ShowMan.Name &&
            _state.Answerer != null)
        {
            _state.Decision = DecisionType.AnswerValidating;
            _state.Answerer.AnswerIsRight = args[1] == "+";
            _state.Answerer.AnswerValidationFactor = args.Length > 2 && double.TryParse(args[2], out var factor) && factor >= 0.0 ? factor : 1.0;
            _state.ShowmanDecision = true;

            if (_state.Answerer != null
                && _state.IsOralNow
                && (_state.QuestionPlay.AnswerOptions == null || !_state.Settings.AppSettings.OralPlayersActions))
            {
                // Cancelling Oral Answer player mode
                _gameActions.SendMessage(Messages.Cancel, _state.Answerer.Name);
            }

            _controller.Stop(StopReason.Decision);
            return;
        }

        if (_state.Decision == DecisionType.Appellation)
        {
            for (var i = 0; i < _state.Players.Count; i++)
            {
                if (_state.Players[i].AppellationFlag && _state.Players[i].Name == message.Sender)
                {
                    if (args[1] == "+")
                    {
                        _state.AppellationPositiveVoteCount++;
                    }
                    else
                    {
                        _state.AppellationNegativeVoteCount++;
                    }

                    _state.Players[i].AppellationFlag = false;
                    _state.AppellationAwaitedVoteCount--;
                    _gameActions.SendMessageWithArgs(Messages.PersonApellated, i);
                    _gameActions.SendMessageWithArgs(Messages.PlayerState, PlayerState.HasAnswered, i);
                    break;
                }
            }

            if (_state.AppellationAwaitedVoteCount == 0)
            {
                _controller.Stop(StopReason.Decision);
            }

            var halfVoteCount = _state.AppellationTotalVoteCount / 2;

            if (_state.AppellationPositiveVoteCount > halfVoteCount || _state.AppellationNegativeVoteCount > halfVoteCount)
            {
                SendAppellationCancellationsToActivePlayers();
                _controller.Stop(StopReason.Decision);
            }
        }
    }

    private void SendAppellationCancellationsToActivePlayers()
    {
        foreach (var player in _state.Players)
        {
            if (player.AppellationFlag)
            {
                _gameActions.SendMessage(Messages.Cancel, player.Name);
            }
        }
    }

    private void OnPass(Message message)
    {
        if (!_state.IsQuestionAskPlaying) // TODO: this condition looks ugly and unnecessary
        {
            return;
        }

        var nextTask = Logic.Runner.PendingTask;

        if (nextTask == Tasks.AskStake
            || _state.Decision == DecisionType.StakeMaking
            || _state.Decision == DecisionType.NextPersonStakeMaking)
        {
            // Sending pass in stakes in advance
            if (_state.Decision == DecisionType.StakeMaking && _state.ActivePlayer?.Name == message.Sender)
            {
                return; // Currently making stake player can pass with the standard button
            }

            // Stake making pass
            for (var i = 0; i < _state.Players.Count; i++)
            {
                var player = _state.Players[i];

                if (player.Name == message.Sender)
                {
                    if (player.StakeMaking && i != _state.Stakes.StakerIndex) // Current stakes winner cannot pass
                    {
                        player.StakeMaking = false;
                        
                        _state.OrderHistory.Append("Player passed on stakes making: ")
                            .Append(i)
                            .Append(' ')
                            .Append(_state.ActivePlayer?.StakeMaking)
                            .AppendLine();
                        
                        // TODO: leave only one pass message
                        _gameActions.SendMessageWithArgs(Messages.Pass, i);
                        _gameActions.SendMessageWithArgs(Messages.PersonStake, i, 2);

                        if (_state.ActivePlayer != null
                            && _state.ActivePlayer.StakeMaking
                            && (nextTask == Tasks.AskStake || _state.Decision == DecisionType.NextPersonStakeMaking))
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
        for (var i = 0; i < _state.Players.Count; i++)
        {
            var player = _state.Players[i];

            if (player.Name == message.Sender && player.CanPress)
            {
                player.CanPress = false;
                _gameActions.SendMessageWithArgs(Messages.Pass, i);
                canPressChanged = true;
                break;
            }
        }

        if (canPressChanged
            && _state.Players.All(p => !p.CanPress || !p.IsConnected)
            && (_state.Decision == DecisionType.None || _state.Decision == DecisionType.Pressing) // TODO: Can this state be described in a more clear way?
            && !_state.TInfo.Pause
            && !_state.QuestionPlay.IsAnswer)
        {
            _controller.MoveToAnswer();
            _controller.ScheduleExecution(Tasks.WaitTry, 1, force: true);
        }
    }

    /// <summary>
    /// Handles player button press.
    /// </summary>
    /// <param name="playerName">Pressed player name.</param>
    /// <param name="pressDurationMs">Player reaction time.</param>
    private void OnI(string playerName, int pressDurationMs)
    {
        if (_state.TInfo.Pause)
        {
            return;
        }

        if (_state.Decision != DecisionType.Pressing)
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

        var buttonPressMode = _state.Settings.AppSettings.ButtonPressMode;

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
        _state.PendingAnswererIndex = answererIndex;

        if (_controller.Stop(StopReason.Answer))
        {
            _state.Decision = DecisionType.None;
        }
    }

    private void ProcessAnswerer_RandomWithinInterval(int answererIndex)
    {
        if (!_state.PendingAnswererIndicies.Contains(answererIndex))
        {
            _state.PendingAnswererIndicies.Add(answererIndex);
        }

        if (!_state.IsDeferringAnswer)
        {            
            _state.WaitInterval = (int)(_state.Host.Options.ButtonsAcceptInterval.TotalMilliseconds / 100);
            _controller.Stop(StopReason.Wait);
        }
    }

    private void ProcessAnswerer_FirstWinsClient(int answererIndex, int pressDurationMs)
    {
        if (_state.PendingAnswererIndex == -1 || pressDurationMs > 0 && pressDurationMs < _state.AnswererPressDuration)
        {
            _state.PendingAnswererIndex = answererIndex;
            _state.AnswererPressDuration = pressDurationMs > 0 ? pressDurationMs : (int)_state.Host.Options.ButtonsAcceptInterval.TotalMilliseconds;
        }
        else
        {
            _state.PendingAnswererIndicies.Add(answererIndex);
        }

        if (!_state.IsDeferringAnswer)
        {
            _state.WaitInterval = (int)(_state.Host.Options.ButtonsAcceptInterval.TotalMilliseconds / 100);
            _controller.Stop(StopReason.Wait);
        }
    }

    private int DetectAnswererIndex(string playerName)
    {
        var answererIndex = -1;
        var blockingButtonTime = _state.TimeSettings.ButtonBlocking;

        for (var i = 0; i < _state.Players.Count; i++)
        {
            var player = _state.Players[i];

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
        for (var i = 0; i < _state.Players.Count; i++)
        {
            var player = _state.Players[i];

            if (player.Name == playerName)
            {
                if (_state.Answerer != player)
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

    private void OnDisconnectRequested(string person) => DisconnectRequested?.Invoke(this, person);

    /// <summary>
    /// Updates game configuration.
    /// </summary>
    private void OnConfig(Message message, string[] args)
    {
        if (message.Sender != _state.HostName || args.Length <= 1)
        {
            return;
        }

        if (_state.HostName == null || !_state.AllPersons.TryGetValue(_state.HostName, out var host))
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
                SetPerson(message, args, host);
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
        if (_state.Players.Count >= Constants.MaxPlayers)
        {
            return;
        }

        var newAccount = new ViewerAccount(Constants.FreePlace, false, false) { IsHuman = true };

        _state.BeginUpdatePersons("AddTable " + message.Text);

        try
        {
            _state.Players.Add(new GamePlayerAccount(newAccount));
            Logic.AddHistory($"Player added (total: {_state.Players.Count})");
        }
        finally
        {
            _state.EndUpdatePersons();
        }

        var info = new StringBuilder(Messages.Config).Append(Message.ArgsSeparatorChar)
            .Append(MessageParams.Config_AddTable).Append(Message.ArgsSeparatorChar);

        AppendAccountExt(newAccount, info);

        _gameActions.SendMessage(info.ToString());
        OnPersonsChanged();
    }

    private void DeleteTable(Message message, string[] args, Account host)
    {
        if (args.Length <= 2)
        {
            return;
        }

        var indexStr = args[2];

        if (_state.Players.Count <= 2
            || !int.TryParse(indexStr, out int index)
            || index <= -1
            || index >= _state.Players.Count)
        {
            return;
        }

        var account = _state.Players[index];
        var isOnline = account.IsConnected;

        if (_state.Stage != GameStage.Before && account.IsHuman && isOnline && !account.IsMoveable)
        {
            return;
        }

        _state.BeginUpdatePersons("DeleteTable " + message.Text);

        try
        {
            _state.Players.RemoveAt(index);
            Logic.AddHistory($"Player removed at {index}; AnswererIndex: {_state.AnswererIndex}");

            try
            {
                DropPlayerIndex(index);
            }
            catch (Exception exc)
            {
                throw new InvalidOperationException(
                    $"DropPlayerIndex error. Persons history: {_state.PersonsUpdateHistory}; logic history: {Logic.PrintHistory()}; stake history: {_state.OrderHistory}",
                    exc);
            }

            if (isOnline && account.IsHuman)
            {
                _state.Viewers.Add(account);
            }
        }
        finally
        {
            _state.EndUpdatePersons();
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

        if (_state.Stage == GameStage.Before)
        {
            var readyAll = _state.MainPersons.All(p => p.Ready);

            if (readyAll)
            {
                StartGame();
            }
        }

        OnPersonsChanged();
    }

    /// <summary>
    /// Correctly removes player from the game adjusting game state.
    /// </summary>
    /// <param name="playerIndex">Index of the player to remove.</param>
    private void DropPlayerIndex(int playerIndex)
    {
        if (_state.ChooserIndex > playerIndex)
        {
            _state.ChooserIndex--;
        }
        else if (_state.ChooserIndex == playerIndex)
        {
            DropCurrentChooser();
        }

        _state.QuestionPlay.RemovePlayer(playerIndex);

        if (_state.AnswererIndex > playerIndex)
        {
            _state.AnswererIndex--;
            Logic.AddHistory($"AnswererIndex reduced to {_state.AnswererIndex}");
        }
        else if (_state.AnswererIndex == playerIndex)
        {
            DropCurrentAnswerer();
        }

        if (_state.AppelaerIndex > playerIndex)
        {
            _state.AppelaerIndex--;
        }
        else if (_state.AppelaerIndex == playerIndex)
        {
            DropCurrentAppelaer();
        }

        if (Logic.Stakes.HandlePlayerDrop(playerIndex))
        {
            Logic.AddHistory($"StakerIndex set to {_state.Stakes.StakerIndex}");
        }

        // TODO: move to stakes.HandlePlayerDrop()
        if (_state.Question != null
            && _state.QuestionTypeName == QuestionTypes.Stake
            && _state.Order.Length > 0)
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

        if (!_state.IsWaiting)
        {
            return;
        }

        switch (_state.Decision)
        {
            case DecisionType.StarterChoosing:
                // Asking again
                _gameActions.SendMessage(Messages.Cancel, _state.ShowMan.Name);
                _controller.StopWaiting();
                _controller.PlanExecution(Tasks.AskFirst, 20);
                break;
        }
    }

    private void DropPlayerFromThemeDeleters(int playerIndex)
    {
        var themeDeleters = _state.ThemeDeleters;

        if (themeDeleters == null)
        {
            return;
        }

        Logic.AddHistory($"ThemeDeleters before remove: {themeDeleters}");
        themeDeleters.RemoveAt(playerIndex);
        Logic.AddHistory($"ThemeDeleters removed: {playerIndex}; result: {themeDeleters}");

        if (!_state.IsWaiting)
        {
            return;
        }

        if (_state.Decision == DecisionType.NextPersonFinalThemeDeleting)
        {
            if (themeDeleters.IsEmpty())
            {
                _controller.StopWaiting();
                _state.MoveDirection = MoveDirections.RoundNext;
                _controller.Stop(StopReason.Move);
            }
            else
            {
                var indicies = themeDeleters.Current.PossibleIndicies;
                var hasAnyFlag = false;

                for (var i = 0; i < _state.Players.Count; i++)
                {
                    _state.Players[i].Flag = indicies.Contains(i);
                    hasAnyFlag = true;
                }

                if (!hasAnyFlag)
                {
                    _controller.PlanExecution(Tasks.AskToDelete, 10);
                }
            }
        }
        else if (_state.Decision == DecisionType.ThemeDeleting)
        {
            if (themeDeleters.IsEmpty())
            {
                _controller.StopWaiting();
                _state.MoveDirection = MoveDirections.RoundNext;
                _controller.Stop(StopReason.Move);
            }
        }
    }

    private void DropPlayerFromQuestionHistory(int playerIndex)
    {
        var newHistory = new List<AnswerResult>();

        for (var i = 0; i < _state.QuestionHistory.Count; i++)
        {
            var answerResult = _state.QuestionHistory[i];

            if (answerResult.PlayerIndex == playerIndex)
            {
                continue;
            }

            var newPlayerIndex = answerResult.PlayerIndex - (answerResult.PlayerIndex > playerIndex ? 1 : 0);
            newHistory.Add(new AnswerResult(newPlayerIndex, answerResult.IsRight, answerResult.Sum));
        }

        _state.QuestionHistory.Clear();
        _state.QuestionHistory.AddRange(newHistory);
    }

    private void ValidatePlayers()
    {
        var playersAreValid = _state.PlayersValidator == null || _state.PlayersValidator();

        if (playersAreValid)
        {
            return;
        }

        _controller.StopWaiting();
        _state.MoveDirection = MoveDirections.RoundNext;
        _controller.Stop(StopReason.Move);
    }

    private void DropPlayerFromStakes(int playerIndex)
    {
        var currentOrder = _state.Order;
        var currentOrderIndex = _state.OrderIndex;
        var currentStaker = currentOrderIndex == -1 ? -1 : currentOrder[currentOrderIndex];

        _state.OrderHistory
            .Append("DropPlayerFromStakes. Before ")
            .Append(playerIndex)
            .Append(' ')
            .Append(string.Join(",", currentOrder))
            .AppendFormat(" {0}", _state.OrderIndex)
            .AppendLine();

        var newOrder = new int[_state.Players.Count];

        for (int i = 0, j = 0; i < currentOrder.Length; i++)
        {
            if (currentOrder[i] == playerIndex)
            {
                if (_state.OrderIndex >= i)
                {
                    _state.OrderIndex--; // -1 - OK
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

        if (_state.OrderIndex == currentOrder.Length - 1)
        {
            _state.OrderIndex = newOrder.Length - 1;
        }

        _state.Order = newOrder;

        _state.OrderHistory
            .Append("DropPlayerFromStakes. After ")
            .Append(string.Join(",", newOrder))
            .AppendFormat(" {0}", _state.OrderIndex)
            .AppendLine();

        if (!_state.Players.Any(p => p.StakeMaking))
        {
            Logic.AddHistory("Last staker dropped");
            _state.SkipQuestion?.Invoke();
            Logic.PlanExecution(Tasks.MoveNext, 20, 1);
        }
        else if (currentOrderIndex != -1 && _state.OrderIndex == -1
            || currentStaker != -1 && _state.Order[_state.OrderIndex] == -1)
        {
            Logic.AddHistory("Current staker dropped");

            if (_state.Decision == DecisionType.StakeMaking || _state.Decision == DecisionType.NextPersonStakeMaking)
            {
                // Staker has been deleted. We need to move game further
                if (_state.IsOralNow || _state.Decision == DecisionType.NextPersonStakeMaking)
                {
                    _gameActions.SendMessage(Messages.Cancel, _state.ShowMan.Name);
                }

                ContinueMakingStakes();
            }
        }
        else if (_state.Decision == DecisionType.NextPersonStakeMaking)
        {
            _gameActions.SendMessage(Messages.Cancel, _state.ShowMan.Name);
            ContinueMakingStakes();
        }
    }

    private void DropCurrentChooser()
    {
        // Give turn to player with least score
        // TODO: MinBy in .NET 7
        var minSum = _state.Players.Min(p => p.Sum);
        _state.ChooserIndex = _state.Players.TakeWhile(p => p.Sum != minSum).Count();
    }

    private void DropCurrentAppelaer()
    {
        _state.AppelaerIndex = -1;
        Logic.AddHistory($"AppelaerIndex dropped");
    }

    /// <summary>
    /// Correctly removes current answerer from the game.
    /// </summary>
    private void DropCurrentAnswerer()
    {
        // Drop answerer index
        _state.AnswererIndex = -1;

        if (!_state.IsQuestionAskPlaying)
        {
            return;
        }

        var nextTask = Logic.Runner.PendingTask;

        Logic.AddHistory(
            $"AnswererIndex dropped; nextTask = {nextTask};" +
            $" ClientData.Decision = {_state.Decision}");

        if ((_state.Decision == DecisionType.Answering ||
            _state.Decision == DecisionType.AnswerValidating) && !Logic.HaveMultipleAnswerers())
        {
            // Answerer has been dropped. The game should be moved forward
            Logic.StopWaiting();

            if (_state.IsOralNow)
            {
                _gameActions.SendMessage(Messages.Cancel, _state.ShowMan.Name);
            }

            Logic.PlanExecution(Tasks.ContinueQuestion, 1);
        }
        else if (nextTask == Tasks.AskRight || nextTask == Tasks.WaitRight)
        {
            // Player has been removed after giving answer. But the answer has not been validated by showman yet
            if (_state.QuestionPlay.AnswererIndicies.Count == 0)
            {
                Logic.PlanExecution(Tasks.ContinueQuestion, 1);
            }
            else
            {
                Logic.PlanExecution(Tasks.Announce, 15);
            }
        }
        else if (_state.QuestionPlay.AnswererIndicies.Count == 0 && !_state.QuestionPlay.UseButtons)
        {
            _state.SkipQuestion?.Invoke();
            Logic.PlanExecution(Tasks.MoveNext, 20, 1);
        }
        else if (nextTask == Tasks.AnnounceStake)
        {
            Logic.PlanExecution(Tasks.Announce, 15);
        }
    }

    private void DropPlayerFromAnnouncing(int index)
    {
        if (_state.AnnouncedAnswerersEnumerator == null)
        {
            return;
        }

        Logic.AddHistory($"AnnouncedAnswerersEnumerator before update: {_state.AnnouncedAnswerersEnumerator}");
        _state.AnnouncedAnswerersEnumerator.Update(CustomEnumeratorUpdaters.RemoveByIndex(index));
        Logic.AddHistory($"AnnouncedAnswerersEnumerator after update: {_state.AnnouncedAnswerersEnumerator}");
    }

    private void ContinueMakingStakes()
    {
        Logic.AddHistory("ContinueMakingStakes");

        var previousDecision = _state.Decision;
        Logic.StopWaiting(); // Drops ClientData.Decision

        if (Logic.TryDetectStakesWinner())
        {
            if (_state.Stake == -1)
            {
                _state.Stake = _state.CurPriceRight;
            }

            return;
        }

        if (_state.OrderIndex > -1 && previousDecision == DecisionType.NextPersonStakeMaking)
        {
            Logic.AddHistory("Rolling order index back");
            _state.OrderIndex--;
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

            if (!int.TryParse(indexStr, out index) || index < 0 || index >= _state.Players.Count)
            {
                return;
            }

            account = _state.Players[index];
        }
        else
        {
            account = _state.ShowMan;
        }

        if (!account.IsConnected || !account.IsHuman || _state.Stage != GameStage.Before && !account.IsMoveable)
        {
            return;
        }

        var newAccount = new Account { IsHuman = true, Name = Constants.FreePlace };

        _state.BeginUpdatePersons("FreeTable " + message.Text);

        try
        {
            if (isPlayer)
            {
                _state.Players[index] = new GamePlayerAccount(newAccount);
                InheritAccountState(_state.Players[index], account);
            }
            else
            {
                _state.ShowMan = new GamePersonAccount(newAccount);
            }

            account.Ready = false;

            _state.Viewers.Add(account);
        }
        finally
        {
            _state.EndUpdatePersons();
        }

        foreach (var item in _state.MainPersons)
        {
            if (item.Ready)
            {
                _gameActions.SendMessage($"{Messages.Ready}\n{item.Name}", message.Sender);
            }
        }

        _gameActions.SendMessageWithArgs(Messages.Config, MessageParams.Config_Free, args[2], args[3]);

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

    private void SetPerson(Message message, string[] args, Account host)
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

            if (!int.TryParse(indexStr, out index) || index < 0 || index >= _state.Players.Count)
            {
                return;
            }

            account = _state.Players[index];
        }
        else
        {
            account = _state.ShowMan;
        }

        if (_state.Stage != GameStage.Before && account.IsConnected && !account.IsMoveable)
        {
            return;
        }

        var oldName = account.Name;
        GamePersonAccount? newAccount;

        if (!account.IsHuman)
        {
            if (_state.AllPersons.ContainsKey(replacer))
            {
                _gameActions.SendMessageToWithArgs(message.Sender, Messages.UserError, ErrorCode.PersonAlreadyExists);
                return;
            }

            _state.BeginUpdatePersons($"SetComputerPerson {account.Name} {account.IsConnected} {replacer} {index}");

            try
            {
                newAccount = isPlayer
                    ? ReplaceComputerPlayer(index, account.Name, replacer)
                    : ReplaceComputerShowman(account.Name, replacer);

                if (newAccount is GamePlayerAccount playerAccount)
                {
                    InheritAccountState(playerAccount, account);
                }
            }
            finally
            {
                _state.EndUpdatePersons();
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

        _state.BeginUpdatePersons($"SetHumanPerson {account.Name} {account.IsConnected} {replacer} {index}");

        try
        {
            if (_state.ShowMan.Name == replacer && _state.ShowMan.IsHuman)
            {
                otherAccount = _state.ShowMan;

                if (_state.Stage != GameStage.Before && otherAccount.IsConnected && !otherAccount.IsMoveable)
                {
                    return false;
                }

                _state.ShowMan = new GamePersonAccount(account)
                {
                    Ready = account.Ready,
                    IsConnected = account.IsConnected,
                    IsMoveable = account.IsMoveable
                };

                Logic.SendQuestionAnswersToShowman();
            }
            else
            {
                for (var i = 0; i < _state.Players.Count; i++)
                {
                    if (_state.Players[i].Name == replacer && _state.Players[i].IsHuman)
                    {
                        otherAccount = _state.Players[i];

                        if (_state.Stage != GameStage.Before && otherAccount.IsConnected && !otherAccount.IsMoveable)
                        {
                            return false;
                        }

                        _state.Players[i] = new GamePlayerAccount(account)
                        {
                            Ready = account.Ready,
                            IsConnected = account.IsConnected,
                            IsMoveable = account.IsMoveable,
                            Flag = _state.Players[i].Flag,
                            Sum = _state.Players[i].Sum
                        };

                        InheritAccountState(_state.Players[i], otherAccount);

                        otherIndex = i;
                        break;
                    }
                }

                if (otherIndex == -1)
                {
                    for (var i = 0; i < _state.Viewers.Count; i++)
                    {
                        if (_state.Viewers[i].Name == replacer) // always IsHuman
                        {
                            otherAccount = _state.Viewers[i];
                            otherIndex = i;

                            if (_state.Stage != GameStage.Before && otherAccount.IsConnected && !otherAccount.IsMoveable)
                            {
                                return false;
                            }

                            if (account.IsConnected)
                            {
                                _state.Viewers[i] = new ViewerAccount(account) { IsConnected = true };
                            }
                            else
                            {
                                _state.Viewers.RemoveAt(i);
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
                var previousPlayer = _state.Players[index];

                _state.Players[index] = new GamePlayerAccount(otherAccount)
                {
                    IsConnected = otherAccount.IsConnected,
                    IsMoveable = otherAccount.IsMoveable,
                    Sum = previousPlayer.Sum
                };

                InheritAccountState(_state.Players[index], previousPlayer);

                if (otherPerson != null)
                {
                    _state.Players[index].Ready = otherPerson.Ready;
                }
            }
            else
            {
                _state.ShowMan = new GamePersonAccount(otherAccount)
                {
                    IsConnected = otherAccount.IsConnected,
                    IsMoveable = otherAccount.IsMoveable
                };

                if (otherPerson != null)
                {
                    _state.ShowMan.Ready = otherPerson.Ready;
                }

                Logic.SendQuestionAnswersToShowman();
            }

            InformAvatar(otherAccount);
            return true;
        }
        finally
        {
            _state.EndUpdatePersons();
        }
    }

    internal void ChangePersonType(string personType, string indexStr, ViewerAccount? responsePerson)
    {
        GamePersonAccount account;
        int index = -1;

        var isPlayer = personType == Constants.Player;

        if (isPlayer)
        {
            if (!int.TryParse(indexStr, out index) || index < 0 || index >= _state.Players.Count)
            {
                return;
            }

            account = _state.Players[index];
        }
        else
        {
            account = _state.ShowMan;
        }

        if (account == null)
        {
            _state.Host.LogWarning("ChangePersonType: account == null");
            return;
        }

        if (_state.Stage != GameStage.Before && account.IsConnected && !account.IsMoveable)
        {
            return;
        }

        var oldName = account.Name;

        var newType = !account.IsHuman;
        string newName = "";
        bool newIsMale = true;

        ViewerAccount? newAcc = null;

        _state.BeginUpdatePersons($"ChangePersonType {personType} {indexStr}");

        try
        {
            if (account.IsConnected && account.IsHuman)
            {
                _state.Viewers.Add(account);
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
                    _state.IsOral = _state.Settings.AppSettings.Oral && _state.ShowMan.IsHuman;
                }
            }
            else if (isPlayer)
            {
                if (_defaultPlayers == null)
                {
                    return;
                }

                var visited = new List<int>();

                for (var i = 0; i < _state.Players.Count; i++)
                {
                    if (i != index && _state.Players[i].IsConnected)
                    {
                        for (var j = 0; j < _defaultPlayers.Length; j++)
                        {
                            if (_defaultPlayers[j].Name == _state.Players[i].Name)
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
                var newPlayerAccount = CreateNewComputerPlayer(index, compPlayer);
                InheritAccountState(newPlayerAccount, account);
                newAcc = newPlayerAccount;
                newName = newAcc.Name;
                newIsMale = newAcc.IsMale;
            }
            else
            {
                var showman = new ComputerAccount(_defaultShowmans[0]);
                var name = showman.Name;
                var nameIndex = 0;

                while (nameIndex < Constants.MaxPlayers && _state.AllPersons.ContainsKey(name))
                {
                    name = $"{showman.Name} {nameIndex++}";
                }

                showman.Name = name;

                newAcc = CreateNewComputerShowman(showman);
                newName = newAcc.Name;
                newIsMale = newAcc.IsMale;

                _state.IsOral = _state.Settings.AppSettings.Oral && _state.ShowMan.IsHuman;
            }
        }
        finally
        {
            _state.EndUpdatePersons();
        }

        foreach (var item in _state.MainPersons)
        {
            if (item.Ready)
            {
                _gameActions.SendMessage($"{Messages.Ready}\n{item.Name}");
            }
        }

        _gameActions.SendMessageWithArgs(Messages.Config, MessageParams.Config_ChangeType, personType, index, newType ? '+' : '-', newName, newIsMale ? '+' : '-');

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

        _state.Players[index] = newAccount;

        var playerClient = Network.Clients.Client.Create(newAccount.Name, _client.Node);
        var data = new ViewerData();
        var actions = new ViewerActions(playerClient);
        var logic = new ViewerComputerLogic(data, actions, new Intelligence(account), GameRole.Player);
        _ = new Player(playerClient, account, logic, actions, data);

        OnInfo(newAccount.Name);

        return newAccount;
    }

    private GamePersonAccount CreateNewComputerShowman(ComputerAccount account)
    {
        if (_state.Host == null)
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

        _state.ShowMan = newAccount;

        var showmanClient = Network.Clients.Client.Create(newAccount.Name, _client.Node);
        var data = new ViewerData();
        var actions = new ViewerActions(showmanClient);
        
        var logic = new ViewerComputerLogic(
            data,
            actions,
            new Intelligence(account),
            GameRole.Showman);
        
        var showman = new Showman(showmanClient, account, logic, actions, data);

        OnInfo(newAccount.Name);
        Logic.SendQuestionAnswersToShowman();

        return newAccount;
    }

    internal void StartGame()
    {
        _state.Stage = GameStage.Begin;
        _state.GameResultInfo.StartTime = DateTimeOffset.UtcNow;

        _controller.OnStageChanged(GameStages.Started, LO[nameof(R.GameBeginning)]);
        _gameActions.InformStage();

        _state.IsOral = _state.Settings.AppSettings.Oral && _state.ShowMan.IsHuman;

        _controller.ScheduleExecution(Tasks.StartGame, 1, 1);
    }

    private bool? TryAuthenticateAccount(
        GameRole role,
        string name,
        bool isMale,
        int index,
        ViewerAccount account)
    {
        if (account.IsConnected)
        {
            return account.Name == name ? false : null;
        }

        _state.BeginUpdatePersons($"Connected {name} as {role} at {index}");

        try
        {
            var append = role == GameRole.Viewer && account.Name == Constants.FreePlace;

            account.Name = name;
            account.IsMale = isMale;
            account.Picture = "";
            account.IsConnected = true;

            if (append)
            {
                _state.Viewers.Add(new ViewerAccount(account) { IsConnected = account.IsConnected });
            }
        }
        finally
        {
            _state.EndUpdatePersons();
        }

        var connectedMessage = LO[account.IsMale ? nameof(R.Connected_Male) : nameof(R.Connected_Female)];
        _gameActions.SendMessageWithArgs(Messages.Connected, role.ToString().ToLowerInvariant(), index, name, isMale ? 'm' : 'f', "");

        if (_state.HostName == null && !_state.Settings.IsAutomatic)
        {
            UpdateHostName(name);
        }

        if (role == GameRole.Showman)
        {
            Logic.SendQuestionAnswersToShowman();
        }

        OnPersonsChanged();

        return true;
    }

    private void OnPersonsChanged(bool joined = true, bool withError = false) => PersonsChanged?.Invoke(this, joined, withError);

    private void InformAvatar(ViewerAccount account)
    {
        foreach (var personName in _state.AllPersons.Keys)
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
                if (!_state.Host.AreCustomAvatarsSupported)
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
        for (var i = 0; i < _state.Players.Count; i++)
        {
            if (!_state.Players[i].IsConnected)
            {
                ChangePersonType(Constants.Player, i.ToString(), null);
            }
        }

        StartGame();
    }
}
