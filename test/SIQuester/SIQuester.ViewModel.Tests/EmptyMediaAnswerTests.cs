using Microsoft.Extensions.DependencyInjection;
using SIPackages;
using SIPackages.Core;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Tests.Helpers;

namespace SIQuester.ViewModel.Tests;

/// <summary>
/// Tests for removing the last item of a media answer.
/// </summary>
[TestFixture]
internal sealed class EmptyMediaAnswerTests
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
    public void RemoveLastAnswerItem_ShouldRemoveAnswer()
    {
        // Arrange
        var (_, question) = CreateQuestion();
        var answerContent = GetContent(question, QuestionParameterNames.Answer);

        // Act
        answerContent.CurrentPosition = 0;
        answerContent.RemoveItem.Execute(null);

        // Assert - no empty answer left: the right answers are used again
        Assert.That(question.Model.Parameters.ContainsKey(QuestionParameterNames.Answer), Is.False);
        Assert.That(question.Model.Right, Is.EqualTo(new[] { "Answer" }));
    }

    [Test]
    public void UndoRemoveLastAnswerItem_ShouldRestoreAnswer()
    {
        // Arrange
        var (qDocument, question) = CreateQuestion();
        var answerContent = GetContent(question, QuestionParameterNames.Answer);
        answerContent.CurrentPosition = 0;
        answerContent.RemoveItem.Execute(null);

        // Act
        qDocument.OperationsManager.Undo.Execute(null);

        // Assert - one undo step brings the answer back with its picture
        Assert.That(question.Model.Parameters.TryGetValue(QuestionParameterNames.Answer, out var answer), Is.True);
        Assert.That(answer!.ContentValue!.Single().Type, Is.EqualTo(ContentTypes.Image));
    }

    [Test]
    public void RemoveLastQuestionItem_ShouldLeaveEmptyText()
    {
        // Arrange
        var (_, question) = CreateQuestion();
        var questionContent = GetContent(question, QuestionParameterNames.Question);

        // Act
        questionContent.CurrentPosition = 0;
        questionContent.RemoveItem.Execute(null);

        // Assert - the question itself keeps an empty text item to type into, as before
        Assert.That(question.Model.Parameters.TryGetValue(QuestionParameterNames.Question, out var content), Is.True);
        var item = content!.ContentValue!.Single();
        Assert.That(item.Type, Is.EqualTo(ContentTypes.Text));
        Assert.That(item.Value, Is.Empty);
    }

    private (QDocument, QuestionViewModel) CreateQuestion()
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

        question.Parameters[QuestionParameterNames.Answer] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem> { new() { Type = ContentTypes.Image, Value = "answer.png", IsRef = true } }
        };

        question.Right.Add("Answer");
        theme.Questions.Add(question);
        round.Themes.Add(theme);
        document.Package.Rounds.Add(round);

        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        return (qDocument, qDocument.Package.Rounds[0].Themes[0].Questions[0]);
    }

    private static ContentItemsViewModel GetContent(QuestionViewModel question, string parameterName)
    {
        foreach (var parameter in question.Parameters)
        {
            if (parameter.Key == parameterName && parameter.Value.ContentValue != null)
            {
                return parameter.Value.ContentValue;
            }
        }

        throw new InvalidOperationException($"No content parameter {parameterName}");
    }
}
