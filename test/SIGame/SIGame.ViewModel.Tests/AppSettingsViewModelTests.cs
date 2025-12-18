using SIData;
using SIGame.ViewModel;

namespace SIGame.ViewModel.Tests;

/// <summary>
/// Positive scenario tests for AppSettingsViewModel.
/// Tests settings initialization, property changes, and command execution.
/// </summary>
[TestFixture]
public sealed class AppSettingsViewModelTests
{
    #region Initialization

    [Test]
    public void AppSettingsViewModel_Initialization_ShouldSetupProperties()
    {
        // Arrange
        var appSettings = new AppSettings();

        // Act
        var viewModel = new AppSettingsViewModel(appSettings);

        // Assert
        Assert.That(viewModel, Is.Not.Null);
        Assert.That(viewModel.Model, Is.Not.Null);
        Assert.That(viewModel.TimeSettings, Is.Not.Null);
        Assert.That(viewModel.ThemeSettings, Is.Not.Null);
    }

    [Test]
    public void AppSettingsViewModel_Initialization_ShouldSetupCommands()
    {
        // Arrange
        var appSettings = new AppSettings();

        // Act
        var viewModel = new AppSettingsViewModel(appSettings);

        // Assert
        Assert.That(viewModel.Apply, Is.Not.Null, "Apply command should be initialized");
        Assert.That(viewModel.SetDefault, Is.Not.Null, "SetDefault command should be initialized");
        Assert.That(viewModel.MoveLogs, Is.Not.Null, "MoveLogs command should be initialized");
        Assert.That(viewModel.Export, Is.Not.Null, "Export command should be initialized");
        Assert.That(viewModel.Import, Is.Not.Null, "Import command should be initialized");
    }

    #endregion

    #region Property Changes

    [Test]
    public void AppSettingsViewModel_GameMode_ShouldUpdateProperty()
    {
        // Arrange
        var appSettings = new AppSettings { GameMode = GameModes.Tv };
        var viewModel = new AppSettingsViewModel(appSettings);

        // Act
        viewModel.GameMode = GameModes.Sport;

        // Assert
        Assert.That(viewModel.GameMode, Is.EqualTo(GameModes.Sport));
        Assert.That(viewModel.Model.GameMode, Is.EqualTo(GameModes.Sport));
    }

    [Test]
    public void AppSettingsViewModel_GameMode_ShouldRaisePropertyChanged()
    {
        // Arrange
        var appSettings = new AppSettings { GameMode = GameModes.Tv };
        var viewModel = new AppSettingsViewModel(appSettings);
        var propertyChangedRaised = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(viewModel.GameMode))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        viewModel.GameMode = GameModes.Sport;

        // Assert
        Assert.That(propertyChangedRaised, Is.True, "PropertyChanged event should be raised for GameMode");
    }

    [Test]
    public void AppSettingsViewModel_GameModeHint_ShouldUpdateWhenGameModeChanges()
    {
        // Arrange
        var appSettings = new AppSettings { GameMode = GameModes.Tv };
        var viewModel = new AppSettingsViewModel(appSettings);
        var initialHint = viewModel.GameModeHint;

        // Act
        viewModel.GameMode = GameModes.Sport;
        var newHint = viewModel.GameModeHint;

        // Assert
        Assert.That(newHint, Is.Not.EqualTo(initialHint));
        Assert.That(newHint, Is.Not.Empty);
    }

    [Test]
    public void AppSettingsViewModel_IsEditable_ShouldUpdateProperty()
    {
        // Arrange
        var appSettings = new AppSettings();
        var viewModel = new AppSettingsViewModel(appSettings) { IsEditable = true };

        // Act
        viewModel.IsEditable = false;

        // Assert
        Assert.That(viewModel.IsEditable, Is.False);
    }

    [Test]
    public void AppSettingsViewModel_IsEditable_ShouldRaisePropertyChanged()
    {
        // Arrange
        var appSettings = new AppSettings();
        var viewModel = new AppSettingsViewModel(appSettings);
        var propertyChangedRaised = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(viewModel.IsEditable))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        viewModel.IsEditable = !viewModel.IsEditable;

        // Assert
        Assert.That(propertyChangedRaised, Is.True, "PropertyChanged event should be raised for IsEditable");
    }

    #endregion

    #region TimeSettings

    [Test]
    public void AppSettingsViewModel_TimeSettings_ShouldBeAccessible()
    {
        // Arrange
        var appSettings = new AppSettings();

        // Act
        var viewModel = new AppSettingsViewModel(appSettings);

        // Assert
        Assert.That(viewModel.TimeSettings, Is.Not.Null);
        Assert.That(viewModel.TimeSettings.Model, Is.Not.Null);
    }

    #endregion

    #region ThemeSettings

    [Test]
    public void AppSettingsViewModel_ThemeSettings_ShouldBeAccessible()
    {
        // Arrange
        var appSettings = new AppSettings();

        // Act
        var viewModel = new AppSettingsViewModel(appSettings);

        // Assert
        Assert.That(viewModel.ThemeSettings, Is.Not.Null);
        Assert.That(viewModel.ThemeSettings.Model, Is.Not.Null);
    }

    #endregion

    #region Model Binding

    [Test]
    public void AppSettingsViewModel_Model_ShouldBindToProvidedSettings()
    {
        // Arrange
        var appSettings = new AppSettings { GameMode = GameModes.Sport };

        // Act
        var viewModel = new AppSettingsViewModel(appSettings);

        // Assert
        Assert.That(viewModel.Model, Is.SameAs(appSettings));
        Assert.That(viewModel.GameMode, Is.EqualTo(GameModes.Sport));
    }

    [Test]
    public void AppSettingsViewModel_ModelChanges_ShouldReflectInViewModel()
    {
        // Arrange
        var appSettings = new AppSettings { GameMode = GameModes.Tv };
        var viewModel = new AppSettingsViewModel(appSettings);

        // Act
        appSettings.GameMode = GameModes.Sport;

        // Assert
        Assert.That(viewModel.GameMode, Is.EqualTo(GameModes.Sport));
    }

    #endregion

    #region GameMode Values

    [Test]
    public void AppSettingsViewModel_GameMode_Tv_ShouldSetCorrectly()
    {
        // Arrange
        var appSettings = new AppSettings();
        var viewModel = new AppSettingsViewModel(appSettings);

        // Act
        viewModel.GameMode = GameModes.Tv;

        // Assert
        Assert.That(viewModel.GameMode, Is.EqualTo(GameModes.Tv));
    }

    [Test]
    public void AppSettingsViewModel_GameMode_Sport_ShouldSetCorrectly()
    {
        // Arrange
        var appSettings = new AppSettings();
        var viewModel = new AppSettingsViewModel(appSettings);

        // Act
        viewModel.GameMode = GameModes.Sport;

        // Assert
        Assert.That(viewModel.GameMode, Is.EqualTo(GameModes.Sport));
    }

    #endregion
}
