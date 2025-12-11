using SIPackages;
using SIPackages.Core;

namespace SIEngine.Core.Tests;

/// <summary>
/// Tests for QuestionEngine functionality.
/// </summary>
[TestFixture]
public sealed class QuestionEngineTests
{
    #region Basic Question Flow Tests

    [Test]
    public void SimpleTextQuestion_ShouldPlayThrough4Stages()
    {
        // Arrange
        var question = CreateSimpleTextQuestion("What is 2+2?", "4");
        var handler = new TestQuestionEnginePlayHandler();
        var options = CreateDefaultOptions();
        var engine = new QuestionEngine(question, options, handler);

        // Act & Assert - Stage 1: Preambula (question start)
        Assert.That(engine.PlayNext(), Is.True);
        Assert.That(handler.QuestionStartCalled, Is.True);

        // Stage 1b: SetAnswerType (from library script, but skipped if no answer type parameter)
        // This step will be skipped since we don't have answer type parameters

        // Act & Assert - Stage 2: Displaying question content
        // ContentStart and QuestionContent happen together since no WaitForFinish
        Assert.That(engine.PlayNext(), Is.True);
        Assert.That(handler.ContentStartCalled, Is.True);
        Assert.That(handler.QuestionContentCalled, Is.True);
        Assert.That(handler.LastContentItems, Has.Count.EqualTo(1));
        var contentArray = handler.LastContentItems!.ToArray();
        Assert.That(contentArray[0].Type, Is.EqualTo(ContentTypes.Text));
        Assert.That(contentArray[0].Value, Is.EqualTo("What is 2+2?"));

        // Act & Assert - Stage 3: Asking for answer
        Assert.That(engine.PlayNext(), Is.True);
        Assert.That(handler.AskAnswerCalled, Is.True);

        Assert.That(engine.PlayNext(), Is.True);
        Assert.That(handler.AnswerStartCalled, Is.True);

        // Act & Assert - Stage 4: Displaying right answer
        handler.ShowSimpleRightAnswers = true;
        Assert.That(engine.PlayNext(), Is.True);
        Assert.That(handler.SimpleRightAnswerStartCalled, Is.True);

        // Question complete
        Assert.That(engine.PlayNext(), Is.False);
    }

    [Test]
    public void Question_WithMultipleContentItems_ShouldDisplayInSequence()
    {
        // Arrange
        var question = CreateQuestionWithMultipleContent();
        var handler = new TestQuestionEnginePlayHandler();
        var options = CreateDefaultOptions();
        var engine = new QuestionEngine(question, options, handler);

        // Act - Play through to content
        engine.PlayNext(); // Question start
        engine.PlayNext(); // Content start
        
        handler.QuestionContentCalled = false;
        handler.LastContentItems = null;

        // First content item (text) - displayed immediately with WaitForFinish
        Assert.That(engine.PlayNext(), Is.True);
        Assert.That(handler.QuestionContentCalled, Is.True);
        Assert.That(handler.LastContentItems, Has.Count.EqualTo(1));
        var contentArray1 = handler.LastContentItems!.ToArray();
        Assert.That(contentArray1[0].Type, Is.EqualTo(ContentTypes.Text));

        // Second content item (image) - waits
        handler.QuestionContentCalled = false;
        Assert.That(engine.PlayNext(), Is.True);
        Assert.That(handler.QuestionContentCalled, Is.True);
        Assert.That(handler.LastContentItems, Has.Count.EqualTo(1));
        var contentArray2 = handler.LastContentItems!.ToArray();
        Assert.That(contentArray2[0].Type, Is.EqualTo(ContentTypes.Image));

        // Third content item (text) - displayed after image completes
        handler.QuestionContentCalled = false;
        Assert.That(engine.PlayNext(), Is.True);
        Assert.That(handler.QuestionContentCalled, Is.True);
        Assert.That(handler.LastContentItems, Has.Count.EqualTo(1));
        var contentArray3 = handler.LastContentItems!.ToArray();
        Assert.That(contentArray3[0].Type, Is.EqualTo(ContentTypes.Text));
    }

    [Test]
    public void Question_WithUnsupportedType_ShouldReturnFalseImmediately()
    {
        // Arrange - Question with unsupported type and PlaySpecials = false
        var question = new Question();
        question.TypeName = "unsupported-type";
        var handler = new TestQuestionEnginePlayHandler();
        var options = CreateDefaultOptions();
        options.PlaySpecials = false; // This will use DefaultTypeName which won't match
        options.DefaultTypeName = "also-unsupported"; // Use non-existent type
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
        Assert.That(handler.LastAnswerOptions[0].Label, Is.Not.Null);
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
        var engine = new QuestionEngine(question, options, handler);

        // Act - Start playing
        engine.PlayNext(); // Question start
        engine.PlayNext(); // Content start + Show content

        // Move to answer before asking
        handler.AnswerStartCalled = false;
        engine.MoveToAnswer();

        // Assert - Should be at answer stage
        Assert.That(handler.AnswerStartCalled, Is.True);

        // Continue playing - should show answer
        handler.ShowSimpleRightAnswers = true;
        handler.SimpleRightAnswerStartCalled = false;
        
        Assert.That(engine.PlayNext(), Is.True);
        Assert.That(handler.SimpleRightAnswerStartCalled, Is.True);
    }

    [Test]
    public void MoveToAnswer_CalledAfterAnswer_ShouldNotMoveBackward()
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

        var contentCallsBefore = handler.QuestionContentCallCount;

        // Try to move to answer again
        engine.MoveToAnswer();

        // Continue (should be done)
        var hasMore = engine.PlayNext();

        // Assert - Should not restart or show content again
        Assert.That(hasMore, Is.False);
        Assert.That(handler.QuestionContentCallCount, Is.EqualTo(contentCallsBefore));
    }

    #endregion

    #region Preambula Stage Tests

    [Test]
    public void Question_WithSetTheme_ShouldCallHandler()
    {
        // Arrange
        var question = CreateQuestionWithTheme("Science");
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
        Assert.That(handler.LastThemeName, Is.EqualTo("Science"));
    }

    [Test]
    public void Question_WithSetAnswerer_ShouldCallHandler()
    {
        // Arrange
        var question = CreateQuestionWithAnswerer();
        var handler = new TestQuestionEnginePlayHandler();
        var options = CreateDefaultOptions();
        var engine = new QuestionEngine(question, options, handler);

        // Act
        while (engine.PlayNext() && !handler.SetAnswererCalled)
        {
            // Continue until answerer is set
        }

        // Assert
        Assert.That(handler.SetAnswererCalled, Is.True);
        Assert.That(handler.LastAnswererMode, Is.Not.Null);
    }

    [Test]
    public void Question_WithAnnouncePrice_ShouldCallHandler()
    {
        // Arrange
        var question = CreateQuestionWithAnnouncePrice();
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
    public void Question_WithSetPrice_ShouldCallHandler()
    {
        // Arrange
        var question = CreateQuestionWithSetPrice();
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
        Assert.That(handler.LastSetPriceMode, Is.Not.Null);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Test]
    public void Question_WithMissingParameter_ShouldSkipStep()
    {
        // Arrange - Question with reference to non-existent parameter
        var question = new Question { Script = new Script() };
        
        var showContentStep = new Step { Type = StepTypes.ShowContent };
        showContentStep.Parameters[StepParameterNames.Content] = new StepParameter
        {
            Type = StepParameterTypes.Simple,
            IsRef = true,
            SimpleValue = "nonexistent-param"
        };
        question.Script.Steps.Add(showContentStep);

        var handler = new TestQuestionEnginePlayHandler();
        var options = CreateDefaultOptions();
        var engine = new QuestionEngine(question, options, handler);

        // Act
        engine.PlayNext(); // Start
        var result = engine.PlayNext(); // Try to show content

        // Assert - Should skip the step with missing parameter
        Assert.That(result, Is.False);
    }

    [Test]
    public void Question_WithEmptyContentList_ShouldHandleGracefully()
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
        // Arrange - Select type with only 1 option (minimum is 2)
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
        var question = new Question(); // No custom script - will use library script
        
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
        var question = new Question(); // No custom script - will use library script
        
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
        var question = new Question(); // No custom script - will use library script

        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new ContentItem { Type = ContentTypes.Text, Value = "Pick the correct answer" }
            }
        };

        // Define answer options as question parameters
        question.Parameters["option_A"] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new ContentItem { Type = ContentTypes.Text, Value = "Option A", Placement = ContentPlacements.Screen }
            }
        };

        question.Parameters["option_B"] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new ContentItem { Type = ContentTypes.Text, Value = "Option B", Placement = ContentPlacements.Screen }
            }
        };

        question.Right.Add("A");

        // Create custom script for select answer type (library doesn't have this by default)
        var script = new Script();
        
        var setAnswerTypeStep = new Step { Type = StepTypes.SetAnswerType };
        setAnswerTypeStep.AddSimpleParameter(StepParameterNames.Type, StepParameterValues.SetAnswerTypeType_Select);
        
        var options = new StepParameters
        {
            ["A"] = new StepParameter
            {
                Type = StepParameterTypes.Content,
                IsRef = true,
                SimpleValue = "option_A"
            },
            ["B"] = new StepParameter
            {
                Type = StepParameterTypes.Content,
                IsRef = true,
                SimpleValue = "option_B"
            }
        };

        setAnswerTypeStep.Parameters[StepParameterNames.Options] = new StepParameter
        {
            Type = StepParameterTypes.Group,
            GroupValue = options
        };

        script.Steps.Add(setAnswerTypeStep);
        AddShowContentStep(script, QuestionParameterNames.Question);
        AddAskAnswerStep(script);
        AddShowContentStep(script, QuestionParameterNames.Answer, StepParameterValues.FallbackStepIdRef_Right);

        question.Script = script;

        return question;
    }

    private static Question CreateNumericAnswerQuestion(int deviation)
    {
        var question = new Question(); // No custom script - will use library script

        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new ContentItem { Type = ContentTypes.Text, Value = "What is 100?" }
            }
        };

        question.Parameters[QuestionParameterNames.AnswerDeviation] = new StepParameter
        {
            Type = StepParameterTypes.Simple,
            SimpleValue = deviation.ToString()
        };

        question.Right.Add("100");

        // Create custom script for numeric answer type (library doesn't have this by default)
        var script = new Script();
        
        var setAnswerTypeStep = new Step { Type = StepTypes.SetAnswerType };
        setAnswerTypeStep.AddSimpleParameter(StepParameterNames.Type, StepParameterValues.SetAnswerTypeType_Number);
        script.Steps.Add(setAnswerTypeStep);

        AddShowContentStep(script, QuestionParameterNames.Question);
        AddAskAnswerStep(script);
        AddShowContentStep(script, QuestionParameterNames.Answer, StepParameterValues.FallbackStepIdRef_Right);

        question.Script = script;

        return question;
    }

    private static Question CreateQuestionWithTheme(string themeName)
    {
        var question = new Question { TypeName = QuestionTypes.Secret }; // Use Secret type which has SetTheme

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
                new ContentItem { Type = ContentTypes.Text, Value = "Question" }
            }
        };

        question.Right.Add("Answer");

        return question;
    }

    private static Question CreateQuestionWithAnswerer()
    {
        var question = new Question { TypeName = QuestionTypes.Stake }; // Use Stake type which has SetAnswerer

        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new ContentItem { Type = ContentTypes.Text, Value = "Question" }
            }
        };

        question.Right.Add("Answer");

        return question;
    }

    private static Question CreateQuestionWithAnnouncePrice()
    {
        var question = new Question { TypeName = QuestionTypes.Secret }; // Use Secret type which has AnnouncePrice

        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new ContentItem { Type = ContentTypes.Text, Value = "Question" }
            }
        };

        question.Right.Add("Answer");

        return question;
    }

    private static Question CreateQuestionWithSetPrice()
    {
        var question = new Question { TypeName = QuestionTypes.Stake }; // Use Stake type which has SetPrice

        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new ContentItem { Type = ContentTypes.Text, Value = "Question" }
            }
        };

        question.Right.Add("Answer");

        return question;
    }

    private static Question CreateQuestionWithEmptyContent()
    {
        var question = new Question(); // No custom script - will use library script

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
                    new ContentItem { Type = ContentTypes.Text, Value = "Only option" }
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

    private static void AddShowContentStep(Script script, string parameterRef, string? fallbackRefId = null)
    {
        var step = new Step { Type = StepTypes.ShowContent };
        step.Parameters[StepParameterNames.Content] = new StepParameter
        {
            Type = StepParameterTypes.Simple,
            IsRef = true,
            SimpleValue = parameterRef
        };

        if (fallbackRefId != null)
        {
            step.AddSimpleParameter(StepParameterNames.FallbackRefId, fallbackRefId);
        }

        script.Steps.Add(step);
    }

    private static void AddAskAnswerStep(Script script)
    {
        var step = new Step { Type = StepTypes.AskAnswer };
        step.AddSimpleParameter(StepParameterNames.Mode, StepParameterValues.AskAnswerMode_Button);
        script.Steps.Add(step);
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
