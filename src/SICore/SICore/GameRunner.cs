﻿using SICore.Clients.Game;
using SICore.Contracts;
using SICore.Network;
using SICore.Network.Clients;
using SICore.Network.Servers;
using SICore.Services;
using SIData;
using SIEngine;
using SIPackages;

namespace SICore;

/// <summary>
/// Creates and runs new game.
/// </summary>
public sealed class GameRunner
{
    private static EngineOptions CreateEngineOptions(AppSettingsCore appSettingsCore) => new()
    {
        IsMultimediaPressMode = appSettingsCore.FalseStart,
        IsPressMode = appSettingsCore.FalseStart,
        ShowRight = true,
        PlaySpecials = true,
        PlayAllQuestionsInFinalRound = appSettingsCore.PlayAllQuestionsInFinalRound,
    };

    private readonly Node _node;
    private readonly IGameSettingsCore<AppSettingsCore> _settings;
    private readonly SIDocument _document;
    private readonly IGameHost _gameHost;
    private readonly IFileShare _fileShare;
    private readonly ComputerAccount[] _defaultPlayers;
    private readonly ComputerAccount[] _defaultShowmans;
    private readonly string? _documentPath;
    private readonly IAvatarHelper _avatarHelper;
    private readonly IPinHelper? _pinHelper;
    private readonly string? _gameName;
    private readonly bool _createHost;

    public GameRunner(
        Node node,
        IGameSettingsCore<AppSettingsCore> settings,
        SIDocument document,
        IGameHost gameHost,
        IFileShare fileShare,
        ComputerAccount[] defaultPlayers,
        ComputerAccount[] defaultShowmans,
        string? documentPath,
        IAvatarHelper avatarHelper,
        IPinHelper? pinHelper,
        string? gameName = null,
        bool createHost = true)
    {
        _node = node;
        _settings = settings;
        _document = document;
        _gameHost = gameHost;
        _fileShare = fileShare;
        _defaultPlayers = defaultPlayers;
        _defaultShowmans = defaultShowmans;
        _documentPath = documentPath;
        _avatarHelper = avatarHelper;
        _pinHelper = pinHelper;
        _gameName = gameName;
        _createHost = createHost;
    }

    // TODO: remove hostLogicFactory; attach host after game creation
    public (IViewerClient?, Game) Run(Func<ViewerData, ViewerActions, Localizer, IPersonController> hostLogicFactory)
    {
        var gameData = new GameData(_gameHost, new GamePersonAccount(_settings.Showman))
        {
            Settings = _settings,
            HostName = _settings.IsAutomatic ? null : _settings.HumanPlayerName,
            GameName = _gameName ?? "",
        };

        var localizer = new Localizer(_settings.AppSettings.Culture);

        var isHost = _createHost && gameData.HostName == _settings.Showman.Name;
        IViewerClient? host = null;

        gameData.BeginUpdatePersons("Start");

        try
        {
            if (!_settings.Showman.IsHuman || isHost)
            {
                var showmanClient = new Client(_settings.Showman.Name);
                var data = new ViewerData(_gameHost);
                var actions = new ViewerActions(showmanClient);

                var logic = isHost
                    ? hostLogicFactory(data, actions, localizer)
                    : new ViewerComputerLogic(
                        data,
                        actions,
                        new Intelligence((ComputerAccount)_settings.Showman),
                        GameRole.Showman);
                
                var showman = new Showman(showmanClient, _settings.Showman, isHost, logic, actions, localizer, data);
                showmanClient.ConnectTo(_node);

                if (isHost)
                {
                    host = showman;
                }

                gameData.ShowMan.IsConnected = true;
            }

            for (int i = 0; i < _settings.Players.Length; i++)
            {
                gameData.Players.Add(new GamePlayerAccount(_settings.Players[i]));
                var name = _settings.Players[i].Name;
                var human = _settings.Players[i].IsHuman;
                isHost = _createHost && gameData.HostName == name;

                if (!human || isHost)
                {
                    var playerClient = new Client(_settings.Players[i].Name);
                    var data = new ViewerData(_gameHost);
                    var actions = new ViewerActions(playerClient);

                    var logic = isHost
                        ? hostLogicFactory(data, actions, localizer)
                        : new ViewerComputerLogic(
                            data,
                            actions,
                            new Intelligence((ComputerAccount)_settings.Players[i]),
                            GameRole.Player);
                    
                    var player = new Player(playerClient, _settings.Players[i], isHost, logic, actions, localizer, data);
                    playerClient.ConnectTo(_node);

                    if (isHost)
                    {
                        host = player;
                    }

                    gameData.Players[i].IsConnected = true;
                }
            }

            for (int i = 0; i < _settings.Viewers.Length; i++)
            {
                var name = _settings.Viewers[i].Name;
                isHost = _createHost && gameData.HostName == name;

                if (isHost)
                {
                    gameData.Viewers.Add(new ViewerAccount(_settings.Viewers[i]));
                    
                    var viewerClient = new Client(_settings.Viewers[i].Name);
                    var data = new ViewerData(_gameHost);
                    var actions = new ViewerActions(viewerClient);
                    var logic = hostLogicFactory(data, actions, localizer);
                    var viewer = new Viewer(viewerClient, _settings.Viewers[i], isHost, logic, actions, localizer, data);
                    viewerClient.ConnectTo(_node);
                    host = viewer;

                    gameData.Viewers[i].IsConnected = true;
                }
            }
        }
        finally
        {
            gameData.EndUpdatePersons();
        }

        var playHandler = new PlayHandler(gameData);
        var questionPlayHandler = new QuestionPlayHandler(gameData);

        var engine = EngineFactory.CreateEngine(
            gameData.Settings.AppSettings.GameMode == GameModes.Tv,
            _document,
            () => CreateEngineOptions(gameData.Settings.AppSettings),
            playHandler,
            questionPlayHandler);

        var client = Client.Create(NetworkConstants.GameName, _node);

        var gameActions = new GameActions(client, gameData, localizer, _fileShare);
        
        var gameLogic = new GameLogic(
            gameData,
            gameActions,
            /* TODO: This dependency should be removed by using engine callbacks */ engine,
            localizer,
            _fileShare,
            _pinHelper);

        questionPlayHandler.GameLogic = gameLogic;
        playHandler.GameActions = gameActions;
        playHandler.GameLogic = gameLogic;

        var game = new Game(
            client,
            localizer,
            gameData, 
            gameActions, 
            gameLogic,
            _defaultPlayers,
            _defaultShowmans,
            _fileShare,
            _avatarHelper);

        game.Run();

        return (host, game);
    }
}
