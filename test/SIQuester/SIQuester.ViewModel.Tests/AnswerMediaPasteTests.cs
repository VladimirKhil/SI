using Microsoft.Extensions.DependencyInjection;
using SIPackages;
using SIPackages.Core;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Tests.Helpers;

namespace SIQuester.ViewModel.Tests;

/// <summary>
/// Tests for pasting media files (Ctrl+V) into question answers.
/// </summary>
[TestFixture]
internal sealed class AnswerMediaPasteTests
{
    private IServiceProvider _serviceProvider = null!;
    private IDocumentViewModelFactory _documentFactory = null!;
    private string _tempDirectory = null!;

    [SetUp]
    public void Setup()
    {
        _serviceProvider = TestHelper.CreateServiceProvider();
        _documentFactory = _serviceProvider.GetRequiredService<IDocumentViewModelFactory>();
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        try
        {
            Directory.Delete(_tempDirectory, true);
        }
        catch (IOException)
        {
            // The document may still hold a pending file open
        }
    }

    [Test]
    public void TryPasteMedia_RightAnswer_ShouldCreateMediaAnswer()
    {
        // Arrange
        var question = CreateQuestion();
        var imagePath = CreateFile("pasted.png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        // Act
        var result = question.Right.TryPasteMedia(new[] { imagePath });

        // Assert
        Assert.That(result, Is.True);
        Assert.That(question.Model.Parameters.TryGetValue(QuestionParameterNames.Answer, out var answer), Is.True);
        Assert.That(answer!.ContentValue!.Any(item => item.Type == ContentTypes.Image && item.Value == "pasted.png"), Is.True);
    }

    [Test]
    public void TryPasteMedia_WrongAnswer_ShouldDoNothing()
    {
        // Arrange
        var question = CreateQuestion();
        var imagePath = CreateFile("pasted.png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        // Act
        var result = question.Wrong.TryPasteMedia(new[] { imagePath });

        // Assert - wrong answers are text only
        Assert.That(result, Is.False);
        Assert.That(question.Model.Parameters.TryGetValue(QuestionParameterNames.Answer, out _), Is.False);
    }

    [Test]
    public void TryPasteMedia_UnsupportedFile_ShouldNotLeaveEmptyAnswer()
    {
        // Arrange
        var question = CreateQuestion();
        var textPath = CreateFile("notes.txt", new byte[] { 0x41, 0x42 });

        // Act
        question.Right.TryPasteMedia(new[] { textPath });

        // Assert
        Assert.That(question.Model.Parameters.TryGetValue(QuestionParameterNames.Answer, out _), Is.False);
    }

    private QuestionViewModel CreateQuestion()
    {
        var document = SIDocument.Create("Test Package", "Test Author");
        var round = new Round { Name = "Round 1" };
        var theme = new Theme { Name = "Theme 1" };
        var question = new Question { Price = 100 };

        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem> { new() { Type = ContentTypes.Text, Value = "Question text" } }
        };

        question.Right.Add("Answer");
        theme.Questions.Add(question);
        round.Themes.Add(theme);
        document.Package.Rounds.Add(round);

        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        return qDocument.Package.Rounds[0].Themes[0].Questions[0];
    }

    private string CreateFile(string name, byte[] data)
    {
        var path = Path.Combine(_tempDirectory, name);
        File.WriteAllBytes(path, data);
        return path;
    }
}
