using Microsoft.Extensions.DependencyInjection;
using SIPackages;
using SIPackages.Core;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Tests.Helpers;
using System.Text;

namespace SIQuester.ViewModel.Tests;

/// <summary>
/// Tests for media operations in QDocument ViewModel.
/// </summary>
[TestFixture]
internal sealed class MediaOperationsTests
{
    private IServiceProvider _serviceProvider = null!;
    private IDocumentViewModelFactory _documentFactory = null!;

    [SetUp]
    public void Setup()
    {
        _serviceProvider = TestHelper.CreateServiceProvider();
        _documentFactory = _serviceProvider.GetRequiredService<IDocumentViewModelFactory>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    #region Adding Media Files

    [Test]
    public void AddImageToDocument_ShouldIncreaseImageCount()
    {
        // Arrange
        var document = SIDocument.Create("Test Package", "Test Author");
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var initialCount = qDocument.Images.Files.Count;
        
        // Create a simple test image data
        var imageData = CreateTestImageData();

        // Act
        using (var stream = new MemoryStream(imageData))
        {
            document.Images.AddFile("test_image.png", stream);
        }

        // Assert
        Assert.That(qDocument.Images.Files.Count, Is.EqualTo(initialCount + 1));
        Assert.That(document.Images.GetNames(), Does.Contain("test_image.png"));
    }

    [Test]
    public void AddAudioToDocument_ShouldIncreaseAudioCount()
    {
        // Arrange
        var document = SIDocument.Create("Test Package", "Test Author");
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var initialCount = qDocument.Audio.Files.Count;
        
        // Create a simple test audio data
        var audioData = CreateTestAudioData();

        // Act
        using (var stream = new MemoryStream(audioData))
        {
            document.Audio.AddFile("test_audio.mp3", stream);
        }

        // Assert
        Assert.That(qDocument.Audio.Files.Count, Is.EqualTo(initialCount + 1));
        Assert.That(document.Audio.GetNames(), Does.Contain("test_audio.mp3"));
    }

    [Test]
    public void AddVideoToDocument_ShouldIncreaseVideoCount()
    {
        // Arrange
        var document = SIDocument.Create("Test Package", "Test Author");
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var initialCount = qDocument.Video.Files.Count;
        
        // Create a simple test video data
        var videoData = CreateTestVideoData();

        // Act
        using (var stream = new MemoryStream(videoData))
        {
            document.Video.AddFile("test_video.mp4", stream);
        }

        // Assert
        Assert.That(qDocument.Video.Files.Count, Is.EqualTo(initialCount + 1));
        Assert.That(document.Video.GetNames(), Does.Contain("test_video.mp4"));
    }

    [Test]
    public void AddHtmlToDocument_ShouldIncreaseHtmlCount()
    {
        // Arrange
        var document = SIDocument.Create("Test Package", "Test Author");
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var initialCount = qDocument.Html.Files.Count;
        
        // Create a simple test HTML data
        var htmlData = CreateTestHtmlData();

        // Act
        using (var stream = new MemoryStream(htmlData))
        {
            document.Html.AddFile("test_page.html", stream);
        }

        // Assert
        Assert.That(qDocument.Html.Files.Count, Is.EqualTo(initialCount + 1));
        Assert.That(document.Html.GetNames(), Does.Contain("test_page.html"));
    }

    #endregion

    #region Using Media in Questions

    [Test]
    public void AddImageToQuestion_ShouldReferenceImage()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var question = qDocument.Package.Rounds[0].Themes[0].Questions[0];
        
        // Add image to document
        var imageData = CreateTestImageData();
        using (var stream = new MemoryStream(imageData))
        {
            document.Images.AddFile("question_image.png", stream);
        }

        // Act - Add image reference to question
        var imageAtom = new Atom
        {
            Type = AtomTypes.Image,
            Text = "@question_image.png"
        };
        question.Model.Scenario.Add(imageAtom);

        // Assert
        Assert.That(question.Model.Scenario.Any(a => a.Type == AtomTypes.Image), Is.True);
        Assert.That(question.Model.Scenario.Last().Text, Is.EqualTo("@question_image.png"));
    }

    [Test]
    public void AddAudioToQuestion_ShouldReferenceAudio()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var question = qDocument.Package.Rounds[0].Themes[0].Questions[0];
        
        // Add audio to document
        var audioData = CreateTestAudioData();
        using (var stream = new MemoryStream(audioData))
        {
            document.Audio.AddFile("question_audio.mp3", stream);
        }

        // Act - Add audio reference to question
        var audioAtom = new Atom
        {
            Type = AtomTypes.Audio,
            Text = "@question_audio.mp3"
        };
        question.Model.Scenario.Add(audioAtom);

        // Assert
        Assert.That(question.Model.Scenario.Any(a => a.Type == AtomTypes.Audio), Is.True);
        Assert.That(question.Model.Scenario.Last().Text, Is.EqualTo("@question_audio.mp3"));
    }

    [Test]
    public void AddVideoToQuestion_ShouldReferenceVideo()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var question = qDocument.Package.Rounds[0].Themes[0].Questions[0];
        
        // Add video to document
        var videoData = CreateTestVideoData();
        using (var stream = new MemoryStream(videoData))
        {
            document.Video.AddFile("question_video.mp4", stream);
        }

        // Act - Add video reference to question
        var videoAtom = new Atom
        {
            Type = AtomTypes.Video,
            Text = "@question_video.mp4"
        };
        question.Model.Scenario.Add(videoAtom);

        // Assert
        Assert.That(question.Model.Scenario.Any(a => a.Type == AtomTypes.Video), Is.True);
        Assert.That(question.Model.Scenario.Last().Text, Is.EqualTo("@question_video.mp4"));
    }

    #endregion

    #region Removing Media Files

    [Test]
    public void RemoveImage_ShouldDecreaseImageCount()
    {
        // Arrange
        var document = SIDocument.Create("Test Package", "Test Author");
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        
        // Add an image first
        var imageData = CreateTestImageData();
        using (var stream = new MemoryStream(imageData))
        {
            document.Images.AddFile("removable_image.png", stream);
        }
        var countAfterAdd = qDocument.Images.Files.Count;

        // Act
        document.Images.DeleteFile("removable_image.png");

        // Assert
        Assert.That(qDocument.Images.Files.Count, Is.EqualTo(countAfterAdd - 1));
        Assert.That(document.Images.GetNames(), Does.Not.Contain("removable_image.png"));
    }

    [Test]
    public void RemoveAudio_ShouldDecreaseAudioCount()
    {
        // Arrange
        var document = SIDocument.Create("Test Package", "Test Author");
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        
        // Add an audio first
        var audioData = CreateTestAudioData();
        using (var stream = new MemoryStream(audioData))
        {
            document.Audio.AddFile("removable_audio.mp3", stream);
        }
        var countAfterAdd = qDocument.Audio.Files.Count;

        // Act
        document.Audio.DeleteFile("removable_audio.mp3");

        // Assert
        Assert.That(qDocument.Audio.Files.Count, Is.EqualTo(countAfterAdd - 1));
        Assert.That(document.Audio.GetNames(), Does.Not.Contain("removable_audio.mp3"));
    }

    #endregion

    #region Helper Methods

    private static byte[] CreateTestImageData()
    {
        // Create a minimal valid PNG file (1x1 pixel)
        // This is a simplified PNG, just enough for testing
        var data = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        return data;
    }

    private static byte[] CreateTestAudioData()
    {
        // Create minimal test audio data
        return Encoding.UTF8.GetBytes("FAKE_AUDIO_DATA_FOR_TESTING");
    }

    private static byte[] CreateTestVideoData()
    {
        // Create minimal test video data
        return Encoding.UTF8.GetBytes("FAKE_VIDEO_DATA_FOR_TESTING");
    }

    private static byte[] CreateTestHtmlData()
    {
        // Create minimal test HTML data
        return Encoding.UTF8.GetBytes("<html><body>Test HTML</body></html>");
    }

    #endregion
}
