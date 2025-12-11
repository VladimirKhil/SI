using SIPackages.Core;

namespace SIPackages.Tests;

/// <summary>
/// Tests for SIDocument deserialization from .siq files.
/// </summary>
public sealed class SIDocumentDeserializationTests
{
    [Test]
    public void Load_OldFormat_DeserializesSuccessfully()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);

        // Assert
        Assert.That(document, Is.Not.Null);
        Assert.That(document.Package, Is.Not.Null);
    }

    [Test]
    public void Load_NewFormat_DeserializesSuccessfully()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);

        // Assert
        Assert.That(document, Is.Not.Null);
        Assert.That(document.Package, Is.Not.Null);
    }

    [Test]
    public void Load_OldFormat_PackageMetadata_IsCorrect()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);
        var package = document.Package;

        // Assert - Note: old format (version 4) is automatically upgraded to version 5.0 when loaded
        Assert.Multiple(() =>
        {
            Assert.That(package.Name, Is.EqualTo("Тестовый пакет SIGame"));
            Assert.That(package.Version, Is.EqualTo(5.0));
            Assert.That(package.ID, Is.EqualTo("a16f96e7-4616-47d0-8652-aee5196094af"));
            Assert.That(package.Restriction, Is.EqualTo("12+"));
            Assert.That(package.Date, Is.EqualTo("07.07.2022"));
            Assert.That(package.Publisher, Is.EqualTo("Группа, которая владеет пакетом"));
            Assert.That(package.Difficulty, Is.EqualTo(5));
            Assert.That(package.Language, Is.EqualTo("ru-RU"));
        });
    }

    [Test]
    public void Load_NewFormat_PackageMetadata_IsCorrect()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var package = document.Package;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(package.Name, Is.EqualTo("Тестовый пакет SIGame"));
            Assert.That(package.Version, Is.EqualTo(5.0));
            Assert.That(package.ID, Is.EqualTo("a16f96e7-4616-47d0-8652-aee5196094af"));
            Assert.That(package.Restriction, Is.EqualTo("12+"));
            Assert.That(package.Date, Is.EqualTo("07.07.2022"));
            Assert.That(package.Publisher, Is.EqualTo("Группа, которая владеет пакетом"));
            Assert.That(package.Difficulty, Is.EqualTo(5));
            Assert.That(package.Language, Is.EqualTo("ru-RU"));
        });
    }

    [Test]
    public void Load_OldFormat_Tags_AreDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);
        var package = document.Package;

        // Assert
        Assert.That(package.Tags, Has.Count.EqualTo(2));
        Assert.That(package.Tags[0], Is.EqualTo("Тема пакета 1"));
        Assert.That(package.Tags[1], Is.EqualTo("Тема пакета 2"));
    }

    [Test]
    public void Load_NewFormat_Tags_AreDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var package = document.Package;

        // Assert
        Assert.That(package.Tags, Has.Count.EqualTo(2));
        Assert.That(package.Tags[0], Is.EqualTo("Тема пакета 1"));
        Assert.That(package.Tags[1], Is.EqualTo("Тема пакета 2"));
    }

    [Test]
    public void Load_OldFormat_Rounds_AreDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);
        var package = document.Package;

        // Assert
        Assert.That(package.Rounds, Has.Count.EqualTo(2));
        Assert.That(package.Rounds[0].Name, Is.EqualTo("1-й раунд"));
        Assert.That(package.Rounds[1].Name, Is.EqualTo("Финал"));
        Assert.That(package.Rounds[1].Type, Is.EqualTo(RoundTypes.Final));
    }

    [Test]
    public void Load_NewFormat_Rounds_AreDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var package = document.Package;

        // Assert - Note: round name is "1" not "1-й раунд" in the new format file
        Assert.That(package.Rounds, Has.Count.EqualTo(2));
        Assert.That(package.Rounds[0].Name, Is.EqualTo("1"));
        Assert.That(package.Rounds[1].Name, Is.EqualTo("Финал"));
        Assert.That(package.Rounds[1].Type, Is.EqualTo(RoundTypes.Final));
    }

    [Test]
    public void Load_OldFormat_Images_AreAvailable()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);

        // Assert
        var images = document.Images.ToArray();
        Assert.That(images.Length, Is.GreaterThan(0));
        Assert.That(images, Does.Contain("sigame_-_ava_2019.png"));
        Assert.That(images, Does.Contain("sample-boat-400x300.png"));
    }

    [Test]
    public void Load_NewFormat_Images_AreAvailable()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);

        // Assert
        var images = document.Images.ToArray();
        Assert.That(images.Length, Is.GreaterThan(0));
        Assert.That(images, Does.Contain("sigame_-_ava_2019.png"));
        Assert.That(images, Does.Contain("sample-boat-400x300.png"));
        Assert.That(images, Does.Contain("answer.png"));
    }

    [Test]
    public void Load_OldFormat_Audio_AreAvailable()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);

        // Assert
        var audio = document.Audio.ToArray();
        Assert.That(audio.Length, Is.GreaterThan(0));
        Assert.That(audio, Does.Contain("sample-3s.mp3"));
    }

    [Test]
    public void Load_NewFormat_Audio_AreAvailable()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);

        // Assert
        var audio = document.Audio.ToArray();
        Assert.That(audio.Length, Is.GreaterThan(0));
        Assert.That(audio, Does.Contain("sample-3s.mp3"));
    }

    [Test]
    public void Load_OldFormat_Video_AreAvailable()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);

        // Assert
        var video = document.Video.ToArray();
        Assert.That(video.Length, Is.GreaterThan(0));
        Assert.That(video, Does.Contain("Big_Buck_Bunny_1080_10s_1MB.mp4"));
    }

    [Test]
    public void Load_NewFormat_Video_AreAvailable()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);

        // Assert
        var video = document.Video.ToArray();
        Assert.That(video.Length, Is.GreaterThan(0));
        Assert.That(video, Does.Contain("Big_Buck_Bunny_1080_10s_1MB.mp4"));
    }

    [Test]
    public void Load_NewFormat_Html_AreAvailable()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);

        // Assert
        var html = document.Html.ToArray();
        Assert.That(html.Length, Is.GreaterThan(0));
        Assert.That(html, Does.Contain("test.html"));
    }

    [Test]
    public void Load_OldFormat_ImageFile_CanBeRetrieved()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);

        // Assert
        var imageFile = document.Images.GetFile("sample-boat-400x300.png");
        Assert.That(imageFile, Is.Not.Null);
        Assert.That(imageFile.Stream, Is.Not.Null);
        // Note: Cannot check Stream.Length on compressed streams
    }

    [Test]
    public void Load_NewFormat_ImageFile_CanBeRetrieved()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);

        // Assert
        var imageFile = document.Images.GetFile("sample-boat-400x300.png");
        Assert.That(imageFile, Is.Not.Null);
        Assert.That(imageFile.Stream, Is.Not.Null);
        // Note: Cannot check Stream.Length on compressed streams
    }
}
