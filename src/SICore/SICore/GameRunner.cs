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
        /* TODO: remove */ IGameSettingsCore<AppSettingsCore> settings,
        SI.Contracts.RoomSettings roomSettings,
        SI.Contracts.TimeSettings timeSettings,
        SI.Contracts.RulesSettings rules,
        /* TODO: remove */ string language,
        SIDocument document,
        IGameHost gameHost,
        IFileShare fileShare,
        ComputerAccount[] defaultPlayers,
        ComputerAccount[] defaultShowmans,
        ComputerAccount[] customAccounts,
        IAvatarHelper avatarHelper,
        IPinHelper? pinHelper,
        Uri? packageSource,
        string? gameName = null,
        IPackageStatisticsProvider? packageStatisticsProvider = null,
        bool hiddenPlayers = false)
    {
        var gameState = new GameState(
            gameHost,
            new GamePersonAccount(settings.Showman),
            packageSource,
            language,
            roomSettings,
            timeSettings,
            rules,
            packageStatisticsProvider)
        {
            HostName = roomSettings.IsAutomatic ? null : roomSettings.HostName,
            GameName = gameName ?? "",
            HiddenPersons = hiddenPlayers,
        };

        var localizer = new Localizer(language);

        gameState.BeginUpdatePersons("Start");

        try
        {
            var showmanSettings = roomSettings.Showman;

            if (showmanSettings.Type == SI.Contracts.Models.AccountType.Bot)
            {
                var showmanClient = new Client(showmanSettings.Name);
                var state = new PersonState(showmanSettings.AvatarUri);
                var actions = new PersonActions(showmanClient);

                var controller = new PersonComputerController(
                    state,
                    actions,
                    new Intelligence(GetComputerAccount(showmanSettings, defaultShowmans, customAccounts)),
                    GameRole.Showman);

                var showman = new Showman(showmanClient, controller, actions, state);
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
                for (int i = 0; i < roomSettings.Players.Length; i++)
                {
                    var playerSettings = roomSettings.Players[i];
                    gameState.Players.Add(new GamePlayerAccount(settings.Players[i]));

                    if (playerSettings.Type == SI.Contracts.Models.AccountType.Bot)
                    {
                        var playerClient = new Client(playerSettings.Name);
                        var state = new PersonState(playerSettings.AvatarUri);
                        var actions = new PersonActions(playerClient);

                        var controller = new PersonComputerController(
                            state,
                            actions,
                            new Intelligence(GetComputerAccount(playerSettings, defaultPlayers, customAccounts)),
                            GameRole.Player);

                        var player = new Player(playerClient, controller, actions, state);
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
            /* TODO: remove */ localizer,
            fileShare,
            pinHelper);

        questionPlayHandler.Controller = gameController;
        playHandler.GameActions = gameActions;
        playHandler.Controller = gameController;

        return new Game(
            client,
            /* TODO: remove */ localizer,
            gameState,
            gameActions,
            gameController,
            defaultPlayers,
            defaultShowmans,
            fileShare,
            avatarHelper);
    }

    private static ComputerAccount GetComputerAccount(
        SI.Contracts.Models.Account account,
        ComputerAccount[] defaultAccounts,
        ComputerAccount[] customAccounts)
    {
        var name = account.Name;
        var computerAccount = defaultAccounts.FirstOrDefault(a => a.Name == name);

        if (computerAccount != null)
        {
            return computerAccount;
        }

        computerAccount = customAccounts.FirstOrDefault(a => a.Name == name);

        if (computerAccount != null)
        {
            return computerAccount;
        }

        computerAccount = new ComputerAccount(account.Name, account.Gender == SI.Contracts.Models.Gender.Male) { Picture = account.AvatarUri };
        computerAccount.Randomize();

        return computerAccount;
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
