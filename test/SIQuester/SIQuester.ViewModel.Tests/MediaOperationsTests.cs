using Microsoft.Extensions.DependencyInjection;
using SIPackages;
using SIPackages.Core;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Tests.Helpers;

namespace SIQuester.ViewModel.Tests;

/// <summary>
/// Tests for media operations in QDocument ViewModel.
/// Tests simulate user behavior by adding and using media files in questions.
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
    public async Task AddImageToDocument_ShouldIncreaseImageCount()
    {
        // Arrange
        var document = SIDocument.Create("Test Package", "Test Author");
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var initialCount = qDocument.Images.Files.Count;
        
        // Create a simple test image data (minimal PNG header)
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        // Act
        using (var stream = new MemoryStream(imageData))
        {
            await document.Images.AddFileAsync("test_image.png", stream);
        }

        // Assert
        Assert.That(qDocument.Images.Files.Count, Is.EqualTo(initialCount + 1));
        Assert.That(document.Images, Does.Contain("test_image.png"));
    }

    [Test]
    public async Task AddAudioToDocument_ShouldIncreaseAudioCount()
    {
        // Arrange
        var document = SIDocument.Create("Test Package", "Test Author");
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var initialCount = qDocument.Audio.Files.Count;
        
        // Create test audio data
        var audioData = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        // Act
        using (var stream = new MemoryStream(audioData))
        {
            await document.Audio.AddFileAsync("test_audio.mp3", stream);
        }

        // Assert
        Assert.That(qDocument.Audio.Files.Count, Is.EqualTo(initialCount + 1));
        Assert.That(document.Audio, Does.Contain("test_audio.mp3"));
    }

    [Test]
    public async Task AddVideoToDocument_ShouldIncreaseVideoCount()
    {
        // Arrange
        var document = SIDocument.Create("Test Package", "Test Author");
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var initialCount = qDocument.Video.Files.Count;
        
        // Create test video data
        var videoData = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        // Act
        using (var stream = new MemoryStream(videoData))
        {
            await document.Video.AddFileAsync("test_video.mp4", stream);
        }

        // Assert
        Assert.That(qDocument.Video.Files.Count, Is.EqualTo(initialCount + 1));
        Assert.That(document.Video, Does.Contain("test_video.mp4"));
    }

    #endregion

    #region Using Media in Questions

    [Test]
    public async Task AddImageToQuestion_ShouldReferenceImage()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var question = qDocument.Package.Rounds[0].Themes[0].Questions[0];
        
        // Add image to document
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        using (var stream = new MemoryStream(imageData))
        {
            await document.Images.AddFileAsync("question_image.png", stream);
        }

        // Act - Add image reference to question using Script
        var imageStep = new Step
        {
            Type = StepTypes.ShowContent,
            Parameters =
            {
                [StepParameterNames.Content] = new StepParameter
                {
                    Type = StepParameterTypes.Content,
                    ContentValue = new List<ContentItem>
                    {
                        new() { Type = ContentTypes.Image, Value = "@question_image.png" }
                    }
                }
            }
        };
        question.Model.Script.Steps.Add(imageStep);

        // Assert
        var content = question.Model.GetContent().ToList();
        Assert.That(content.Any(c => c.Type == ContentTypes.Image), Is.True);
        Assert.That(content.Last().Value, Is.EqualTo("@question_image.png"));
    }

    [Test]
    public async Task AddAudioToQuestion_ShouldReferenceAudio()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var question = qDocument.Package.Rounds[0].Themes[0].Questions[0];
        
        // Add audio to document
        var audioData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        using (var stream = new MemoryStream(audioData))
        {
            await document.Audio.AddFileAsync("question_audio.mp3", stream);
        }

        // Act - Add audio reference to question
        var audioStep = new Step
        {
            Type = StepTypes.ShowContent,
            Parameters =
            {
                [StepParameterNames.Content] = new StepParameter
                {
                    Type = StepParameterTypes.Content,
                    ContentValue = new List<ContentItem>
                    {
                        new() { Type = ContentTypes.Audio, Value = "@question_audio.mp3" }
                    }
                }
            }
        };
        question.Model.Script.Steps.Add(audioStep);

        // Assert
        var content = question.Model.GetContent().ToList();
        Assert.That(content.Any(c => c.Type == ContentTypes.Audio), Is.True);
        Assert.That(content.Last().Value, Is.EqualTo("@question_audio.mp3"));
    }

    #endregion

    #region Removing Media Files

    [Test]
    public async Task RemoveImage_ShouldDecreaseImageCount()
    {
        // Arrange
        var document = SIDocument.Create("Test Package", "Test Author");
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        
        // Add an image first
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        using (var stream = new MemoryStream(imageData))
        {
            await document.Images.AddFileAsync("removable_image.png", stream);
        }
        var countAfterAdd = qDocument.Images.Files.Count;

        // Act
        document.Images.RemoveFile("removable_image.png");

        // Assert
        Assert.That(qDocument.Images.Files.Count, Is.EqualTo(countAfterAdd - 1));
        Assert.That(document.Images, Does.Not.Contain("removable_image.png"));
    }

    [Test]
    public async Task RemoveAudio_ShouldDecreaseAudioCount()
    {
        // Arrange
        var document = SIDocument.Create("Test Package", "Test Author");
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        
        // Add audio first
        var audioData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        using (var stream = new MemoryStream(audioData))
        {
            await document.Audio.AddFileAsync("removable_audio.mp3", stream);
        }
        var countAfterAdd = qDocument.Audio.Files.Count;

        // Act
        document.Audio.RemoveFile("removable_audio.mp3");

        // Assert
        Assert.That(qDocument.Audio.Files.Count, Is.EqualTo(countAfterAdd - 1));
        Assert.That(document.Audio, Does.Not.Contain("removable_audio.mp3"));
    }

    #endregion
}
