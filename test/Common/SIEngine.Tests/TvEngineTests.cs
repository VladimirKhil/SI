using NUnit.Framework;
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
            true,
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

        Assert.That(engine.Stage, Is.EqualTo(GameStage.Question));

        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.EndQuestion);
        AssertMove(engine, GameStage.SelectingQuestion);

        engineHandler.SelectQuestion?.Invoke(0, 1);

        Assert.That(engine.Stage, Is.EqualTo(GameStage.Question));

        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.EndQuestion);
        AssertMove(engine, GameStage.SelectingQuestion);

        engineHandler.SelectQuestion?.Invoke(0, 2);

        Assert.That(engine.Stage, Is.EqualTo(GameStage.Question));

        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.EndQuestion);

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

        question.Scenario.Add(new Atom { Type = AtomTypes.Text, Text = "question" });
        question.Scenario.Add(new Atom { Type = AtomTypes.Marker });
        question.Scenario.Add(new Atom { Type = AtomTypes.Audio, Text = "audio.mp3", AtomTime = 10 });

        var question2 = new Question();
        theme.Questions.Add(question2);

        question2.Scenario.Add(new Atom { Type = AtomTypes.Text, Text = "question" });
        question2.Right.Add("right");

        var question3 = new Question();
        theme.Questions.Add(question3);

        question3.Scenario.Add(new Atom { Type = AtomTypes.Text, Text = "question" });
        question3.Scenario.Add(new Atom { Type = AtomTypes.Marker });
        question3.Scenario.Add(new Atom { Type = AtomTypes.Audio, Text = "audio.mp3", AtomTime = 10 });
        question3.Scenario.Add(new Atom { Type = AtomTypes.Text, Text = "answer" });

        return document;
    }

    private static void AssertMove(ISIEngine engine, GameStage stage)
    {
        engine.MoveNext();

        Assert.That(engine.Stage, Is.EqualTo(stage));
    }
}