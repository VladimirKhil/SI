using Microsoft.Extensions.DependencyInjection;
using SIGame.ViewModel.PlatformSpecific;
using SIGame.ViewModel.Tests.Helpers;
using SIGame.ViewModel.Tests.Mocks;

namespace SIGame.ViewModel.Tests;

/// <summary>
/// Positive scenario tests for MainViewModel.
/// Tests basic initialization, navigation, and command execution.
/// </summary>
[TestFixture]
public sealed class MainViewModelTests
{
    private IServiceProvider _serviceProvider = null!;
    private TestPlatformManager _platformManager = null!;

    [SetUp]
    public void Setup()
    {
        _platformManager = new TestPlatformManager();
        PlatformManager.Instance = _platformManager;
        _serviceProvider = TestHelper.CreateServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    #region Initialization

    [Test]
    public void MainViewModel_Initialization_ShouldSetupCommands()
    {
        // Act
        var mainViewModel = TestHelper.CreateMainViewModel(_serviceProvider);

        // Assert
        Assert.That(mainViewModel, Is.Not.Null);
        Assert.That(mainViewModel.NewGame, Is.Not.Null, "NewGame command should be initialized");
        Assert.That(mainViewModel.Open, Is.Not.Null, "Open command should be initialized");
        Assert.That(mainViewModel.NetworkGame, Is.Not.Null, "NetworkGame command should be initialized");
        Assert.That(mainViewModel.BestPlayers, Is.Not.Null, "BestPlayers command should be initialized");
        Assert.That(mainViewModel.About, Is.Not.Null, "About command should be initialized");
        Assert.That(mainViewModel.SetProfile, Is.Not.Null, "SetProfile command should be initialized");
        Assert.That(mainViewModel.Cancel, Is.Not.Null, "Cancel command should be initialized");
    }

    [Test]
    public void MainViewModel_Initialization_ShouldSetupProperties()
    {
        // Act
        var mainViewModel = TestHelper.CreateMainViewModel(_serviceProvider);

        // Assert
        Assert.That(mainViewModel.Human, Is.Not.Null, "Human player should be initialized");
        Assert.That(mainViewModel.Settings, Is.Not.Null, "Settings should be initialized");
        Assert.That(mainViewModel.MainMenu, Is.Not.Null, "MainMenu should be initialized");
        Assert.That(mainViewModel.StartMenu, Is.Not.Null, "StartMenu should be initialized");
        Assert.That(mainViewModel.ActiveView, Is.Not.Null, "ActiveView should be initialized");
    }

    [Test]
    public void MainViewModel_Initialization_ShouldSetupStartMenu()
    {
        // Act
        var mainViewModel = TestHelper.CreateMainViewModel(_serviceProvider);

        // Assert
        Assert.That(mainViewModel.StartMenu.MainCommands, Is.Not.Empty, "StartMenu should have commands");
        Assert.That(mainViewModel.StartMenu.MainCommands.Count, Is.GreaterThanOrEqualTo(6), "StartMenu should have at least 6 main commands");
    }

    #endregion

    #region ActiveView Management

    [Test]
    public void MainViewModel_ChangeActiveView_ShouldUpdateProperty()
    {
        // Arrange
        var mainViewModel = TestHelper.CreateMainViewModel(_serviceProvider);
        var initialView = mainViewModel.ActiveView;

        // Act
        var newView = new object();
        mainViewModel.ActiveView = newView;

        // Assert
        Assert.That(mainViewModel.ActiveView, Is.EqualTo(newView));
        Assert.That(mainViewModel.ActiveView, Is.Not.EqualTo(initialView));
    }

    [Test]
    public void MainViewModel_ChangeActiveView_ShouldRaisePropertyChanged()
    {
        // Arrange
        var mainViewModel = TestHelper.CreateMainViewModel(_serviceProvider);
        var propertyChangedRaised = false;
        mainViewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(mainViewModel.ActiveView))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        mainViewModel.ActiveView = new object();

        // Assert
        Assert.That(propertyChangedRaised, Is.True, "PropertyChanged event should be raised for ActiveView");
    }

    #endregion

    #region Human Player Management

    [Test]
    public void MainViewModel_HumanPlayer_ShouldBeAccessible()
    {
        // Act
        var mainViewModel = TestHelper.CreateMainViewModel(_serviceProvider);

        // Assert
        Assert.That(mainViewModel.Human, Is.Not.Null);
        Assert.That(mainViewModel.Human.Model, Is.Not.Null);
    }

    #endregion

    #region Settings Management

    [Test]
    public void MainViewModel_Settings_ShouldBeAccessible()
    {
        // Act
        var mainViewModel = TestHelper.CreateMainViewModel(_serviceProvider);

        // Assert
        Assert.That(mainViewModel.Settings, Is.Not.Null);
        Assert.That(mainViewModel.Settings.Model, Is.Not.Null);
    }

    #endregion

    #region Slide Menu

    [Test]
    public void MainViewModel_SlideMenu_ShouldToggle()
    {
        // Arrange
        var mainViewModel = TestHelper.CreateMainViewModel(_serviceProvider);
        var initialState = mainViewModel.IsSlideMenuOpen;

        // Act
        mainViewModel.ShowSlideMenu.Execute("MainMenu");

        // Assert
        Assert.That(mainViewModel.IsSlideMenuOpen, Is.Not.EqualTo(initialState));
    }

    [Test]
    public void MainViewModel_CloseSlideMenu_ShouldSetToFalse()
    {
        // Arrange
        var mainViewModel = TestHelper.CreateMainViewModel(_serviceProvider);
        mainViewModel.ShowSlideMenu.Execute("MainMenu");

        // Act
        mainViewModel.CloseSlideMenu.Execute(null);

        // Assert
        Assert.That(mainViewModel.IsSlideMenuOpen, Is.False);
    }

    [Test]
    public void MainViewModel_ShowSlideMenu_ShouldUpdateStartMenuPage()
    {
        // Arrange
        var mainViewModel = TestHelper.CreateMainViewModel(_serviceProvider);
        var pageName = "TestPage";

        // Act
        mainViewModel.ShowSlideMenu.Execute(pageName);

        // Assert
        Assert.That(mainViewModel.StartMenuPage, Is.EqualTo($"{pageName}.xaml"));
    }

    #endregion

    #region Command Availability

    [Test]
    public void MainViewModel_NewGameCommand_ShouldBeExecutable()
    {
        // Arrange
        var mainViewModel = TestHelper.CreateMainViewModel(_serviceProvider);

        // Act
        var canExecute = mainViewModel.NewGame.CanExecute(null);

        // Assert
        Assert.That(canExecute, Is.True, "NewGame command should be executable");
    }

    [Test]
    public void MainViewModel_AboutCommand_ShouldBeExecutable()
    {
        // Arrange
        var mainViewModel = TestHelper.CreateMainViewModel(_serviceProvider);

        // Act
        var canExecute = mainViewModel.About.CanExecute(null);

        // Assert
        Assert.That(canExecute, Is.True, "About command should be executable");
    }

    #endregion

    #region Lifecycle

    [Test]
    public void MainViewModel_Dispose_ShouldCleanupResources()
    {
        // Arrange
        var mainViewModel = TestHelper.CreateMainViewModel(_serviceProvider);

        // Act & Assert - should not throw
        Assert.DoesNotThrow(() => mainViewModel.Dispose());
    }

    #endregion

    #region PropertyChanged Events

    [Test]
    public void MainViewModel_IsSlideMenuOpen_ShouldRaisePropertyChanged()
    {
        // Arrange
        var mainViewModel = TestHelper.CreateMainViewModel(_serviceProvider);
        var propertyChangedRaised = false;
        mainViewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(mainViewModel.IsSlideMenuOpen))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        mainViewModel.IsSlideMenuOpen = !mainViewModel.IsSlideMenuOpen;

        // Assert
        Assert.That(propertyChangedRaised, Is.True, "PropertyChanged event should be raised for IsSlideMenuOpen");
    }

    [Test]
    public void MainViewModel_StartMenuPage_ShouldRaisePropertyChanged()
    {
        // Arrange
        var mainViewModel = TestHelper.CreateMainViewModel(_serviceProvider);
        var propertyChangedRaised = false;
        mainViewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(mainViewModel.StartMenuPage))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        mainViewModel.StartMenuPage = "NewPage.xaml";

        // Assert
        Assert.That(propertyChangedRaised, Is.True, "PropertyChanged event should be raised for StartMenuPage");
    }

    #endregion
}
