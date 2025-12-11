using NUnit.Framework;
using SIEngine.Rules;
using SIPackages;
using SIPackages.Core;

namespace SIEngine.Tests;

[TestFixture]
internal sealed class TvEngineTests
{
    [Test]
    public void CommonTest()
    {
        var document = CreateDocument();
        var engineHandler = new SIEnginePlayHandlerMock();

        var engine = EngineFactory.CreateEngine(
            WellKnownGameRules.Classic,
            document,
            () => new EngineOptions
            {
                ShowRight = true,
                PlaySpecials = true,
                IsPressMode = true,
                IsMultimediaPressMode = true,
            },
            engineHandler,
            new QuestionEnginePlayHandlerMock());

        Assert.That(engine.Stage, Is.EqualTo(GameStage.Begin));

        AssertMove(engine, GameStage.GameThemes);
        AssertMove(engine, GameStage.Round);
        AssertMove(engine, GameStage.SelectingQuestion);
        AssertMove(engine, GameStage.SelectingQuestion);
        AssertMove(engine, GameStage.SelectingQuestion);

        engineHandler.SelectQuestion?.Invoke(0, 0);

        Assert.That(engine.Stage, Is.EqualTo(GameStage.QuestionType));

        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.SelectingQuestion);

        engineHandler.SelectQuestion?.Invoke(0, 1);

        Assert.That(engine.Stage, Is.EqualTo(GameStage.QuestionType));

        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.SelectingQuestion);

        engineHandler.SelectQuestion?.Invoke(0, 2);

        Assert.That(engine.Stage, Is.EqualTo(GameStage.QuestionType));

        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.Question);

        AssertMove(engine, GameStage.EndRound);
        AssertMove(engine, GameStage.EndGame);
        AssertMove(engine, GameStage.None);
    }

    private static SIDocument CreateDocument()
    {
        var document = SIDocument.Create("test", "author", new PackageContainerMock());

        var round = new Round();
        document.Package.Rounds.Add(round);

        var theme = new Theme();
        round.Themes.Add(theme);

        var question = new Question();
        theme.Questions.Add(question);
        
        SetQuestionPart(question, new ContentItem() { Type = ContentTypes.Text, Value = "question" });
        SetAnswerPart(question, new ContentItem() { Type = ContentTypes.Audio, Value = "audio.mp3", Duration = TimeSpan.FromSeconds(10) });

        var question2 = new Question();
        theme.Questions.Add(question2);

        SetQuestionPart(question2, new ContentItem() { Type = ContentTypes.Text, Value = "question" });
        question2.Right.Add("right");

        var question3 = new Question();
        theme.Questions.Add(question3);
        
        SetQuestionPart(question3, new ContentItem() { Type = ContentTypes.Text, Value = "question" });
        
        SetAnswerPart(
            question,
            new ContentItem() { Type = ContentTypes.Audio, Value = "audio.mp3", Duration = TimeSpan.FromSeconds(10) },
            new ContentItem() { Type = ContentTypes.Text, Value = "answer" });

        return document;
    }

    private static void SetQuestionPart(Question question, params ContentItem[] contentItems)
    {
        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>(contentItems)
        };
    }

    private static void SetAnswerPart(Question question, params ContentItem[] contentItems)
    {
        question.Parameters[QuestionParameterNames.Answer] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>(contentItems)
        };
    }

    private static void AssertMove(GameEngine engine, GameStage stage)
    {
        engine.MoveNext();

        Assert.That(engine.Stage, Is.EqualTo(stage));
    }
}