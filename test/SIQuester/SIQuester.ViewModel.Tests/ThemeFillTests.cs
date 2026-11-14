using Microsoft.Extensions.DependencyInjection;
using SIPackages;
using SIPackages.Core;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Tests.Helpers;

namespace SIQuester.ViewModel.Tests;

/// <summary>
/// Tests for the theme fill counters (questions with text / with a right answer).
/// </summary>
[TestFixture]
internal sealed class ThemeFillTests
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

    [Test]
    public void ThemeCounters_ShouldCountQuestionsWithTextAndAnswer()
    {
        // Arrange: one filled question, one with text only, one empty
        var theme = CreateTheme(
            CreateQuestion(100, "Question text", "Answer"),
            CreateQuestion(200, "Question text", null),
            CreateQuestion(300, null, null));

        // Assert
        Assert.That(theme.Questions, Has.Count.EqualTo(3));
        Assert.That(theme.QuestionsWithTextCount, Is.EqualTo(2));
        Assert.That(theme.QuestionsWithAnswerCount, Is.EqualTo(1));
    }

    [Test]
    public void ThemeCounters_ShouldUpdateWhenQuestionIsAdded()
    {
        // Arrange
        var theme = CreateTheme(CreateQuestion(100, "Question text", "Answer"));

        // Act
        theme.Questions.Add(new QuestionViewModel(CreateQuestion(200, "Another question", "Another answer")));

        // Assert
        Assert.That(theme.QuestionsWithTextCount, Is.EqualTo(2));
        Assert.That(theme.QuestionsWithAnswerCount, Is.EqualTo(2));
    }

    private ThemeViewModel CreateTheme(params Question[] questions)
    {
        var document = SIDocument.Create("Test Package", "Test Author");
        var round = new Round { Name = "Round 1" };
        var theme = new Theme { Name = "Theme 1" };

        foreach (var question in questions)
        {
            theme.Questions.Add(question);
        }

        round.Themes.Add(theme);
        document.Package.Rounds.Add(round);

        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        return qDocument.Package.Rounds[0].Themes[0];
    }

    private static Question CreateQuestion(int price, string? text, string? answer)
    {
        var question = new Question { Price = price };

        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem> { new() { Type = ContentTypes.Text, Value = text ?? "" } }
        };

        if (answer != null)
        {
            question.Right.Add(answer);
        }

        return question;
    }
}
