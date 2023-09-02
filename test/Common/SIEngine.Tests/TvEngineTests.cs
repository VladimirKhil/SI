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

        var engine = EngineFactory.CreateEngine(
            true,
            document,
            () => new EngineOptions
            {
                ShowRight = true,
                ShowScore = false,
                AutomaticGame = false,
                PlaySpecials = true,
                ThinkingTime = 3,
                IsPressMode = true,
                IsMultimediaPressMode = true,
            },
            new SIEnginePlayHandlerMock(),
            new QuestionEnginePlayHandlerMock());

        Assert.AreEqual(GameStage.Begin, engine.Stage);

        AssertMove(engine, GameStage.GameThemes);
        AssertMove(engine, GameStage.Round);
        AssertMove(engine, GameStage.RoundThemes);
        AssertMove(engine, GameStage.RoundTable);

        engine.SelectQuestion(0, 0);

        Assert.AreEqual(GameStage.Question, engine.Stage);

        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.EndQuestion);
        AssertMove(engine, GameStage.RoundTable);

        engine.SelectQuestion(0, 1);

        Assert.AreEqual(GameStage.Question, engine.Stage);

        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.EndQuestion);
        AssertMove(engine, GameStage.RoundTable);

        engine.SelectQuestion(0, 2);

        Assert.AreEqual(GameStage.Question, engine.Stage);

        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.Question);
        AssertMove(engine, GameStage.EndQuestion);

        AssertMove(engine, GameStage.End);
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

        Assert.AreEqual(stage, engine.Stage);
    }
}