namespace SIPackages.Tests;

/// <summary>
/// Tests for Theme deserialization from .siq files.
/// </summary>
public sealed class ThemeDeserializationTests
{
    [Test]
    public void Load_OldFormat_Theme_QuestionsAreDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);
        var theme = document.Package.Rounds[0].Themes[0];

        // Assert
        Assert.That(theme.Questions, Has.Count.GreaterThan(0));
        Assert.That(theme.Questions, Has.Count.EqualTo(10));
    }

    [Test]
    public void Load_NewFormat_Theme_QuestionsAreDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var theme = document.Package.Rounds[0].Themes[0];

        // Assert
        Assert.That(theme.Questions, Has.Count.GreaterThan(0));
        Assert.That(theme.Questions, Has.Count.EqualTo(12));
    }

    [Test]
    public void Load_OldFormat_Theme_InfoIsDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);
        var theme = document.Package.Rounds[0].Themes[0];

        // Assert
        Assert.That(theme.Info, Is.Not.Null);
        Assert.That(theme.Info.Comments.Text, Does.Contain("Комментарий к теме"));
    }

    [Test]
    public void Load_NewFormat_Theme_InfoIsDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var theme = document.Package.Rounds[0].Themes[0];

        // Assert
        Assert.That(theme.Info, Is.Not.Null);
        Assert.That(theme.Info.Comments.Text, Does.Contain("Комментарий к теме"));
    }

    [Test]
    public void Load_OldFormat_ContentTheme_HasCorrectQuestionCount()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);
        var contentTheme = document.Package.Rounds[0].Themes[1];

        // Assert
        Assert.That(contentTheme.Name, Is.EqualTo("Контент вопросов"));
        Assert.That(contentTheme.Questions, Has.Count.EqualTo(12));
    }

    [Test]
    public void Load_NewFormat_ContentTheme_HasCorrectQuestionCount()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var contentTheme = document.Package.Rounds[0].Themes[1];

        // Assert
        Assert.That(contentTheme.Name, Is.EqualTo("Контент вопросов"));
        Assert.That(contentTheme.Questions, Has.Count.EqualTo(14));
    }

    [Test]
    public void Load_OldFormat_FinalTheme_HasSingleQuestion()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);
        var finalTheme = document.Package.Rounds[1].Themes[0];

        // Assert
        Assert.That(finalTheme.Questions, Has.Count.EqualTo(1));
        Assert.That(finalTheme.Questions[0].Price, Is.EqualTo(0));
    }

    [Test]
    public void Load_NewFormat_FinalTheme_HasSingleQuestion()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var finalTheme = document.Package.Rounds[1].Themes[0];

        // Assert
        Assert.That(finalTheme.Questions, Has.Count.EqualTo(1));
        Assert.That(finalTheme.Questions[0].Price, Is.EqualTo(0));
    }
}
