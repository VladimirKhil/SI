using SIPackages.Core;

namespace SIPackages.Tests;

/// <summary>
/// Tests for Round deserialization from .siq files.
/// </summary>
public sealed class RoundDeserializationTests
{
    [Test]
    public void Load_OldFormat_FirstRound_ThemesAreDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);
        var round = document.Package.Rounds[0];

        // Assert
        Assert.That(round.Themes, Has.Count.EqualTo(2));
        Assert.That(round.Themes[0].Name, Is.EqualTo("Вопросы разных типов"));
        Assert.That(round.Themes[1].Name, Is.EqualTo("Контент вопросов"));
    }

    [Test]
    public void Load_NewFormat_FirstRound_ThemesAreDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var round = document.Package.Rounds[0];

        // Assert
        Assert.That(round.Themes, Has.Count.EqualTo(3));
        Assert.That(round.Themes[0].Name, Is.EqualTo("Вопросы разных типов"));
        Assert.That(round.Themes[1].Name, Is.EqualTo("Контент вопросов"));
        Assert.That(round.Themes[2].Name, Is.EqualTo("Дополнительно"));
    }

    [Test]
    public void Load_OldFormat_FinalRound_TypeIsCorrect()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);
        var finalRound = document.Package.Rounds[1];

        // Assert
        Assert.That(finalRound.Type, Is.EqualTo(RoundTypes.Final));
        Assert.That(finalRound.Name, Is.EqualTo("Финал"));
    }

    [Test]
    public void Load_NewFormat_FinalRound_TypeIsCorrect()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var finalRound = document.Package.Rounds[1];

        // Assert
        Assert.That(finalRound.Type, Is.EqualTo(RoundTypes.Final));
        Assert.That(finalRound.Name, Is.EqualTo("Финал"));
    }

    [Test]
    public void Load_OldFormat_FinalRound_ThemesAreDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);
        var finalRound = document.Package.Rounds[1];

        // Assert
        Assert.That(finalRound.Themes, Has.Count.EqualTo(3));
        Assert.That(finalRound.Themes[0].Name, Is.EqualTo("Тема 1"));
        Assert.That(finalRound.Themes[1].Name, Is.EqualTo("Тема 2"));
        Assert.That(finalRound.Themes[2].Name, Is.EqualTo("Тема 3"));
    }

    [Test]
    public void Load_NewFormat_FinalRound_ThemesAreDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var finalRound = document.Package.Rounds[1];

        // Assert
        Assert.That(finalRound.Themes, Has.Count.EqualTo(3));
        Assert.That(finalRound.Themes[0].Name, Is.EqualTo("Тема 1"));
        Assert.That(finalRound.Themes[1].Name, Is.EqualTo("Тема 2"));
        Assert.That(finalRound.Themes[2].Name, Is.EqualTo("Тема 3"));
    }

    [Test]
    public void Load_OldFormat_Round_InfoIsDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);
        var round = document.Package.Rounds[0];

        // Assert
        Assert.That(round.Info, Is.Not.Null);
        Assert.That(round.Info.Comments.Text, Does.Contain("обычный раунд"));
    }

    [Test]
    public void Load_NewFormat_Round_InfoIsDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var round = document.Package.Rounds[0];

        // Assert
        Assert.That(round.Info, Is.Not.Null);
        Assert.That(round.Info.Comments.Text, Does.Contain("обычный раунд"));
    }
}
