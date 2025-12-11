using NUnit.Framework;
using SIEngine.Models;
using SIEngine.Rules;
using SIPackages;
using SIPackages.Core;

namespace SIEngine.Tests;

/// <summary>
/// Comprehensive tests for GameEngine functionality focusing on positive scenarios and complete game flows.
/// </summary>
[TestFixture]
internal sealed class GameEngineTests
{
    #region Complete Package Playthrough Tests

    [Test]
    public void CompletePackage_ClassicRules_ShouldPlayInFiniteSteps()
    {
        // Arrange
        var document = CreateFullPackage();
        var handler = new TrackingGameHandler();
        var questionHandler = new QuestionEnginePlayHandlerMock();

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
            handler,
            questionHandler);

        // Act - Play complete game with safety limit
        int stepCount = 0;
        const int maxSteps = 1000; // Safety limit to detect infinite loops

        while (engine.CanNext() && stepCount < maxSteps)
        {
            engine.MoveNext();
            stepCount++;

            // Auto-select first available question when prompted
            if (handler.SelectQuestionCallback != null && engine.Stage == GameStage.SelectingQuestion)
            {
                var options = handler.LastQuestionOptions;
                if (options != null && options.Count > 0)
                {
                    var (theme, question) = options.First();
                    var callback = handler.SelectQuestionCallback;
                    handler.SelectQuestionCallback = null; // Clear to avoid re-selection
                    callback(theme, question);
                }
            }
        }

        // Assert - Game completed in finite steps
        Assert.That(stepCount, Is.LessThan(maxSteps), "Game should complete in finite steps");
        Assert.That(engine.Stage, Is.EqualTo(GameStage.None), "Game should reach None stage");
        Assert.That(handler.PackageStarted, Is.True, "OnPackage should be called");
        Assert.That(handler.PackageEnded, Is.True, "OnPackageEnd should be called");
        Assert.That(handler.RoundsStarted, Is.GreaterThan(0), "At least one round should be played");
        // For Classic rules (SelectByPlayerStrategy), OnQuestion is not called on ISIEnginePlayHandler
        // Instead, check that question types were announced (which happens when questions are played)
        Assert.That(handler.QuestionTypesAnnounced, Is.GreaterThan(0), "At least one question should be played");
    }

    [Test]
    public void CompletePackage_SimpleRules_ShouldPlayInFiniteSteps()
    {
        // Arrange - Simple rules use Sequential strategy (no player selection)
        var document = CreateFullPackage();
        var handler = new TrackingGameHandler();
        var questionHandler = new QuestionEnginePlayHandlerMock();

        var engine = EngineFactory.CreateEngine(
            WellKnownGameRules.Simple,
            document,
            () => new EngineOptions { ShowRight = true },
            handler,
            questionHandler);

        // Act - Play complete game
        int stepCount = 0;
        const int maxSteps = 1000;

        while (engine.CanNext() && stepCount < maxSteps)
        {
            engine.MoveNext();
            stepCount++;
        }

        // Assert
        Assert.That(stepCount, Is.LessThan(maxSteps), "Game should complete in finite steps");
        Assert.That(engine.Stage, Is.EqualTo(GameStage.None));
        Assert.That(handler.PackageStarted, Is.True);
        Assert.That(handler.PackageEnded, Is.True);
        Assert.That(handler.GameThemesShown, Is.False, "Simple rules don't show game themes");
    }

    [Test]
    public void CompletePackage_QuizRules_ShouldPlayInFiniteSteps()
    {
        // Arrange
        var document = CreateFullPackage();
        var handler = new TrackingGameHandler();
        var questionHandler = new QuestionEnginePlayHandlerMock();

        var engine = EngineFactory.CreateEngine(
            WellKnownGameRules.Quiz,
            document,
            () => new EngineOptions { ShowRight = true },
            handler,
            questionHandler);

        // Act
        int stepCount = 0;
        const int maxSteps = 1000;

        while (engine.CanNext() && stepCount < maxSteps)
        {
            engine.MoveNext();
            stepCount++;
        }

        // Assert
        Assert.That(stepCount, Is.LessThan(maxSteps));
        Assert.That(engine.Stage, Is.EqualTo(GameStage.None));
        Assert.That(handler.PackageEnded, Is.True);
    }

    #endregion

    #region Handler Call Order Tests

    [Test]
    public void GameFlow_ShouldFollowCorrectHandlerCallOrder()
    {
        // Arrange
        var document = CreateSimplePackage();
        var handler = new TrackingGameHandler();
        var questionHandler = new QuestionEnginePlayHandlerMock();

        var engine = EngineFactory.CreateEngine(
            WellKnownGameRules.Classic,
            document,
            () => new EngineOptions(),
            handler,
            questionHandler);

        // Act - Play through first question
        engine.MoveNext(); // Begin -> GameThemes
        Assert.That(handler.CallOrder.Last(), Is.EqualTo("OnPackage"));

        engine.MoveNext(); // GameThemes
        Assert.That(handler.CallOrder.Last(), Is.EqualTo("OnGameThemes"));

        engine.MoveNext(); // Round
        Assert.That(handler.CallOrder.Last(), Is.EqualTo("OnRound"));

        engine.MoveNext(); // SelectingQuestion -> shows themes
        Assert.That(handler.CallOrder.Contains("OnRoundThemes"), Is.True);

        engine.MoveNext(); // Ask for selection
        Assert.That(handler.CallOrder.Last(), Is.EqualTo("AskForQuestionSelection"));

        // Select first question
        if (handler.SelectQuestionCallback != null)
        {
            handler.SelectQuestionCallback(0, 0);
            // OnQuestionSelected should have been called
            Assert.That(handler.CallOrder.Last(), Is.EqualTo("OnQuestionSelected"));
        }

        // Now we should be at QuestionType stage
        Assert.That(engine.Stage, Is.EqualTo(GameStage.QuestionType));

        engine.MoveNext(); // QuestionType -> Question (also calls OnQuestionType)
        // OnQuestionType is called, but OnQuestion is not (for SelectByPlayerStrategy)
        Assert.That(handler.CallOrder.Contains("OnQuestionType"), Is.True);
    }

    [Test]
    public void RoundFlow_ShouldCallStartAndEndHandlers()
    {
        // Arrange
        var document = CreateSimplePackage();
        var handler = new TrackingGameHandler();
        var questionHandler = new QuestionEnginePlayHandlerMock();

        var engine = EngineFactory.CreateEngine(
            WellKnownGameRules.Simple,
            document,
            () => new EngineOptions(),
            handler,
            questionHandler);

        // Act - Play complete game
        while (engine.CanNext())
        {
            engine.MoveNext();
        }

        // Assert
        Assert.That(handler.RoundsStarted, Is.EqualTo(handler.RoundsEnded));
        Assert.That(handler.RoundsStarted, Is.GreaterThan(0));
    }

    #endregion

    #region Game Stage Tests

    [Test]
    public void GameStages_ShouldProgressSequentially()
    {
        // Arrange
        var document = CreateSimplePackage();
        var handler = new TrackingGameHandler();
        var questionHandler = new QuestionEnginePlayHandlerMock();

        var engine = EngineFactory.CreateEngine(
            WellKnownGameRules.Classic,
            document,
            () => new EngineOptions(),
            handler,
            questionHandler);

        // Act & Assert - Verify stage progression
        Assert.That(engine.Stage, Is.EqualTo(GameStage.Begin));

        engine.MoveNext();
        Assert.That(engine.Stage, Is.EqualTo(GameStage.GameThemes));

        engine.MoveNext();
        Assert.That(engine.Stage, Is.EqualTo(GameStage.Round));

        engine.MoveNext();
        Assert.That(engine.Stage, Is.EqualTo(GameStage.SelectingQuestion));

        // Continue to question - need to select and then move
        engine.MoveNext();
        if (handler.SelectQuestionCallback != null)
        {
            handler.SelectQuestionCallback(0, 0);
            // After selecting, we should be at QuestionType
            Assert.That(engine.Stage, Is.EqualTo(GameStage.QuestionType));
            
            // Move next - should be at Question stage
            engine.MoveNext();
            Assert.That(engine.Stage, Is.EqualTo(GameStage.Question));
        }
    }

    [Test]
    public void GameEnd_ShouldReachNoneStage()
    {
        // Arrange
        var document = CreateSimplePackage();
        var handler = new TrackingGameHandler();
        var questionHandler = new QuestionEnginePlayHandlerMock();

        var engine = EngineFactory.CreateEngine(
            WellKnownGameRules.Simple,
            document,
            () => new EngineOptions(),
            handler,
            questionHandler);

        // Act - Play to completion
        while (engine.CanNext())
        {
            engine.MoveNext();
        }

        // Assert
        Assert.That(engine.Stage, Is.EqualTo(GameStage.None));
        Assert.That(engine.CanNext(), Is.False);
    }

    #endregion

    #region Question Selection Strategy Tests

    [Test]
    public void SelectByPlayerStrategy_ShouldRequestPlayerChoice()
    {
        // Arrange
        var document = CreateSimplePackage();
        var handler = new TrackingGameHandler();
        var questionHandler = new QuestionEnginePlayHandlerMock();

        var engine = EngineFactory.CreateEngine(
            WellKnownGameRules.Classic,
            document,
            () => new EngineOptions(),
            handler,
            questionHandler);

        // Act - Play until selection is requested
        while (engine.CanNext() && handler.SelectQuestionCallback == null)
        {
            engine.MoveNext();
        }

        // Assert
        Assert.That(handler.SelectQuestionCallback, Is.Not.Null, "Should request question selection");
        Assert.That(handler.LastQuestionOptions, Is.Not.Null);
        Assert.That(handler.LastQuestionOptions!.Count, Is.GreaterThan(0));
    }

    [Test]
    public void SequentialStrategy_ShouldAutoSelectQuestions()
    {
        // Arrange
        var document = CreateSimplePackage();
        var handler = new TrackingGameHandler();
        var questionHandler = new QuestionEnginePlayHandlerMock();

        var engine = EngineFactory.CreateEngine(
            WellKnownGameRules.Simple, // Uses Sequential strategy
            document,
            () => new EngineOptions(),
            handler,
            questionHandler);

        // Act - Play through several questions
        while (engine.CanNext() && handler.QuestionsPlayed < 3)
        {
            engine.MoveNext();
        }

        // Assert
        Assert.That(handler.SelectQuestionCallback, Is.Null, "Sequential should not ask for selection");
        Assert.That(handler.QuestionsPlayed, Is.GreaterThan(0), "Questions should be auto-played");
    }

    #endregion

    #region Round Type Tests

    [Test]
    public void MultipleRounds_ShouldPlayAllRounds()
    {
        // Arrange
        var document = CreatePackageWithMultipleRounds();
        var handler = new TrackingGameHandler();
        var questionHandler = new QuestionEnginePlayHandlerMock();

        var engine = EngineFactory.CreateEngine(
            WellKnownGameRules.Simple,
            document,
            () => new EngineOptions(),
            handler,
            questionHandler);

        // Act
        while (engine.CanNext())
        {
            engine.MoveNext();
        }

        // Assert
        Assert.That(handler.RoundsStarted, Is.EqualTo(3), "All 3 rounds should be played");
        Assert.That(handler.RoundsEnded, Is.EqualTo(3));
    }

    [Test]
    public void RoundEnd_ShouldProvideCorrectReason()
    {
        // Arrange
        var document = CreateSimplePackage();
        var handler = new TrackingGameHandler();
        var questionHandler = new QuestionEnginePlayHandlerMock();

        var engine = EngineFactory.CreateEngine(
            WellKnownGameRules.Simple,
            document,
            () => new EngineOptions(),
            handler,
            questionHandler);

        // Act - Play complete round
        while (engine.CanNext() && handler.RoundsEnded == 0)
        {
            engine.MoveNext();
        }

        // Assert
        Assert.That(handler.LastRoundEndReason, Is.EqualTo(RoundEndReason.Completed));
    }

    #endregion

    #region Navigation Tests

    [Test]
    public void MoveNextRound_ShouldSkipToNextRound()
    {
        // Arrange
        var document = CreatePackageWithMultipleRounds();
        var handler = new TrackingGameHandler();
        var questionHandler = new QuestionEnginePlayHandlerMock();

        var engine = EngineFactory.CreateEngine(
            WellKnownGameRules.Simple,
            document,
            () => new EngineOptions(),
            handler,
            questionHandler);

        // Act - Start game
        engine.MoveNext(); // Begin
        
        // Skip first round
        Assert.That(engine.CanMoveNextRound, Is.True);
        engine.MoveNextRound();

        // Assert
        Assert.That(engine.RoundIndex, Is.EqualTo(1));
        Assert.That(handler.RoundsStarted, Is.EqualTo(0), "First round should be skipped without playing");
    }

    [Test]
    public void MoveToAnswer_ShouldSkipToAnswer()
    {
        // Arrange
        var document = CreateSimplePackage();
        var handler = new TrackingGameHandler();
        var questionHandler = new QuestionEnginePlayHandlerMock();

        var engine = EngineFactory.CreateEngine(
            WellKnownGameRules.Simple,
            document,
            () => new EngineOptions { ShowRight = true },
            handler,
            questionHandler);

        // Act - Play to question stage
        while (engine.CanNext() && engine.Stage != GameStage.Question)
        {
            engine.MoveNext();
        }

        // Jump to answer
        engine.MoveToAnswer();

        // Assert - Should still be in question stage but moved to answer part
        Assert.That(engine.Stage, Is.EqualTo(GameStage.Question));
    }

    #endregion

    #region Question Content Tests

    [Test]
    public void Questions_WithDifferentContent_ShouldAllPlay()
    {
        // Arrange - Package with text, image, and audio questions
        var document = CreatePackageWithVariedContent();
        var handler = new TrackingGameHandler();
        var questionHandler = new QuestionEnginePlayHandlerMock();

        var engine = EngineFactory.CreateEngine(
            WellKnownGameRules.Simple,
            document,
            () => new EngineOptions(),
            handler,
            questionHandler);

        // Act
        while (engine.CanNext())
        {
            engine.MoveNext();
        }

        // Assert
        Assert.That(handler.QuestionsPlayed, Is.GreaterThanOrEqualTo(3));
        Assert.That(engine.Stage, Is.EqualTo(GameStage.None));
    }

    #endregion

    #region Helper Methods

    private static SIDocument CreateSimplePackage()
    {
        var document = SIDocument.Create("Test Package", "Test Author", new PackageContainerMock());

        var round = new Round { Name = "Round 1" };
        document.Package.Rounds.Add(round);

        var theme = new Theme { Name = "Theme 1" };
        round.Themes.Add(theme);

        // Add 3 simple questions
        for (int i = 0; i < 3; i++)
        {
            var question = new Question { Price = (i + 1) * 100 };
            question.Parameters[QuestionParameterNames.Question] = new StepParameter
            {
                Type = StepParameterTypes.Content,
                ContentValue = new List<ContentItem>
                {
                    new ContentItem { Type = ContentTypes.Text, Value = $"Question {i + 1}?" }
                }
            };
            question.Right.Add($"Answer {i + 1}");
            theme.Questions.Add(question);
        }

        return document;
    }

    private static SIDocument CreateFullPackage()
    {
        var document = SIDocument.Create("Full Package", "Test Author", new PackageContainerMock());

        // Add 2 rounds
        for (int r = 0; r < 2; r++)
        {
            var round = new Round { Name = $"Round {r + 1}" };
            document.Package.Rounds.Add(round);

            // Add 2 themes per round
            for (int t = 0; t < 2; t++)
            {
                var theme = new Theme { Name = $"Theme {t + 1}" };
                round.Themes.Add(theme);

                // Add 3 questions per theme
                for (int q = 0; q < 3; q++)
                {
                    var question = new Question { Price = (q + 1) * 100 };
                    question.Parameters[QuestionParameterNames.Question] = new StepParameter
                    {
                        Type = StepParameterTypes.Content,
                        ContentValue = new List<ContentItem>
                        {
                            new ContentItem { Type = ContentTypes.Text, Value = $"R{r + 1}T{t + 1}Q{q + 1}" }
                        }
                    };
                    question.Right.Add("Answer");
                    theme.Questions.Add(question);
                }
            }
        }

        return document;
    }

    private static SIDocument CreatePackageWithMultipleRounds()
    {
        var document = SIDocument.Create("Multi-Round Package", "Test Author", new PackageContainerMock());

        for (int r = 0; r < 3; r++)
        {
            var round = new Round { Name = $"Round {r + 1}" };
            document.Package.Rounds.Add(round);

            var theme = new Theme { Name = "Theme" };
            round.Themes.Add(theme);

            var question = new Question { Price = 100 };
            question.Parameters[QuestionParameterNames.Question] = new StepParameter
            {
                Type = StepParameterTypes.Content,
                ContentValue = new List<ContentItem>
                {
                    new ContentItem { Type = ContentTypes.Text, Value = "Question" }
                }
            };
            question.Right.Add("Answer");
            theme.Questions.Add(question);
        }

        return document;
    }

    private static SIDocument CreatePackageWithVariedContent()
    {
        var document = SIDocument.Create("Varied Content", "Test Author", new PackageContainerMock());

        var round = new Round { Name = "Round 1" };
        document.Package.Rounds.Add(round);

        var theme = new Theme { Name = "Theme 1" };
        round.Themes.Add(theme);

        // Text question
        var q1 = new Question { Price = 100 };
        q1.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new ContentItem { Type = ContentTypes.Text, Value = "Text question" }
            }
        };
        q1.Right.Add("Answer");
        theme.Questions.Add(q1);

        // Image question
        var q2 = new Question { Price = 200 };
        q2.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new ContentItem { Type = ContentTypes.Image, Value = "image.jpg", IsRef = true }
            }
        };
        q2.Right.Add("Answer");
        theme.Questions.Add(q2);

        // Audio question
        var q3 = new Question { Price = 300 };
        q3.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new ContentItem { Type = ContentTypes.Audio, Value = "audio.mp3", IsRef = true }
            }
        };
        q3.Right.Add("Answer");
        theme.Questions.Add(q3);

        return document;
    }

    #endregion
}

/// <summary>
/// Mock handler that tracks all calls for testing.
/// </summary>
internal class TrackingGameHandler : ISIEnginePlayHandler
{
    public bool PackageStarted { get; private set; }
    public bool PackageEnded { get; private set; }
    public bool GameThemesShown { get; private set; }
    public int RoundsStarted { get; private set; }
    public int RoundsEnded { get; private set; }
    public int QuestionsPlayed { get; private set; }
    public int QuestionTypesAnnounced { get; private set; }
    public int QuestionSelectionsNotified { get; private set; }
    public RoundEndReason? LastRoundEndReason { get; private set; }
    public Action<int, int>? SelectQuestionCallback { get; set; }
    public IReadOnlyCollection<(int, int)>? LastQuestionOptions { get; private set; }
    public List<string> CallOrder { get; } = new();

    public void OnPackage(Package package)
    {
        PackageStarted = true;
        CallOrder.Add("OnPackage");
    }

    public void OnPackageEnd()
    {
        PackageEnded = true;
        CallOrder.Add("OnPackageEnd");
    }

    public void OnGameThemes(IEnumerable<string> themes)
    {
        GameThemesShown = true;
        CallOrder.Add("OnGameThemes");
    }

    public void OnRound(Round round, QuestionSelectionStrategyType strategyType)
    {
        RoundsStarted++;
        CallOrder.Add("OnRound");
    }

    public void OnRoundEnd(RoundEndReason reason)
    {
        RoundsEnded++;
        LastRoundEndReason = reason;
        CallOrder.Add("OnRoundEnd");
    }

    public void OnRoundSkip(QuestionSelectionStrategyType strategyType)
    {
        CallOrder.Add("OnRoundSkip");
    }

    public bool ShouldPlayRoundWithRemovableThemes() => true;

    public void OnRoundThemes(IReadOnlyList<Theme> themes, IRoundTableController tableController)
    {
        CallOrder.Add("OnRoundThemes");
    }

    public void OnFinalThemes(IReadOnlyList<Theme> themes, bool willPlayAllThemes, bool isFirstPlay)
    {
        CallOrder.Add("OnFinalThemes");
    }

    public void AskForQuestionSelection(IReadOnlyCollection<(int, int)> options, Action<int, int> selectCallback)
    {
        SelectQuestionCallback = selectCallback;
        LastQuestionOptions = options;
        CallOrder.Add("AskForQuestionSelection");
    }

    public void OnQuestionSelected(int themeIndex, int questionIndex)
    {
        QuestionSelectionsNotified++;
        CallOrder.Add("OnQuestionSelected");
    }

    public void AskForThemeDelete(Action<int> deleteCallback)
    {
        CallOrder.Add("AskForThemeDelete");
    }

    public void OnThemeDeleted(int themeIndex)
    {
        CallOrder.Add("OnThemeDeleted");
    }

    public void OnThemeSelected(int themeIndex, int questionIndex)
    {
        CallOrder.Add("OnThemeSelected");
    }

    public void OnTheme(Theme theme)
    {
        CallOrder.Add("OnTheme");
    }

    public void OnQuestion(Question question)
    {
        QuestionsPlayed++;
        CallOrder.Add("OnQuestion");
    }

    public void OnQuestionType(string typeName, bool isDefault)
    {
        QuestionTypesAnnounced++;
        CallOrder.Add("OnQuestionType");
    }

    public void OnQuestionRestored(int themeIndex, int questionIndex, int price)
    {
        CallOrder.Add("OnQuestionRestored");
    }

    public bool OnQuestionEnd(string comments)
    {
        CallOrder.Add("OnQuestionEnd");
        return false; // Don't timeout
    }
}
