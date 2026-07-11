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
    private static EngineOptions CreateEngineOptions(SI.Contracts.RulesSettings rules) => new()
    {
        IsMultimediaPressMode = rules.FalseStart,
        IsPressMode = rules.FalseStart,
        ShowRight = true,
        PlaySpecials = true,
        PlayAllQuestionsInFinalRound = rules.PlayAllThemesInThemesRemovalRound,
    };

    public static Game CreateGame(
        Node node,
        IGameSettingsCore<AppSettingsCore> settings,
        SI.Contracts.RoomSettings roomSettings,
        SI.Contracts.TimeSettings timeSettings,
        SI.Contracts.RulesSettings rules,
        SIDocument document,
        IGameHost gameHost,
        IFileShare fileShare,
        ComputerAccount[] defaultPlayers,
        ComputerAccount[] defaultShowmans,
        IAvatarHelper avatarHelper,
        IPinHelper? pinHelper,
        Uri? packageSource,
        string? gameName = null,
        IPackageStatisticsProvider? packageStatisticsProvider = null,
        bool hiddenPlayers = false)
    {
        var gameState = new GameState(gameHost, new GamePersonAccount(settings.Showman), packageSource, settings, roomSettings, timeSettings, rules, packageStatisticsProvider)
        {
            HostName = roomSettings.IsAutomatic ? null : roomSettings.HostName,
            GameName = gameName ?? "",
            HiddenPersons = hiddenPlayers,
        };

        var localizer = new Localizer(settings.AppSettings.Culture ?? "en-US");

        gameState.BeginUpdatePersons("Start");

        try
        {
            if (!settings.Showman.IsHuman)
            {
                var showmanClient = new Client(settings.Showman.Name);
                var state = new PersonState();
                var actions = new PersonActions(showmanClient);

                var logic = new PersonComputerController(
                    state,
                    actions,
                    new Intelligence((ComputerAccount)settings.Showman),
                    GameRole.Showman);

                var showman = new Showman(showmanClient, settings.Showman, logic, actions, state);
                showmanClient.ConnectTo(node);

                gameState.ShowMan.IsConnected = true;
            }

            if (hiddenPlayers)
            {
                for (int i = 0; i < 24; i++)
                {
                    gameState.Players.Add(new GamePlayerAccount(new Account { IsHuman = true }));
                }
            }
            else
            {
                for (int i = 0; i < settings.Players.Length; i++)
                {
                    gameState.Players.Add(new GamePlayerAccount(settings.Players[i]));
                    var name = settings.Players[i].Name;
                    var human = settings.Players[i].IsHuman;

                    if (!human)
                    {
                        var playerClient = new Client(settings.Players[i].Name);
                        var state = new PersonState();
                        var actions = new PersonActions(playerClient);

                        var logic = new PersonComputerController(
                            state,
                            actions,
                            new Intelligence((ComputerAccount)settings.Players[i]),
                            GameRole.Player);

                        var player = new Player(playerClient, settings.Players[i], logic, actions, state);
                        playerClient.ConnectTo(node);

                        gameState.Players[i].IsConnected = true;
                    }
                }
            }
        }
        finally
        {
            gameState.EndUpdatePersons();
        }

        var playHandler = new PlayHandler(gameState);
        var questionPlayHandler = new QuestionPlayHandler(gameState);
        var gameRules = GetGameRules(gameState.Rules.GameMode);

        var engine = EngineFactory.CreateEngine(
            gameRules,
            document,
            () => CreateEngineOptions(gameState.Rules),
            playHandler,
            questionPlayHandler);

        var client = Client.Create(NetworkConstants.GameName, node);

        var gameActions = new GameActions(client, gameState, fileShare);

        var gameController = new GameController(
            gameState,
            gameActions,
            /* TODO: This dependency should be removed by using engine callbacks */ engine,
            localizer,
            fileShare,
            pinHelper);

        questionPlayHandler.Controller = gameController;
        playHandler.GameActions = gameActions;
        playHandler.Controller = gameController;

        return new Game(
            client,
            localizer,
            gameState,
            gameActions,
            gameController,
            defaultPlayers,
            defaultShowmans,
            fileShare,
            avatarHelper);
    }

    private static GameRules GetGameRules(SI.Contracts.GameMode gameMode) => gameMode switch
    {
        SI.Contracts.GameMode.Classic => WellKnownGameRules.Classic,
        SI.Contracts.GameMode.Sequential => WellKnownGameRules.Simple,
        SI.Contracts.GameMode.Quiz => WellKnownGameRules.Quiz,
        SI.Contracts.GameMode.TurnTaking => WellKnownGameRules.TurnTaking,
        _ => throw new NotSupportedException($"Game mode {gameMode} is not supported"),
    };
}
