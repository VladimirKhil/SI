using SICore.Clients.Game;
using SICore.Contracts;
using SICore.Network;
using SICore.Network.Clients;
using SICore.Network.Servers;
using SICore.Services;
using SIData;
using SIEngine;
using SIEngine.Rules;
using SIPackages;

namespace SICore;

/// <summary>
/// Creates and runs new game.
/// </summary>
public static class GameRunner
{
    private static EngineOptions CreateEngineOptions(AppSettingsCore appSettingsCore) => new()
    {
        IsMultimediaPressMode = appSettingsCore.FalseStart,
        IsPressMode = appSettingsCore.FalseStart,
        ShowRight = true,
        PlaySpecials = true,
        PlayAllQuestionsInFinalRound = appSettingsCore.PlayAllQuestionsInFinalRound,
    };

    public static Game CreateGame(
        Node node,
        IGameSettingsCore<AppSettingsCore> settings,
        SIDocument document,
        IGameHost gameHost,
        IFileShare fileShare,
        ComputerAccount[] defaultPlayers,
        ComputerAccount[] defaultShowmans,
        IAvatarHelper avatarHelper,
        IPinHelper? pinHelper,
        Uri? packageSource,
        string? gameName = null,
        IPackageStatisticsProvider? packageStatisticsProvider = null)
    {
        var gameData = new GameData(gameHost, new GamePersonAccount(settings.Showman), packageSource, settings, packageStatisticsProvider)
        {
            HostName = settings.IsAutomatic ? null : settings.HumanPlayerName,
            GameName = gameName ?? "",
        };

        var localizer = new Localizer(settings.AppSettings.Culture ?? "en-US");

        gameData.BeginUpdatePersons("Start");

        try
        {
            if (!settings.Showman.IsHuman)
            {
                var showmanClient = new Client(settings.Showman.Name);
                var data = new ViewerData();
                var actions = new ViewerActions(showmanClient);

                var logic = new ViewerComputerLogic(
                    data,
                    actions,
                    new Intelligence((ComputerAccount)settings.Showman),
                    GameRole.Showman);

                var showman = new Showman(showmanClient, settings.Showman, logic, actions, data);
                showmanClient.ConnectTo(node);

                gameData.ShowMan.IsConnected = true;
            }

            for (int i = 0; i < settings.Players.Length; i++)
            {
                gameData.Players.Add(new GamePlayerAccount(settings.Players[i]));
                var name = settings.Players[i].Name;
                var human = settings.Players[i].IsHuman;

                if (!human)
                {
                    var playerClient = new Client(settings.Players[i].Name);
                    var data = new ViewerData();
                    var actions = new ViewerActions(playerClient);

                    var logic = new ViewerComputerLogic(
                        data,
                        actions,
                        new Intelligence((ComputerAccount)settings.Players[i]),
                        GameRole.Player);

                    var player = new Player(playerClient, settings.Players[i], logic, actions, data);
                    playerClient.ConnectTo(node);

                    gameData.Players[i].IsConnected = true;
                }
            }
        }
        finally
        {
            gameData.EndUpdatePersons();
        }

        var playHandler = new PlayHandler(gameData);
        var questionPlayHandler = new QuestionPlayHandler(gameData);
        var gameRules = GetGameRules(gameData.Settings.AppSettings.GameMode);

        var engine = EngineFactory.CreateEngine(
            gameRules,
            document,
            () => CreateEngineOptions(gameData.Settings.AppSettings),
            playHandler,
            questionPlayHandler);

        var client = Client.Create(NetworkConstants.GameName, node);

        var gameActions = new GameActions(client, gameData, fileShare);

        var gameLogic = new GameLogic(
            gameData,
            gameActions,
            /* TODO: This dependency should be removed by using engine callbacks */ engine,
            localizer,
            fileShare,
            pinHelper);

        questionPlayHandler.GameLogic = gameLogic;
        playHandler.GameActions = gameActions;
        playHandler.GameLogic = gameLogic;

        return new Game(
            client,
            localizer,
            gameData,
            gameActions,
            gameLogic,
            defaultPlayers,
            defaultShowmans,
            fileShare,
            avatarHelper);
    }

    private static GameRules GetGameRules(GameModes gameMode) => gameMode switch
    {
        GameModes.Tv => WellKnownGameRules.Classic,
        GameModes.Sport => WellKnownGameRules.Simple,
        GameModes.Quiz => WellKnownGameRules.Quiz,
        GameModes.TurnTaking => WellKnownGameRules.TurnTaking,
        _ => throw new NotSupportedException($"Game mode {gameMode} is not supported"),
    };
}
