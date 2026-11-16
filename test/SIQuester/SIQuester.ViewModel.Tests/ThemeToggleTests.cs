using SIQuester.Model;

namespace SIQuester.ViewModel.Tests;

/// <summary>
/// Tests for the light / dark theme button.
/// </summary>
[TestFixture]
internal sealed class ThemeToggleTests
{
    [Test]
    public void ToggleTheme_SwitchesLightAndDark()
    {
        var settings = AppSettings.Create();
        settings.Theme = ThemeOption.Light;

        settings.ToggleTheme();
        Assert.That(settings.Theme, Is.EqualTo(ThemeOption.Dark));

        settings.ToggleTheme();
        Assert.That(settings.Theme, Is.EqualTo(ThemeOption.Light));
    }

    [Test]
    public void ToggleTheme_ReturnsToDarkGray()
    {
        var settings = AppSettings.Create();
        settings.Theme = ThemeOption.DarkGray;

        settings.ToggleTheme();
        Assert.That(settings.Theme, Is.EqualTo(ThemeOption.Light));

        settings.ToggleTheme();
        Assert.That(settings.Theme, Is.EqualTo(ThemeOption.DarkGray));
    }
}
