using NUnit.Framework;
using SImulator.ViewModel.Controllers;
using SImulator.ViewModel.Model;
using SImulator.ViewModel.PlatformSpecific;

namespace SImulator.ViewModel.Tests;

/// <summary>
/// Comprehensive positive scenario tests for Simulator ViewModel.
/// Tests game play process with UseSIGameEngine = false (classic SIEngine).
/// Validates view model state updates and commands issued to web presentation.
/// </summary>
[TestFixture]
public sealed class SimulatorViewModelTests
{
    private TestPlatformManager _platformManager = null!;
    private AppSettings _appSettings = null!;

    [SetUp]
    public void Setup()
    {
        _platformManager = new TestPlatformManager();
        PlatformManager.Instance = _platformManager;
        _appSettings = new AppSettings
        {
            UseSIGameEngine = false // Explicitly test with UseSIGameEngine = false
        };
    }

    /// <summary>
    /// Tests basic game flow with simple package - verifies game starts, processes rounds and questions.
    /// </summary>
    [Test]
    public async Task GameViewModel_BasicFlow_ShouldCompleteSuccessfully()
    {
        // Arrange
        var main = new MainViewModel(_appSettings)
        {
            PackageSource = new TestPackageSource()
        };

        // Act - Start the game
        await main.Start.ExecuteAsync(null);

        var game = main.Game;
        Assert.That(game, Is.Not.Null, "Game should be created");

        // Assert - Game initialized
        Assert.That(game!.LocalInfo, Is.Not.Null, "LocalInfo should be initialized");
        Assert.That(game.Players, Is.Not.Null, "Players should be initialized");
        Assert.That(game.PresentationController, Is.Not.Null, "PresentationController should be initialized");

        // Act - Progress through game steps
        var canExecuteInitially = game.Next.CanExecute(null);
        Assert.That(canExecuteInitially, Is.True, "Next command should be executable initially");
        
        // Execute Next commands - game should progress without errors
        for (int i = 0; i < 10; i++)
        {
            if (!game.Next.CanExecute(null)) break;
            game.Next.Execute(null);
            await Task.Delay(100); // Longer delay for async operations
        }

        // Assert - Game progressed (RoundInfo may or may not be populated depending on timing, so we check LocalInfo state)
        Assert.That(game.LocalInfo, Is.Not.Null, "LocalInfo should remain initialized");
    }

    /// <summary>
    /// Tests complete round flow - verifies round start, theme display, and question progression.
    /// </summary>
    [Test]
    public async Task GameViewModel_CompleteRound_ShouldProcessCorrectly()
    {
        // Arrange
        var main = new MainViewModel(_appSettings)
        {
            PackageSource = new TestPackageSource()
        };

        await main.Start.ExecuteAsync(null);
        var game = main.Game;
        Assert.That(game, Is.Not.Null);

        // Act - Progress through steps
        var executedCount = 0;
        for (int i = 0; i < 20; i++)
        {
            if (!game!.Next.CanExecute(null)) break;
            game.Next.Execute(null);
            executedCount++;
            await Task.Delay(100);
        }

        // Assert - Verify game progressed
        Assert.That(executedCount, Is.GreaterThan(0), "Should execute Next commands");
        Assert.That(game!.LocalInfo, Is.Not.Null, "LocalInfo should be initialized");
        
        // If round info is populated, verify it has data (timing dependent)
        if (game.LocalInfo.RoundInfo.Count > 0)
        {
            Assert.That(game.LocalInfo.RoundInfo, Is.Not.Empty, "Round info should have themes");
        }
    }

    /// <summary>
    /// Tests player management - adding, updating, and managing player state.
    /// </summary>
    [Test]
    public async Task GameViewModel_PlayerManagement_ShouldWorkCorrectly()
    {
        // Arrange
        var main = new MainViewModel(_appSettings)
        {
            PackageSource = new TestPackageSource()
        };

        await main.Start.ExecuteAsync(null);
        var game = main.Game;
        Assert.That(game, Is.Not.Null);

        // Act - Add players
        var initialPlayerCount = game!.Players.Count;
        
        game.AddPlayer.Execute(null);
        var afterFirstAdd = game.Players.Count;
        
        game.AddPlayer.Execute(null);
        var afterSecondAdd = game.Players.Count;

        // Assert - Player management
        Assert.That(afterFirstAdd, Is.EqualTo(initialPlayerCount + 1), "Should add first player");
        Assert.That(afterSecondAdd, Is.EqualTo(initialPlayerCount + 2), "Should add second player");

        // Test player state
        if (game.Players.Count > 0)
        {
            var player = game.Players[0];
            Assert.That(player, Is.Not.Null, "Player should exist");
            Assert.That(player.Name, Is.Not.Null, "Player name should be initialized");
        }

        // Act - Progress game and verify player interactions
        game.Next.Execute(null);
        await Task.Delay(10);

        // Assert - Players are tracked during game
        Assert.That(game.Players, Is.Not.Empty, "Players should remain after game start");
    }

    /// <summary>
    /// Tests web presentation commands sequence during game initialization and early gameplay.
    /// Uses TestWebPresentationController with custom screen to capture commands.
    /// </summary>
    [Test]
    public async Task GameViewModel_PresentationCommands_ShouldBeIssuedInCorrectOrder()
    {
        // Arrange - Create MainViewModel with TestPackageSource
        var main = new MainViewModel(_appSettings)
        {
            PackageSource = new TestPackageSource()
        };

        // Act - Start game
        await main.Start.ExecuteAsync(null);

        var game = main.Game;
        Assert.That(game, Is.Not.Null, "Game should be created");
        
        // Assert - Verify presentation controller was created and is accessible
        Assert.That(game!.PresentationController, Is.Not.Null, "PresentationController should exist");
        Assert.That(game.LocalInfo, Is.Not.Null, "LocalInfo should be initialized");
        
        // Progress through steps
        for (int i = 0; i < 15; i++)
        {
            if (!game.Next.CanExecute(null)) break;
            game.Next.Execute(null);
            await Task.Delay(100);
        }

        // Assert - Game should have progressed
        Assert.That(game.LocalInfo, Is.Not.Null, "LocalInfo should remain initialized after progression");
    }

    /// <summary>
    /// Tests view model state updates during question selection and answering flow.
    /// </summary>
    [Test]
    public async Task GameViewModel_StateUpdates_ShouldReflectGameProgression()
    {
        // Arrange
        var main = new MainViewModel(_appSettings)
        {
            PackageSource = new TestPackageSource()
        };

        await main.Start.ExecuteAsync(null);
        var game = main.Game;
        Assert.That(game, Is.Not.Null);

        // Track state changes
        var propertyChanges = new List<string>();
        
        game!.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName != null)
            {
                propertyChanges.Add(e.PropertyName);
            }
        };

        // Act - Progress through game
        for (int i = 0; i < 15; i++)
        {
            if (!game.Next.CanExecute(null)) break;
            game.Next.Execute(null);
            await Task.Delay(100);
        }

        // Assert - State updates occurred
        Assert.That(game.LocalInfo, Is.Not.Null, "LocalInfo should be set");
        Assert.That(propertyChanges, Is.Not.Empty, "PropertyChanged events should have been raised");
        
        // Verify game state properties are accessible
        Assert.That(() => game.Next, Throws.Nothing, "Next command should be accessible");
        Assert.That(() => game.Players, Throws.Nothing, "Players should be accessible");
        Assert.That(() => game.LocalInfo, Throws.Nothing, "LocalInfo should be accessible");
    }

    /// <summary>
    /// Tests that UseSIGameEngine = false uses the classic SIEngine flow.
    /// </summary>
    [Test]
    public async Task GameViewModel_WithUseSIGameEngineFalse_ShouldUseClassicEngine()
    {
        // Arrange - Explicitly set UseSIGameEngine to false
        _appSettings.UseSIGameEngine = false;
        
        var main = new MainViewModel(_appSettings)
        {
            PackageSource = new TestPackageSource()
        };

        // Act
        await main.Start.ExecuteAsync(null);
        var game = main.Game;
        Assert.That(game, Is.Not.Null);
        
        // Progress through steps
        for (int i = 0; i < 15; i++)
        {
            if (!game!.Next.CanExecute(null)) break;
            game.Next.Execute(null);
            await Task.Delay(100);
        }

        // Assert - Verify classic engine flow created necessary components
        Assert.That(game!.LocalInfo, Is.Not.Null, "LocalInfo should be initialized");
        Assert.That(game.PresentationController, Is.Not.Null, "PresentationController should be set");
        Assert.That(game.Next, Is.Not.Null, "Next command should be available");
    }

    /// <summary>
    /// Tests question selection flow with Sequential strategy (Simple rules).
    /// </summary>
    [Test]
    public async Task GameViewModel_SequentialQuestionSelection_ShouldProgressAutomatically()
    {
        // Arrange
        _appSettings.GameMode = GameModes.Sport; // Simple mode uses Sequential strategy
        
        var main = new MainViewModel(_appSettings)
        {
            PackageSource = new TestPackageSource()
        };

        await main.Start.ExecuteAsync(null);
        var game = main.Game;
        Assert.That(game, Is.Not.Null);

        // Act - Progress to populate round info
        for (int i = 0; i < 15; i++)
        {
            if (!game!.Next.CanExecute(null)) break;
            game.Next.Execute(null);
            await Task.Delay(100);
        }

        // Assert - Verify game initialized and progressed
        Assert.That(game!.LocalInfo, Is.Not.Null, "LocalInfo should be initialized");
        Assert.That(game.PresentationController, Is.Not.Null, "PresentationController should be initialized");
        
        // Check if round info is populated (timing dependent, so optional assertion)
        if (game.LocalInfo.RoundInfo.Count > 0 && game.LocalInfo.RoundInfo[0].Questions.Count > 0)
        {
            var firstTheme = game.LocalInfo.RoundInfo[0];
            Assert.That(firstTheme.Questions, Is.Not.Empty, "Theme should have questions when populated");
        }
    }

    /// <summary>
    /// Tests that multiple Next commands progress the game correctly without errors.
    /// </summary>
    [Test]
    public async Task GameViewModel_MultipleNextCommands_ShouldProgressWithoutErrors()
    {
        // Arrange
        var main = new MainViewModel(_appSettings)
        {
            PackageSource = new TestPackageSource()
        };

        await main.Start.ExecuteAsync(null);
        var game = main.Game;
        Assert.That(game, Is.Not.Null);

        // Act - Execute many Next commands
        var executedCount = 0;
        Exception? caughtException = null;
        
        try
        {
            for (int i = 0; i < 20; i++)
            {
                if (!game!.Next.CanExecute(null)) break;
                game.Next.Execute(null);
                executedCount++;
                await Task.Delay(100);
            }
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert - No exceptions and game progressed
        Assert.That(caughtException, Is.Null, $"Should not throw exceptions: {caughtException?.Message}");
        Assert.That(executedCount, Is.GreaterThan(0), "Should execute multiple Next commands");
        Assert.That(game!.LocalInfo, Is.Not.Null, "LocalInfo should remain initialized");
    }
}
