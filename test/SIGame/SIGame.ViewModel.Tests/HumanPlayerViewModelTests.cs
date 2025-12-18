using SIData;
using SIGame.ViewModel;
using SIGame.ViewModel.PlatformSpecific;
using SIGame.ViewModel.Tests.Mocks;

namespace SIGame.ViewModel.Tests;

/// <summary>
/// Positive scenario tests for HumanPlayerViewModel.
/// Tests player management, account creation, and property updates.
/// </summary>
[TestFixture]
public sealed class HumanPlayerViewModelTests
{
    private TestPlatformManager _platformManager = null!;

    [SetUp]
    public void Setup()
    {
        _platformManager = new TestPlatformManager();
        PlatformManager.Instance = _platformManager;
    }

    #region Initialization

    [Test]
    public void HumanPlayerViewModel_Initialization_ShouldSetupProperties()
    {
        // Arrange
        var commonSettings = new CommonSettings();
        commonSettings.Humans2.Add(new HumanAccount { Name = "Player 1" });

        // Act
        var viewModel = new HumanPlayerViewModel(commonSettings);

        // Assert
        Assert.That(viewModel, Is.Not.Null);
        Assert.That(viewModel.HumanPlayers, Is.Not.Null);
        Assert.That(viewModel.HumanPlayers.Length, Is.GreaterThan(0));
        Assert.That(viewModel.EditAccount, Is.Not.Null);
        Assert.That(viewModel.RemoveAccount, Is.Not.Null);
    }

    [Test]
    public void HumanPlayerViewModel_Initialization_WithGameSettings_ShouldSetupCorrectly()
    {
        // Arrange
        var gameSettings = new GameSettings();
        var commonSettings = new CommonSettings();
        commonSettings.Humans2.Add(new HumanAccount { Name = "Player 1" });

        // Act
        var viewModel = new HumanPlayerViewModel(gameSettings, commonSettings);

        // Assert
        Assert.That(viewModel, Is.Not.Null);
        Assert.That(viewModel.Model, Is.SameAs(gameSettings));
        Assert.That(viewModel.HumanPlayers, Is.Not.Null);
    }

    #endregion

    #region HumanPlayers Management

    [Test]
    public void HumanPlayerViewModel_UpdateHumanPlayers_ShouldIncludeNewOption()
    {
        // Arrange
        var commonSettings = new CommonSettings();
        commonSettings.Humans2.Add(new HumanAccount { Name = "Player 1" });
        var viewModel = new HumanPlayerViewModel(commonSettings);

        // Act
        viewModel.UpdateHumanPlayers();

        // Assert
        Assert.That(viewModel.HumanPlayers, Is.Not.Null);
        Assert.That(viewModel.HumanPlayers.Length, Is.GreaterThanOrEqualTo(2)); // At least one player + "New" option
        Assert.That(viewModel.HumanPlayers.Last().CanBeDeleted, Is.False); // "New" option cannot be deleted
    }

    [Test]
    public void HumanPlayerViewModel_UpdateHumanPlayers_ShouldIncludeAllPlayers()
    {
        // Arrange
        var commonSettings = new CommonSettings();
        commonSettings.Humans2.Add(new HumanAccount { Name = "Player 1" });
        commonSettings.Humans2.Add(new HumanAccount { Name = "Player 2" });
        var viewModel = new HumanPlayerViewModel(commonSettings);

        // Act
        viewModel.UpdateHumanPlayers();

        // Assert
        Assert.That(viewModel.HumanPlayers.Length, Is.EqualTo(3)); // 2 players + "New" option
    }

    #endregion

    #region HumanPlayer Property

    [Test]
    public void HumanPlayerViewModel_SetHumanPlayer_ShouldUpdateProperty()
    {
        // Arrange
        var commonSettings = new CommonSettings();
        var player1 = new HumanAccount { Name = "Player 1" };
        var player2 = new HumanAccount { Name = "Player 2" };
        commonSettings.Humans2.Add(player1);
        commonSettings.Humans2.Add(player2);
        var viewModel = new HumanPlayerViewModel(commonSettings);

        // Act
        viewModel.HumanPlayer = player2;

        // Assert
        Assert.That(viewModel.HumanPlayer, Is.EqualTo(player2));
    }

    [Test]
    public void HumanPlayerViewModel_SetHumanPlayer_ShouldRaisePropertyChanged()
    {
        // Arrange
        var commonSettings = new CommonSettings();
        var player1 = new HumanAccount { Name = "Player 1" };
        var player2 = new HumanAccount { Name = "Player 2" };
        commonSettings.Humans2.Add(player1);
        commonSettings.Humans2.Add(player2);
        var viewModel = new HumanPlayerViewModel(commonSettings);
        var propertyChangedRaised = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(viewModel.HumanPlayer))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        viewModel.HumanPlayer = player2;

        // Assert
        Assert.That(propertyChangedRaised, Is.True);
    }

    #endregion

    #region IsProgress Property

    [Test]
    public void HumanPlayerViewModel_IsProgress_ShouldReturnFalse()
    {
        // Arrange
        var commonSettings = new CommonSettings();
        commonSettings.Humans2.Add(new HumanAccount { Name = "Player 1" });

        // Act
        var viewModel = new HumanPlayerViewModel(commonSettings);

        // Assert
        Assert.That(viewModel.IsProgress, Is.False);
    }

    #endregion

    #region NewAccount Property

    [Test]
    public void HumanPlayerViewModel_NewAccount_CanBeSetToNull()
    {
        // Arrange
        var commonSettings = new CommonSettings();
        commonSettings.Humans2.Add(new HumanAccount { Name = "Player 1" });
        var viewModel = new HumanPlayerViewModel(commonSettings);

        // Act
        viewModel.NewAccount = null;

        // Assert
        Assert.That(viewModel.NewAccount, Is.Null);
    }

    [Test]
    public void HumanPlayerViewModel_NewAccount_PropertyChangedRaisedOnNull()
    {
        // Arrange
        var commonSettings = new CommonSettings();
        commonSettings.Humans2.Add(new HumanAccount { Name = "Player 1" });
        var viewModel = new HumanPlayerViewModel(commonSettings);
        var propertyChangedRaised = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(viewModel.NewAccount))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        viewModel.NewAccount = null;

        // Assert
        Assert.That(propertyChangedRaised, Is.True);
    }

    #endregion

    #region Commands

    [Test]
    public void HumanPlayerViewModel_EditAccountCommand_ShouldBeExecutable()
    {
        // Arrange
        var commonSettings = new CommonSettings();
        var player = new HumanAccount { Name = "Player 1" };
        commonSettings.Humans2.Add(player);
        var viewModel = new HumanPlayerViewModel(commonSettings);

        // Act
        var canExecute = viewModel.EditAccount.CanExecute(player);

        // Assert
        Assert.That(canExecute, Is.True);
    }

    [Test]
    public void HumanPlayerViewModel_RemoveAccountCommand_ShouldBeExecutable()
    {
        // Arrange
        var commonSettings = new CommonSettings();
        var player = new HumanAccount { Name = "Player 1", CanBeDeleted = true };
        commonSettings.Humans2.Add(player);
        var viewModel = new HumanPlayerViewModel(commonSettings);

        // Act
        var canExecute = viewModel.RemoveAccount.CanExecute(player);

        // Assert
        Assert.That(canExecute, Is.True);
    }

    #endregion

    #region Multiple Players

    [Test]
    public void HumanPlayerViewModel_WithMultiplePlayers_ShouldInitializeCorrectly()
    {
        // Arrange
        var commonSettings = new CommonSettings();
        commonSettings.Humans2.Add(new HumanAccount { Name = "Player 1" });
        commonSettings.Humans2.Add(new HumanAccount { Name = "Player 2" });
        commonSettings.Humans2.Add(new HumanAccount { Name = "Player 3" });

        // Act
        var viewModel = new HumanPlayerViewModel(commonSettings);

        // Assert
        Assert.That(viewModel.HumanPlayers.Length, Is.EqualTo(4)); // 3 players + "New" option
        Assert.That(viewModel.HumanPlayer, Is.Not.Null);
    }

    #endregion

    #region Model Updates

    [Test]
    public void HumanPlayerViewModel_SetHumanPlayer_ShouldUpdateModelName()
    {
        // Arrange
        var gameSettings = new GameSettings();
        var commonSettings = new CommonSettings();
        var player = new HumanAccount { Name = "Player 1" };
        commonSettings.Humans2.Add(player);
        var viewModel = new HumanPlayerViewModel(gameSettings, commonSettings);

        // Act
        viewModel.HumanPlayer = player;

        // Assert
        Assert.That(viewModel.Model.HumanPlayerName, Is.EqualTo("Player 1"));
    }

    #endregion
}
