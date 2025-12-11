using SIPackages;
using SIPackages.Core;

namespace SIEngine.Core.Tests;

/// <summary>
/// Tests for QuestionEngine functionality using library scripts from ScriptsLibrary.
/// </summary>
[TestFixture]
public sealed class QuestionEngineTests
{
    #region Basic Question Flow Tests

    [Test]
    public void SimpleTextQuestion_ShouldPlayCompleteFlow()
    {
        // Arrange
        var question = CreateSimpleTextQuestion("What is 2+2?", "4");
        var handler = new TestQuestionEnginePlayHandler();
        var options = CreateDefaultOptions();
        options.ShowSimpleRightAnswers = true;
        var engine = new QuestionEngine(question, options, handler);

        // Act - Play through entire question
        int stepCount = 0;
        while (engine.PlayNext() && stepCount < 20) // Safety limit
        {
            stepCount++;
        }

        // Assert - Verify key handlers were called
        Assert.That(handler.QuestionStartCalled, Is.True, "OnQuestionStart should be called");
        Assert.That(handler.ContentStartCalled, Is.True, "OnContentStart should be called");
        Assert.That(handler.QuestionContentCalled, Is.True, "OnQuestionContent should be called");
        Assert.That(handler.AskAnswerCalled, Is.True, "OnAskAnswer should be called");
        Assert.That(handler.AnswerStartCalled, Is.True, "OnAnswerStart should be called");
        Assert.That(handler.SimpleRightAnswerStartCalled, Is.True, "OnSimpleRightAnswerStart should be called");
        
        // Verify question content was correct
        Assert.That(handler.LastContentItems, Is.Not.Null);
        var content = handler.LastContentItems!.FirstOrDefault(c => c.Type == ContentTypes.Text);
        Assert.That(content, Is.Not.Null);
    }

    [Test]
    public void Question_WithMultipleContentItems_ShouldDisplayAll()
    {
        // Arrange
        var question = CreateQuestionWithMultipleContent();
        var handler = new TestQuestionEnginePlayHandler();
        var options = CreateDefaultOptions();
        var engine = new QuestionEngine(question, options, handler);

        // Act - Play through content display
        while (engine.PlayNext() && !handler.AskAnswerCalled)
        {
            // Continue until we reach ask answer
        }

        // Assert - Content should have been shown
        Assert.That(handler.QuestionContentCallCount, Is.GreaterThan(0));
        Assert.That(handler.ContentStartCalled, Is.True);
    }

    [Test]
    public void Question_WithUnsupportedType_ShouldReturnFalseImmediately()
    {
        // Arrange - Question with unsupported type
        var question = new Question();
        question.TypeName = "unsupported-type";
        var handler = new TestQuestionEnginePlayHandler();
        var options = CreateDefaultOptions();
        options.PlaySpecials = false;
        options.DefaultTypeName = "also-unsupported";
        var engine = new QuestionEngine(question, options, handler);

        // Act & Assert
        Assert.That(engine.PlayNext(), Is.False);
        Assert.That(handler.QuestionStartCalled, Is.False);
    }

    #endregion

    #region Answer Type Tests

    [Test]
    public void Question_WithSelectAnswerType_ShouldProvideOptions()
    {
        // Arrange
        var question = CreateSelectAnswerQuestion();
        var handler = new TestQuestionEnginePlayHandler();
        var options = CreateDefaultOptions();
        var engine = new QuestionEngine(question, options, handler);

        // Act - Play through to answer options
        while (engine.PlayNext() && !handler.AnswerOptionsCalled)
        {
            // Continue until answer options are set
        }

        // Assert
        Assert.That(handler.AnswerOptionsCalled, Is.True);
        Assert.That(handler.LastAnswerOptions, Is.Not.Null);
        Assert.That(handler.LastAnswerOptions!.Length, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void Question_WithNumericAnswerType_ShouldSetDeviation()
    {
        // Arrange
        var question = CreateNumericAnswerQuestion(deviation: 5);
        var handler = new TestQuestionEnginePlayHandler();
        var options = CreateDefaultOptions();
        var engine = new QuestionEngine(question, options, handler);

        // Act - Play through to numeric answer type
        while (engine.PlayNext() && !handler.NumericAnswerTypeCalled)
        {
            // Continue until numeric answer type is set
        }

        // Assert
        Assert.That(handler.NumericAnswerTypeCalled, Is.True);
        Assert.That(handler.LastDeviation, Is.EqualTo(5));
    }

    #endregion

    #region MoveToAnswer Tests

    [Test]
    public void MoveToAnswer_ShouldSkipToAnswerStage()
    {
        // Arrange
        var question = CreateSimpleTextQuestion("Question?", "Answer");
        var handler = new TestQuestionEnginePlayHandler();
        var options = CreateDefaultOptions();
        options.ShowSimpleRightAnswers = true;
        var engine = new QuestionEngine(question, options, handler);

        // Act - Start playing
        engine.PlayNext(); // Question start

        // Move to answer early
        handler.AnswerStartCalled = false;
        engine.MoveToAnswer();

        // Assert - Should be at answer stage
        Assert.That(handler.AnswerStartCalled, Is.True);

        // Continue playing - should show answer
        handler.SimpleRightAnswerStartCalled = false;
        
        while (engine.PlayNext() && !handler.SimpleRightAnswerStartCalled)
        {
            // Continue until answer is shown
        }
        
        Assert.That(handler.SimpleRightAnswerStartCalled, Is.True);
    }

    [Test]
    public void MoveToAnswer_CalledAfterCompletion_ShouldNotCauseIssues()
    {
        // Arrange
        var question = CreateSimpleTextQuestion("Question?", "Answer");
        var handler = new TestQuestionEnginePlayHandler();
        handler.ShowSimpleRightAnswers = true;
        var options = CreateDefaultOptions();
        var engine = new QuestionEngine(question, options, handler);

        // Act - Play through entire question
        while (engine.PlayNext())
        {
            // Play to the end
        }

        // Try to move to answer again
        Assert.DoesNotThrow(() => engine.MoveToAnswer());

        // Should still be complete
        Assert.That(engine.PlayNext(), Is.False);
    }

    #endregion

    #region Preambula Stage Tests (Using Library Scripts)

    [Test]
    public void StakeQuestion_ShouldCallSetAnswerer()
    {
        // Arrange
        var question = CreateStakeQuestion();
        var handler = new TestQuestionEnginePlayHandler();
        var options = CreateDefaultOptions();
        var engine = new QuestionEngine(question, options, handler);

        // Act - Play through to SetAnswerer
        while (engine.PlayNext() && !handler.SetAnswererCalled)
        {
            // Continue
        }

        // Assert
        Assert.That(handler.SetAnswererCalled, Is.True);
        Assert.That(handler.LastAnswererMode, Is.EqualTo(StepParameterValues.SetAnswererMode_Stake));
    }

    [Test]
    public void SecretQuestion_ShouldCallSetTheme()
    {
        // Arrange
        var question = CreateSecretQuestion("Science");
        var handler = new TestQuestionEnginePlayHandler();
        var options = CreateDefaultOptions();
        var engine = new QuestionEngine(question, options, handler);

        // Act
        while (engine.PlayNext() && !handler.SetThemeCalled)
        {
            // Continue until theme is set
        }

        // Assert
        Assert.That(handler.SetThemeCalled, Is.True);
    }

    [Test]
    public void SecretQuestion_ShouldCallAnnouncePrice()
    {
        // Arrange
        var question = CreateSecretQuestionWithPrice();
        var handler = new TestQuestionEnginePlayHandler();
        var options = CreateDefaultOptions();
        var engine = new QuestionEngine(question, options, handler);

        // Act
        while (engine.PlayNext() && !handler.AnnouncePriceCalled)
        {
            // Continue until price is announced
        }

        // Assert
        Assert.That(handler.AnnouncePriceCalled, Is.True);
        Assert.That(handler.LastPriceRange, Is.Not.Null);
    }

    [Test]
    public void SecretQuestion_ShouldCallSetPrice()
    {
        // Arrange
        var question = CreateSecretQuestionWithPrice();
        var handler = new TestQuestionEnginePlayHandler();
        var options = CreateDefaultOptions();
        var engine = new QuestionEngine(question, options, handler);

        // Act
        while (engine.PlayNext() && !handler.SetPriceCalled)
        {
            // Continue until price is set
        }

        // Assert
        Assert.That(handler.SetPriceCalled, Is.True);
        Assert.That(handler.LastSetPriceMode, Is.EqualTo(StepParameterValues.SetPriceMode_Select));
    }

    #endregion

    #region Edge Cases and Error Handling

    [Test]
    public void Question_WithEmptyContent_ShouldHandleGracefully()
    {
        // Arrange
        var question = CreateQuestionWithEmptyContent();
        var handler = new TestQuestionEnginePlayHandler();
        var options = CreateDefaultOptions();
        var engine = new QuestionEngine(question, options, handler);

        // Act & Assert - Should not crash
        Assert.DoesNotThrow(() =>
        {
            while (engine.PlayNext())
            {
                // Play through
            }
        });
    }

    [Test]
    public void Question_WithSelectAnswerButInsufficientOptions_ShouldSkip()
    {
        // Arrange
        var question = CreateSelectQuestionWithInsufficientOptions();
        var handler = new TestQuestionEnginePlayHandler();
        var options = CreateDefaultOptions();
        var engine = new QuestionEngine(question, options, handler);

        // Act
        while (engine.PlayNext())
        {
            // Play through entire question
        }

        // Assert - Answer options should not have been called
        Assert.That(handler.AnswerOptionsCalled, Is.False);
    }

    #endregion

    #region Helper Methods - Question Creators

    private static Question CreateSimpleTextQuestion(string questionText, string answer)
    {
        var question = new Question(); // Uses library "simple" script
        
        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new ContentItem
                {
                    Type = ContentTypes.Text,
                    Value = questionText,
                    Placement = ContentPlacements.Screen
                }
            }
        };

        question.Right.Add(answer);

        return question;
    }

    private static Question CreateQuestionWithMultipleContent()
    {
        var question = new Question(); // Uses library script
        
        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new ContentItem
                {
                    Type = ContentTypes.Text,
                    Value = "Listen carefully:",
                    Placement = ContentPlacements.Screen,
                    WaitForFinish = true
                },
                new ContentItem
                {
                    Type = ContentTypes.Image,
                    Value = "image.jpg",
                    IsRef = true,
                    Placement = ContentPlacements.Screen,
                    WaitForFinish = true
                },
                new ContentItem
                {
                    Type = ContentTypes.Text,
                    Value = "What is shown?",
                    Placement = ContentPlacements.Screen
                }
            }
        };

        question.Right.Add("Answer");

        return question;
    }

    private static Question CreateSelectAnswerQuestion()
    {
        var question = new Question(); // Uses library script

        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new ContentItem { Type = ContentTypes.Text, Value = "Pick the correct answer", Placement = ContentPlacements.Screen }
            }
        };

        // Define answer type
        question.Parameters[QuestionParameterNames.AnswerType] = new StepParameter
        {
            Type = StepParameterTypes.Simple,
            SimpleValue = StepParameterValues.SetAnswerTypeType_Select
        };

        // Define answer options
        var options = new StepParameters
        {
            ["A"] = new StepParameter
            {
                Type = StepParameterTypes.Content,
                ContentValue = new List<ContentItem>
                {
                    new ContentItem { Type = ContentTypes.Text, Value = "Option A", Placement = ContentPlacements.Screen }
                }
            },
            ["B"] = new StepParameter
            {
                Type = StepParameterTypes.Content,
                ContentValue = new List<ContentItem>
                {
                    new ContentItem { Type = ContentTypes.Text, Value = "Option B", Placement = ContentPlacements.Screen }
                }
            }
        };

        question.Parameters[QuestionParameterNames.AnswerOptions] = new StepParameter
        {
            Type = StepParameterTypes.Group,
            GroupValue = options
        };

        question.Right.Add("A");

        return question;
    }

    private static Question CreateNumericAnswerQuestion(int deviation)
    {
        var question = new Question(); // Uses library script

        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new ContentItem { Type = ContentTypes.Text, Value = "What is 100?", Placement = ContentPlacements.Screen }
            }
        };

        // Define answer type as numeric
        question.Parameters[QuestionParameterNames.AnswerType] = new StepParameter
        {
            Type = StepParameterTypes.Simple,
            SimpleValue = StepParameterValues.SetAnswerTypeType_Number
        };

        question.Parameters[QuestionParameterNames.AnswerDeviation] = new StepParameter
        {
            Type = StepParameterTypes.Simple,
            SimpleValue = deviation.ToString()
        };

        question.Right.Add("100");

        return question;
    }

    private static Question CreateStakeQuestion()
    {
        var question = new Question { TypeName = QuestionTypes.Stake };

        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new ContentItem { Type = ContentTypes.Text, Value = "Stake question", Placement = ContentPlacements.Screen }
            }
        };

        question.Right.Add("Answer");

        return question;
    }

    private static Question CreateSecretQuestion(string themeName)
    {
        var question = new Question { TypeName = QuestionTypes.Secret };

        question.Parameters[QuestionParameterNames.Theme] = new StepParameter
        {
            Type = StepParameterTypes.Simple,
            SimpleValue = themeName
        };

        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new ContentItem { Type = ContentTypes.Text, Value = "Secret question", Placement = ContentPlacements.Screen }
            }
        };

        question.Right.Add("Answer");

        return question;
    }

    private static Question CreateSecretQuestionWithPrice()
    {
        var question = new Question { TypeName = QuestionTypes.Secret };

        question.Parameters[QuestionParameterNames.Theme] = new StepParameter
        {
            Type = StepParameterTypes.Simple,
            SimpleValue = "Theme"
        };

        question.Parameters[QuestionParameterNames.Price] = new StepParameter
        {
            Type = StepParameterTypes.NumberSet,
            NumberSetValue = new NumberSet { Minimum = 100, Maximum = 500, Step = 100 }
        };

        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new ContentItem { Type = ContentTypes.Text, Value = "Secret question with price", Placement = ContentPlacements.Screen }
            }
        };

        question.Right.Add("Answer");

        return question;
    }

    private static Question CreateQuestionWithEmptyContent()
    {
        var question = new Question(); // Uses library script

        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>() // Empty list
        };

        question.Right.Add("Answer");

        return question;
    }

    private static Question CreateSelectQuestionWithInsufficientOptions()
    {
        var question = new Question();

        // Create custom script with insufficient options
        var script = new Script();
        
        var setAnswerTypeStep = new Step { Type = StepTypes.SetAnswerType };
        setAnswerTypeStep.AddSimpleParameter(StepParameterNames.Type, StepParameterValues.SetAnswerTypeType_Select);
        
        // Only 1 option (need at least 2)
        var options = new StepParameters
        {
            ["A"] = new StepParameter
            {
                Type = StepParameterTypes.Content,
                ContentValue = new List<ContentItem>
                {
                    new ContentItem { Type = ContentTypes.Text, Value = "Only option", Placement = ContentPlacements.Screen }
                }
            }
        };

        setAnswerTypeStep.Parameters[StepParameterNames.Options] = new StepParameter
        {
            Type = StepParameterTypes.Group,
            GroupValue = options
        };

        script.Steps.Add(setAnswerTypeStep);

        question.Script = script;

        return question;
    }

    private static QuestionEngineOptions CreateDefaultOptions()
    {
        return new QuestionEngineOptions
        {
            FalseStarts = FalseStartMode.Disabled,
            ShowSimpleRightAnswers = false,
            DefaultTypeName = QuestionTypes.Simple,
            PlaySpecials = true
        };
    }

    #endregion

    #region Test Handler

    private sealed class TestQuestionEnginePlayHandler : IQuestionEnginePlayHandler
    {
        public bool QuestionStartCalled { get; set; }
        public bool ContentStartCalled { get; set; }
        public bool QuestionContentCalled { get; set; }
        public int QuestionContentCallCount { get; set; }
        public bool AskAnswerCalled { get; set; }
        public bool AnswerStartCalled { get; set; }
        public bool SimpleRightAnswerStartCalled { get; set; }
        public bool AnswerOptionsCalled { get; set; }
        public bool NumericAnswerTypeCalled { get; set; }
        public bool SetThemeCalled { get; set; }
        public bool SetAnswererCalled { get; set; }
        public bool AnnouncePriceCalled { get; set; }
        public bool SetPriceCalled { get; set; }

        public IReadOnlyCollection<ContentItem>? LastContentItems { get; set; }
        public AnswerOption[]? LastAnswerOptions { get; set; }
        public int LastDeviation { get; set; }
        public string? LastThemeName { get; set; }
        public string? LastAnswererMode { get; set; }
        public NumberSet? LastPriceRange { get; set; }
        public string? LastSetPriceMode { get; set; }

        public bool ShowSimpleRightAnswers { get; set; }

        public void OnQuestionStart(bool buttonsRequired, ICollection<string> rightAnswers, Action skipQuestionCallback)
        {
            QuestionStartCalled = true;
        }

        public void OnContentStart(IReadOnlyList<ContentItem> contentItems, Action<int> moveToContentCallback)
        {
            ContentStartCalled = true;
        }

        public void OnQuestionContent(IReadOnlyCollection<ContentItem> content)
        {
            QuestionContentCalled = true;
            QuestionContentCallCount++;
            LastContentItems = content;
        }

        public void OnAskAnswer(string mode)
        {
            AskAnswerCalled = true;
        }

        public void OnAnswerStart()
        {
            AnswerStartCalled = true;
        }

        public void OnSimpleRightAnswerStart()
        {
            SimpleRightAnswerStartCalled = true;
        }

        public bool OnAnswerOptions(AnswerOption[] answerOptions, IReadOnlyList<ContentItem[]> screenContentSequence)
        {
            AnswerOptionsCalled = true;
            LastAnswerOptions = answerOptions;
            return false;
        }

        public bool OnNumericAnswerType(int deviation)
        {
            NumericAnswerTypeCalled = true;
            LastDeviation = deviation;
            return false;
        }

        public bool OnSetTheme(string themeName)
        {
            SetThemeCalled = true;
            LastThemeName = themeName;
            return false;
        }

        public bool OnSetAnswerer(string mode, string? select, string? stakeVisibility)
        {
            SetAnswererCalled = true;
            LastAnswererMode = mode;
            return false;
        }

        public bool OnAnnouncePrice(NumberSet availableRange)
        {
            AnnouncePriceCalled = true;
            LastPriceRange = availableRange;
            return false;
        }

        public bool OnSetPrice(string mode, NumberSet? availableRange)
        {
            SetPriceCalled = true;
            LastSetPriceMode = mode;
            return false;
        }

        public bool OnAccept() => false;

        public bool OnButtonPressStart() => false;

        public bool OnRightAnswerOption(string rightOptionLabel) => false;
    }

    #endregion
}
