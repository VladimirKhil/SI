using SIQuester.Model;

namespace SIQuester.ViewModel.Tests;

/// <summary>
/// Tests for changing the document text size (A− / A+ buttons, Ctrl+wheel, Ctrl+Plus / Ctrl+Minus / Ctrl+0).
/// </summary>
[TestFixture]
internal sealed class FontSizeTests
{
    [Test]
    public void ChangeFontSize_ShouldChangeByDelta()
    {
        var settings = new AppSettings { FontSize = 15 };

        settings.ChangeFontSize(+1);
        Assert.That(settings.FontSize, Is.EqualTo(16));

        settings.ChangeFontSize(-2);
        Assert.That(settings.FontSize, Is.EqualTo(14));
    }

    [Test]
    public void ChangeFontSize_ShouldStayWithinLimits()
    {
        var settings = new AppSettings { FontSize = AppSettings.MaxFontSize };

        settings.ChangeFontSize(+1);
        Assert.That(settings.FontSize, Is.EqualTo(AppSettings.MaxFontSize));

        settings.FontSize = AppSettings.MinFontSize;
        settings.ChangeFontSize(-1);
        Assert.That(settings.FontSize, Is.EqualTo(AppSettings.MinFontSize));
    }

    [Test]
    public void ResetFontSize_ShouldRestoreDefault()
    {
        var settings = new AppSettings { FontSize = 30 };

        settings.ResetFontSize();

        Assert.That(settings.FontSize, Is.EqualTo(AppSettings.DefaultFontSize));
    }
}
