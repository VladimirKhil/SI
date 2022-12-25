using SICore.BusinessLogic;
using SICore.Network;
using SICore.Network.Clients;
using SICore.Network.Servers;
using SIData;
using SIEngine;
using SIPackages;

namespace SICore;

/// <summary>
/// Creates and runs new game.
/// </summary>
public sealed class GameRunner
{
    private readonly Node _node;
    private readonly IGameSettingsCore<AppSettingsCore> _settings;
    private readonly SIDocument _document;
    private readonly IGameManager _backLink;
    private readonly IShare _share;
    private readonly ComputerAccount[] _defaultPlayers;
    private readonly ComputerAccount[] _defaultShowmans;
    private readonly bool _createHost;

    public GameRunner(
        Node node,
        IGameSettingsCore<AppSettingsCore> settings,
        SIDocument document,
        IGameManager backLink,
        IShare share,
        ComputerAccount[] defaultPlayers,
        ComputerAccount[] defaultShowmans,
        bool createHost = true)
    {
        _node = node;
        _settings = settings;
        _document = document;
        _backLink = backLink;
        _share = share;
        _defaultPlayers = defaultPlayers;
        _defaultShowmans = defaultShowmans;
        _createHost = createHost;
    }

    public (IViewerClient?, Game) Run()
    {
        var gameData = new GameData(_backLink, new GamePersonAccount(_settings.Showman))
        {
            Settings = _settings,
            HostName = _settings.IsAutomatic ? null : _settings.HumanPlayerName,
            Share = _share
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
                var showman = new Showman(showmanClient, _settings.Showman, isHost, localizer, new ViewerData(_backLink));
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
                    var player = new Player(playerClient, _settings.Players[i], isHost, localizer, new ViewerData(_backLink));
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
                    var viewer = new SimpleViewer(viewerClient, _settings.Viewers[i], isHost, localizer, new ViewerData(_backLink));
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

        var engine = (EngineBase)EngineFactory.CreateEngine(
            gameData.Settings.AppSettings.GameMode == GameModes.Tv,
            _document,
            DefaultEngineSettingsProvider.Instance);

        var client = Client.Create(NetworkConstants.GameName, _node);

        var gameActions = new GameActions(client, gameData, localizer);
        var gameLogic = new GameLogic(gameData, gameActions, engine, localizer);
        var game = new Game(client, null, localizer, gameData, gameActions, gameLogic, _defaultPlayers, _defaultShowmans);

        game.Run();

        return (host, game);
    }
}
