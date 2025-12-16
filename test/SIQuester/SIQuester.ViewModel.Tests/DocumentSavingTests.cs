using Microsoft.Extensions.DependencyInjection;
using SIPackages;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Tests.Helpers;

namespace SIQuester.ViewModel.Tests;

/// <summary>
/// Tests for document saving operations in QDocument ViewModel.
/// </summary>
[TestFixture]
internal sealed class DocumentSavingTests
{
    private IServiceProvider _serviceProvider = null!;
    private IDocumentViewModelFactory _documentFactory = null!;
    private string _testDirectory = null!;

    [SetUp]
    public void Setup()
    {
        _serviceProvider = TestHelper.CreateServiceProvider();
        _documentFactory = _serviceProvider.GetRequiredService<IDocumentViewModelFactory>();
        _testDirectory = Path.Combine(Path.GetTempPath(), "SIQuester.Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    #region Save Operations

    [Test]
    public async Task SaveDocument_ToNewFile_ShouldCreateFile()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var filePath = Path.Combine(_testDirectory, "test_package.siq");
        qDocument.Path = filePath;

        // Act
        await qDocument.Save.ExecuteAsync(null);

        // Assert
        Assert.That(File.Exists(filePath), Is.True, "Saved file should exist");
    }

    [Test]
    public async Task SaveDocument_WithChanges_ShouldPersistChanges()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var filePath = Path.Combine(_testDirectory, "test_package_modified.siq");
        qDocument.Path = filePath;

        // Modify the document
        qDocument.Package.Model.Name = "Modified Package Name";

        // Act - Save the document
        await qDocument.Save.ExecuteAsync(null);

        // Assert - File should exist
        Assert.That(File.Exists(filePath), Is.True);

        // Load the document again to verify changes were saved
        using var stream = File.OpenRead(filePath);
        var loadedDocument = SIDocument.Load(stream);
        Assert.That(loadedDocument.Package.Name, Is.EqualTo("Modified Package Name"));
    }

    [Test]
    public async Task SaveDocument_AfterSave_ShouldClearChangedFlag()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var filePath = Path.Combine(_testDirectory, "test_clear_flag.siq");
        qDocument.Path = filePath;
        
        // Make a change
        qDocument.Package.Model.Name = "Modified Name";

        // Act
        await qDocument.Save.ExecuteAsync(null);

        // Assert
        Assert.That(qDocument.Changed, Is.False, "Changed flag should be cleared after save");
    }

    #endregion

    #region SaveAs Operations

    [Test]
    public async Task SaveAsDocument_ToDifferentPath_ShouldCreateNewFile()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var originalPath = Path.Combine(_testDirectory, "original.siq");
        var newPath = Path.Combine(_testDirectory, "saved_as.siq");
        
        qDocument.Path = originalPath;
        await qDocument.Save.ExecuteAsync(null);

        // Act - SaveAs to new path
        qDocument.Path = newPath;
        await qDocument.Save.ExecuteAsync(null);

        // Assert - Both files should exist
        Assert.That(File.Exists(originalPath), Is.True, "Original file should exist");
        Assert.That(File.Exists(newPath), Is.True, "New file should exist");
    }

    #endregion

    #region Save with Media

    [Test]
    public async Task SaveDocument_WithImages_ShouldIncludeMedia()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var filePath = Path.Combine(_testDirectory, "package_with_image.siq");
        qDocument.Path = filePath;

        // Add an image
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        using (var stream = new MemoryStream(imageData))
        {
            document.Images.AddFile("test_image.png", stream);
        }

        // Act
        await qDocument.Save.ExecuteAsync(null);

        // Assert - Reload and check if image is present
        using var fileStream = File.OpenRead(filePath);
        var loadedDocument = SIDocument.Load(fileStream);
        Assert.That(loadedDocument.Images.GetNames(), Does.Contain("test_image.png"));
    }

    [Test]
    public async Task SaveDocument_WithAudio_ShouldIncludeMedia()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var filePath = Path.Combine(_testDirectory, "package_with_audio.siq");
        qDocument.Path = filePath;

        // Add audio
        var audioData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        using (var stream = new MemoryStream(audioData))
        {
            document.Audio.AddFile("test_audio.mp3", stream);
        }

        // Act
        await qDocument.Save.ExecuteAsync(null);

        // Assert - Reload and check if audio is present
        using var fileStream = File.OpenRead(filePath);
        var loadedDocument = SIDocument.Load(fileStream);
        Assert.That(loadedDocument.Audio.GetNames(), Does.Contain("test_audio.mp3"));
    }

    #endregion

    #region Save Validation

    [Test]
    public async Task SaveDocument_WithComplexStructure_ShouldPreserveAllData()
    {
        // Arrange
        var document = SIDocument.Create("Complex Package", "Test Author");
        
        // Create multiple rounds, themes, and questions
        for (int r = 0; r < 2; r++)
        {
            var round = new Round { Name = $"Round {r + 1}" };
            for (int t = 0; t < 3; t++)
            {
                var theme = new Theme { Name = $"Theme {t + 1}" };
                for (int q = 0; q < 5; q++)
                {
                    var question = new Question { Price = (q + 1) * 100 };
                    question.Scenario.Add(new Atom { Text = $"Question {q + 1}" });
                    question.Right.Add($"Answer {q + 1}");
                    theme.Questions.Add(question);
                }
                round.Themes.Add(theme);
            }
            document.Package.Rounds.Add(round);
        }

        var qDocument = _documentFactory.CreateViewModelFor(document, "Complex Package");
        var filePath = Path.Combine(_testDirectory, "complex_package.siq");
        qDocument.Path = filePath;

        // Act
        await qDocument.Save.ExecuteAsync(null);

        // Assert - Reload and verify structure
        using var fileStream = File.OpenRead(filePath);
        var loadedDocument = SIDocument.Load(fileStream);
        
        Assert.That(loadedDocument.Package.Rounds.Count, Is.EqualTo(2));
        Assert.That(loadedDocument.Package.Rounds[0].Themes.Count, Is.EqualTo(3));
        Assert.That(loadedDocument.Package.Rounds[0].Themes[0].Questions.Count, Is.EqualTo(5));
        Assert.That(loadedDocument.Package.Rounds[1].Themes[2].Questions[4].Price, Is.EqualTo(500));
    }

    #endregion

    #region Error Handling

    [Test]
    public void SaveDocument_WithInvalidPath_ShouldHandleGracefully()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        
        // Set invalid path (empty)
        qDocument.Path = string.Empty;

        // Act & Assert - Should not throw, but may fail gracefully
        Assert.DoesNotThrowAsync(async () => await qDocument.Save.ExecuteAsync(null));
    }

    #endregion
}
