using SIPackages;
using SIPackages.Core;

namespace SIEngine.Core.Tests;

[TestFixture]
public sealed class QuestionEngineTests
{
    [Test]
    public void SimpleQuestion_ShouldPlayFourStagesInOrder()
    {
        var question = CreateQuestion("What is 2+2?", "4");
        var handler = new TestQuestionEnginePlayHandler();
        var options = CreateDefaultOptions();
        options.ShowSimpleRightAnswers = true;
        options.FalseStarts = FalseStartMode.Enabled;
        var engine = new QuestionEngine(question, options, handler);

        PlayToEnd(engine);

        Assert.That(handler.Events, Is.EqualTo(new[]
        {
            "QuestionStart:True:4",
            "ContentStart:text",
            "QuestionContent:text:What is 2+2?:screen:True:True",
            "AskAnswer:button:0",
            "AnswerStart",
            "SimpleRightAnswerStart",
            "ContentStart:text",
            "QuestionContent:text:4:screen:True:True"
        }));
        var buttonHandler = new TestQuestionEnginePlayHandler();
        var buttonQuestion = CreateQuestion("What is 2+2?", "4");
        buttonQuestion.TypeName = QuestionTypes.WithButton;

        PlayToEnd(new QuestionEngine(buttonQuestion, options, buttonHandler));

        Assert.That(buttonHandler.Events, Is.EqualTo(handler.Events));
    }

    [Test]
    public void StakeQuestion_ShouldSetHighestVisibleStakeAnswerer()
    {
        var question = CreateQuestion("Stake question", "Answer");
        question.TypeName = QuestionTypes.Stake;
        var handler = new TestQuestionEnginePlayHandler();

        PlayToEnd(new QuestionEngine(question, CreateDefaultOptions(), handler));

        Assert.That(handler.Events, Does.Contain("SetAnswerer:stake:highest:visible"));
        Assert.That(handler.Events.IndexOf("SetAnswerer:stake:highest:visible"), Is.LessThan(handler.Events.IndexOf("ContentStart:text")));
        Assert.That(handler.Events, Does.Contain("AskAnswer:direct:0"));
    }

    [Test]
    public void StakeAllQuestion_ShouldSetAllPossibleHiddenStakeAnswerers()
    {
        var question = CreateQuestion("Stake all question", "Answer");
        question.TypeName = QuestionTypes.StakeAll;
        var handler = new TestQuestionEnginePlayHandler();

        PlayToEnd(new QuestionEngine(question, CreateDefaultOptions(), handler));

        Assert.That(handler.Events, Does.Contain("SetAnswerer:stake:allPossible:hidden"));
        Assert.That(handler.Events, Does.Contain("AskAnswer:direct:0"));
    }

    [Test]
    public void SecretQuestion_ShouldSetThemeAndAnnounceThenSetPrice()
    {
        var question = CreateSecretQuestion(QuestionTypes.Secret, "Science");
        var handler = new TestQuestionEnginePlayHandler();

        PlayToEnd(new QuestionEngine(question, CreateDefaultOptions(), handler));

        Assert.That(handler.Events, Does.Contain("SetTheme:Science"));
        Assert.That(handler.Events, Does.Contain("AnnouncePrice:100:500:100"));
        Assert.That(handler.Events, Does.Contain("SetPrice:select:100:500:100"));
        Assert.That(handler.Events.IndexOf("SetAnswerer:byCurrent::"), Is.LessThan(handler.Events.IndexOf("SetTheme:Science")));
        Assert.That(handler.Events.IndexOf("SetTheme:Science"), Is.LessThan(handler.Events.IndexOf("AnnouncePrice:100:500:100")));
        Assert.That(handler.Events.IndexOf("AnnouncePrice:100:500:100"), Is.LessThan(handler.Events.IndexOf("SetPrice:select:100:500:100")));
    }

    [Test]
    public void SecretPublicPriceQuestion_ShouldAnnouncePriceBeforeSettingAnswerer()
    {
        var question = CreateSecretQuestion(QuestionTypes.SecretPublicPrice, "History");
        var handler = new TestQuestionEnginePlayHandler();

        PlayToEnd(new QuestionEngine(question, CreateDefaultOptions(), handler));

        Assert.That(handler.Events.IndexOf("SetTheme:History"), Is.LessThan(handler.Events.IndexOf("AnnouncePrice:100:500:100")));
        Assert.That(handler.Events.IndexOf("AnnouncePrice:100:500:100"), Is.LessThan(handler.Events.IndexOf("SetAnswerer:byCurrent::")));
        Assert.That(handler.Events.IndexOf("SetAnswerer:byCurrent::"), Is.LessThan(handler.Events.IndexOf("SetPrice:select:100:500:100")));
    }

    [Test]
    public void SecretNoQuestionType_ShouldAcceptAfterSelectingPrice()
    {
        var question = CreateSecretQuestion(QuestionTypes.SecretNoQuestion, "Art");
        var handler = new TestQuestionEnginePlayHandler();

        PlayToEnd(new QuestionEngine(question, CreateDefaultOptions(), handler));

        Assert.That(handler.Events, Is.EqualTo(new[]
        {
            "QuestionStart:False:Answer",
            "SetAnswerer:byCurrent::",
            "SetTheme:Art",
            "AnnouncePrice:100:500:100",
            "SetPrice:select:100:500:100",
            "Accept"
        }));
    }

    [Test]
    public void ForYourselfAndNoRiskTypes_ShouldUseSameCurrentPlayerFlow()
    {
        var forYourselfHandler = new TestQuestionEnginePlayHandler();
        var noRiskHandler = new TestQuestionEnginePlayHandler();
        var forYourselfQuestion = CreateQuestion("Question", "Answer");
        forYourselfQuestion.TypeName = QuestionTypes.ForYourself;
        var noRiskQuestion = CreateQuestion("Question", "Answer");
        noRiskQuestion.TypeName = QuestionTypes.NoRisk;

        PlayToEnd(new QuestionEngine(forYourselfQuestion, CreateDefaultOptions(), forYourselfHandler));
        PlayToEnd(new QuestionEngine(noRiskQuestion, CreateDefaultOptions(), noRiskHandler));

        Assert.That(forYourselfHandler.Events, Is.EqualTo(noRiskHandler.Events));
        Assert.That(forYourselfHandler.Events, Does.Contain("SetAnswerer:current::"));
        Assert.That(forYourselfHandler.Events, Does.Contain("SetPrice:multiply:"));
    }

    [Test]
    public void ForAllQuestion_ShouldSetAllPlayersAsAnswerers()
    {
        var question = CreateQuestion("Question", "Answer");
        question.TypeName = QuestionTypes.ForAll;
        var handler = new TestQuestionEnginePlayHandler();

        PlayToEnd(new QuestionEngine(question, CreateDefaultOptions(), handler));

        Assert.That(handler.Events, Does.Contain("SetAnswerer:all::"));
    }

    [TestCase(ContentTypes.Image)]
    [TestCase(ContentTypes.Audio)]
    [TestCase(ContentTypes.Video)]
    [TestCase(ContentTypes.Html)]
    public void QuestionContent_ShouldPassEachContentKindToHandler(string type)
    {
        var content = new ContentItem
        {
            Type = type,
            Value = $"sample.{type}",
            IsRef = true,
            WaitForFinish = false
        };
        var question = CreateQuestion(content);
        var handler = new TestQuestionEnginePlayHandler();
        var engine = new QuestionEngine(question, CreateDefaultOptions(), handler);

        engine.PlayNext();

        Assert.That(handler.ContentStartItems, Is.EqualTo(new[] { content }));
        Assert.That(handler.QuestionContents[0], Is.EqualTo(new[] { content }));
    }

    [Test]
    public void OralQuestionContent_ShouldUseReplicPlacement()
    {
        var content = new ContentItem
        {
            Type = ContentTypes.Text,
            Value = "Say this aloud",
            Placement = ContentPlacements.Replic,
            WaitForFinish = false
        };
        var handler = new TestQuestionEnginePlayHandler();
        var engine = new QuestionEngine(CreateQuestion(content), CreateDefaultOptions(), handler);

        engine.PlayNext();

        Assert.That(handler.QuestionContents[0][0].Placement, Is.EqualTo(ContentPlacements.Replic));
        Assert.That(handler.Events[3], Is.EqualTo("QuestionContent:text:Say this aloud:replic:False:True"));
    }

    [Test]
    public void BackgroundQuestionContent_ShouldPreserveBackgroundPlacement()
    {
        var content = new ContentItem
        {
            Type = ContentTypes.Audio,
            Value = "ambient.mp3",
            Placement = ContentPlacements.Background,
            WaitForFinish = false
        };
        var handler = new TestQuestionEnginePlayHandler();
        var engine = new QuestionEngine(CreateQuestion(content), CreateDefaultOptions(), handler);

        engine.PlayNext();

        Assert.That(handler.QuestionContents[0][0].Placement, Is.EqualTo(ContentPlacements.Background));
        Assert.That(handler.Events[3], Is.EqualTo("QuestionContent:audio:ambient.mp3:background:False:True"));
    }

    [Test]
    public void WaitForFinishAndAnswerDuration_ShouldPauseAndAllowMovingToNextContent()
    {
        var first = new ContentItem { Type = ContentTypes.Text, Value = "First", Duration = TimeSpan.FromSeconds(3), WaitForFinish = true };
        var second = new ContentItem { Type = ContentTypes.Image, Value = "second.png", IsRef = true, Duration = TimeSpan.FromSeconds(4), WaitForFinish = false };
        var question = CreateQuestion(first, second);
        question.Parameters[QuestionParameterNames.AnswerDuration] = new StepParameter
        {
            Type = StepParameterTypes.Simple,
            SimpleValue = "17"
        };
        var handler = new TestQuestionEnginePlayHandler();
        var engine = new QuestionEngine(question, CreateDefaultOptions(), handler);

        Assert.That(engine.PlayNext(), Is.True);
        Assert.That(handler.ContentStartItems, Is.EqualTo(new[] { first, second }));
        Assert.That(handler.QuestionContents, Has.Count.EqualTo(1));
        Assert.That(handler.QuestionContents[0], Is.EqualTo(new[] { first }));

        handler.MoveToContentCallback!(1);

        Assert.That(handler.QuestionContents, Has.Count.EqualTo(2));
        Assert.That(handler.QuestionContents[1], Is.EqualTo(new[] { second }));
        PlayToEnd(engine);
        Assert.That(handler.Events, Does.Contain("AskAnswer:button:17"));
    }

    [Test]
    public void AnswerParameterWithContent_ShouldShowRightAnswerAfterAskAnswer()
    {
        var question = CreateQuestion("Question", "Fallback");
        var answerText = new ContentItem { Type = ContentTypes.Text, Value = "Answer text", WaitForFinish = false };
        var answerImage = new ContentItem { Type = ContentTypes.Image, Value = "answer.png", IsRef = true, WaitForFinish = false };
        question.Parameters[QuestionParameterNames.Answer] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = [answerText, answerImage]
        };
        var handler = new TestQuestionEnginePlayHandler();

        PlayToEnd(new QuestionEngine(question, CreateDefaultOptions(), handler));

        Assert.That(handler.Events.IndexOf("AskAnswer:button:0"), Is.LessThan(handler.Events.IndexOf("AnswerStart")));
        Assert.That(handler.Events.IndexOf("AnswerStart"), Is.LessThan(handler.Events.IndexOf("RightAnswer")));
        Assert.That(handler.Events.IndexOf("RightAnswer"), Is.LessThan(handler.Events.IndexOf("ContentStart:text,image")));
        Assert.That(handler.QuestionContents.Last(), Is.EqualTo(new[] { answerText, answerImage }));
    }

    [Test]
    public void QuestionStart_ShouldReceiveAllRightAnswers()
    {
        var question = CreateQuestion("Question", "First answer");
        question.Right.Add("Second answer");
        question.Wrong.Add("Wrong answer");
        var handler = new TestQuestionEnginePlayHandler();

        new QuestionEngine(question, CreateDefaultOptions(), handler).PlayNext();

        Assert.That(handler.StartRightAnswers, Is.EqualTo(new[] { "First answer", "Second answer" }));
        Assert.That(handler.Events[0], Is.EqualTo("QuestionStart:True:First answer,Second answer"));
    }

    [Test]
    public void Question_WithSimpleRightAnswersEnabled_ShouldShowRightAnswerText()
    {
        var question = CreateQuestion("Question", "Correct");
        var handler = new TestQuestionEnginePlayHandler();
        var options = CreateDefaultOptions();
        options.ShowSimpleRightAnswers = true;

        PlayToEnd(new QuestionEngine(question, options, handler));

        Assert.That(handler.Events, Does.Contain("SimpleRightAnswerStart"));
        Assert.That(handler.QuestionContents.Last()[0].Value, Is.EqualTo("Correct"));
    }

    [Test]
    public void Question_WithSimpleRightAnswersDisabled_ShouldNotShowFallbackAnswerText()
    {
        var question = CreateQuestion("Question", "Correct");
        var handler = new TestQuestionEnginePlayHandler();

        PlayToEnd(new QuestionEngine(question, CreateDefaultOptions(), handler));

        Assert.That(handler.Events, Does.Not.Contain("SimpleRightAnswerStart"));
        Assert.That(handler.Events, Does.Not.Contain("QuestionContent:text:Correct:screen:True:True"));
    }

    [Test]
    public void SelectAnswer_ShouldPassOptionLabelsAndShowRightOption()
    {
        var question = CreateSelectAnswerQuestion();
        var handler = new TestQuestionEnginePlayHandler();

        PlayToEnd(new QuestionEngine(question, CreateDefaultOptions(), handler));

        Assert.That(handler.LastAnswerOptions!.Select(option => option.Label), Is.EqualTo(new[] { "A", "B" }));
        Assert.That(handler.LastAnswerOptions![0].Content.Value, Is.EqualTo("Option A"));
        Assert.That(handler.Events, Does.Contain("RightAnswerOption:A"));
    }

    [Test]
    public void PointAnswerType_ShouldSetDeviationAndShowRightPoint()
    {
        var question = CreateQuestion("Mark the point", "12,24");
        question.Parameters[QuestionParameterNames.AnswerType] = SimpleParameter(StepParameterValues.SetAnswerTypeType_Point);
        question.Parameters[QuestionParameterNames.AnswerDeviation] = SimpleParameter("0.5");
        var handler = new TestQuestionEnginePlayHandler();

        PlayToEnd(new QuestionEngine(question, CreateDefaultOptions(), handler));

        Assert.That(handler.Events, Does.Contain("PointAnswerType:0.5"));
        Assert.That(handler.Events, Does.Contain("RightAnswerPoint:12,24"));
    }

    [Test]
    public void NumericAnswerType_ShouldPassDeviation()
    {
        var question = CreateQuestion("What is 100?", "100");
        question.Parameters[QuestionParameterNames.AnswerType] = SimpleParameter(StepParameterValues.SetAnswerTypeType_Number);
        question.Parameters[QuestionParameterNames.AnswerDeviation] = SimpleParameter("5");
        var handler = new TestQuestionEnginePlayHandler();

        PlayToEnd(new QuestionEngine(question, CreateDefaultOptions(), handler));

        Assert.That(handler.Events, Does.Contain("NumericAnswerType:5"));
    }

    [Test]
    public void ClientManagedAnswerType_ShouldNotifyHandler()
    {
        var question = CreateQuestion("Client validates", "Answer");
        question.Parameters[QuestionParameterNames.AnswerType] = SimpleParameter(StepParameterValues.SetAnswerTypeType_ManagedByClient);
        var handler = new TestQuestionEnginePlayHandler();

        PlayToEnd(new QuestionEngine(question, CreateDefaultOptions(), handler));

        Assert.That(handler.Events, Does.Contain("ClientAnswerType"));
    }

    [Test]
    public void CustomScript_ShouldPlayPreambulaQuestionAskAndAnswerInOrder()
    {
        var question = CreateQuestion("Question content", "Unused fallback");
        question.Script = new Script();
        question.Script.Steps.Add(new Step { Type = StepTypes.SetAnswerType });
        var themeStep = new Step { Type = StepTypes.SetTheme };
        themeStep.Parameters[StepParameterNames.Content] = SimpleParameter("Custom theme");
        question.Script.Steps.Add(themeStep);
        question.Script.Steps.Add(ShowParameterStep("prompt"));
        question.Script.Steps.Add(AskAnswerStep(StepParameterValues.AskAnswerMode_Direct));
        question.Script.Steps.Add(ShowParameterStep(QuestionParameterNames.Answer));
        question.Parameters["prompt"] = ContentParameter(new ContentItem { Type = ContentTypes.Text, Value = "Question content", WaitForFinish = false });
        question.Parameters[QuestionParameterNames.Answer] = ContentParameter(new ContentItem { Type = ContentTypes.Text, Value = "Answer content", WaitForFinish = false });
        var handler = new TestQuestionEnginePlayHandler();

        PlayToEnd(new QuestionEngine(question, CreateDefaultOptions(), handler));

        Assert.That(handler.Events, Is.EqualTo(new[]
        {
            "QuestionStart:False:Unused fallback",
            "SetTheme:Custom theme",
            "ContentStart:text",
            "QuestionContent:text:Question content:screen:False:True",
            "AskAnswer:direct:0",
            "AnswerStart",
            "RightAnswer",
            "ContentStart:text",
            "QuestionContent:text:Answer content:screen:False:True"
        }));
    }

    [Test]
    public void ScriptStepParameterReference_ShouldResolveQuestionParameter()
    {
        var question = CreateQuestion("Question", "Answer");
        question.Script = new Script();
        var themeStep = new Step { Type = StepTypes.SetTheme };
        themeStep.Parameters[StepParameterNames.Content] = new StepParameter
        {
            IsRef = true,
            Type = StepParameterTypes.Simple,
            SimpleValue = "category"
        };
        question.Script.Steps.Add(themeStep);
        question.Script.Steps.Add(ShowParameterStep(QuestionParameterNames.Question));
        question.Parameters["category"] = SimpleParameter("Geography");
        var handler = new TestQuestionEnginePlayHandler();

        new QuestionEngine(question, CreateDefaultOptions(), handler).PlayNext();

        Assert.That(handler.Events, Does.Contain("SetTheme:Geography"));
        Assert.That(handler.Events, Does.Contain("QuestionContent:text:Question:screen:True:True"));
    }

    [Test]
    public void AskAnswerButtonAndDirectModes_ShouldSetButtonRequirementAndMode()
    {
        var buttonHandler = new TestQuestionEnginePlayHandler();
        var directHandler = new TestQuestionEnginePlayHandler();

        PlayToEnd(new QuestionEngine(CreateQuestionWithAskMode(StepParameterValues.AskAnswerMode_Button), CreateDefaultOptions(), buttonHandler));
        PlayToEnd(new QuestionEngine(CreateQuestionWithAskMode(StepParameterValues.AskAnswerMode_Direct), CreateDefaultOptions(), directHandler));

        Assert.That(buttonHandler.Events[0], Is.EqualTo("QuestionStart:True:Answer"));
        Assert.That(buttonHandler.Events, Does.Contain("AskAnswer:button:0"));
        Assert.That(directHandler.Events[0], Is.EqualTo("QuestionStart:False:Answer"));
        Assert.That(directHandler.Events, Does.Contain("AskAnswer:direct:0"));
    }

    [Test]
    public void ButtonPressStart_WhenHandlerPauses_ShouldPausePlayNext()
    {
        var handler = new TestQuestionEnginePlayHandler { PauseOnButtonPressStart = true };
        var engine = new QuestionEngine(CreateQuestion("Question", "Answer"), CreateDefaultOptions(), handler);

        Assert.That(engine.PlayNext(), Is.True);
        Assert.That(handler.Events, Does.Contain("ButtonPressStart"));
        Assert.That(handler.Events, Does.Not.Contain("AskAnswer:button:0"));
    }

    [Test]
    public void MoveToAnswer_ShouldSkipToAnswerStage()
    {
        var question = CreateQuestion("Question?", "Answer");
        var handler = new TestQuestionEnginePlayHandler();
        var options = CreateDefaultOptions();
        options.ShowSimpleRightAnswers = true;
        var engine = new QuestionEngine(question, options, handler);

        engine.PlayNext();
        engine.MoveToAnswer();

        Assert.That(handler.Events, Does.Contain("AnswerStart"));
        PlayToEnd(engine);
        Assert.That(handler.Events, Does.Contain("SimpleRightAnswerStart"));
    }

    [Test]
    public void MoveToAnswer_CalledAfterCompletion_ShouldNotCauseIssues()
    {
        var engine = new QuestionEngine(CreateQuestion("Question?", "Answer"), CreateDefaultOptions(), new TestQuestionEnginePlayHandler());

        PlayToEnd(engine);

        Assert.DoesNotThrow(engine.MoveToAnswer);
        Assert.That(engine.PlayNext(), Is.False);
    }

    [Test]
    public void Question_WithEmptyContent_ShouldHandleGracefully()
    {
        var question = CreateQuestion();
        var handler = new TestQuestionEnginePlayHandler();
        var engine = new QuestionEngine(question, CreateDefaultOptions(), handler);

        Assert.DoesNotThrow(() => PlayToEnd(engine));
    }

    [Test]
    public void Question_WithUnsupportedType_ShouldReturnFalseImmediately()
    {
        var question = new Question { TypeName = "unsupported-type" };
        var options = CreateDefaultOptions();
        options.PlaySpecials = false;
        options.DefaultTypeName = "also-unsupported";
        var handler = new TestQuestionEnginePlayHandler();

        Assert.That(new QuestionEngine(question, options, handler).PlayNext(), Is.False);
        Assert.That(handler.Events, Is.Empty);
    }

    [Test]
    public void Question_WithSelectAnswerButInsufficientOptions_ShouldSkipOptions()
    {
        var question = new Question { Script = new Script() };
        var step = new Step { Type = StepTypes.SetAnswerType };
        step.AddSimpleParameter(StepParameterNames.Type, StepParameterValues.SetAnswerTypeType_Select);
        step.Parameters[StepParameterNames.Options] = new StepParameter
        {
            Type = StepParameterTypes.Group,
            GroupValue = new StepParameters
            {
                ["A"] = ContentParameter(new ContentItem { Type = ContentTypes.Text, Value = "Only option" })
            }
        };
        question.Script.Steps.Add(step);
        var handler = new TestQuestionEnginePlayHandler();

        PlayToEnd(new QuestionEngine(question, CreateDefaultOptions(), handler));

        Assert.That(handler.LastAnswerOptions, Is.Null);
    }

    private static Question CreateQuestion(params ContentItem[] content)
    {
        var question = new Question();
        if (content.Length > 0)
        {
            question.Parameters[QuestionParameterNames.Question] = ContentParameter(content);
        }

        question.Right.Add("Answer");
        return question;
    }

    private static Question CreateQuestion(string text, string rightAnswer)
    {
        var question = CreateQuestion(new ContentItem { Type = ContentTypes.Text, Value = text });
        question.Right.Clear();
        question.Right.Add(rightAnswer);
        return question;
    }

    private static Question CreateSecretQuestion(string typeName, string theme)
    {
        var question = CreateQuestion("Secret question", "Answer");
        question.TypeName = typeName;
        question.Parameters[QuestionParameterNames.Theme] = SimpleParameter(theme);
        question.Parameters[QuestionParameterNames.Price] = new StepParameter
        {
            Type = StepParameterTypes.NumberSet,
            NumberSetValue = new NumberSet { Minimum = 100, Maximum = 500, Step = 100 }
        };
        return question;
    }

    private static Question CreateSelectAnswerQuestion()
    {
        var question = CreateQuestion("Pick an answer", "A");
        question.Parameters[QuestionParameterNames.AnswerType] = SimpleParameter(StepParameterValues.SetAnswerTypeType_Select);
        question.Parameters[QuestionParameterNames.AnswerOptions] = new StepParameter
        {
            Type = StepParameterTypes.Group,
            GroupValue = new StepParameters
            {
                ["A"] = ContentParameter(new ContentItem { Type = ContentTypes.Text, Value = "Option A" }),
                ["B"] = ContentParameter(new ContentItem { Type = ContentTypes.Text, Value = "Option B" })
            }
        };
        return question;
    }

    private static Question CreateQuestionWithAskMode(string mode)
    {
        var question = CreateQuestion("Question", "Answer");
        question.Script = new Script();
        question.Script.Steps.Add(ShowParameterStep(QuestionParameterNames.Question));
        question.Script.Steps.Add(AskAnswerStep(mode));
        return question;
    }

    private static Step ShowParameterStep(string parameterName)
    {
        var step = new Step { Type = StepTypes.ShowContent };
        step.Parameters[StepParameterNames.Content] = new StepParameter
        {
            IsRef = true,
            Type = StepParameterTypes.Simple,
            SimpleValue = parameterName
        };
        return step;
    }

    private static Step AskAnswerStep(string mode)
    {
        var step = new Step { Type = StepTypes.AskAnswer };
        step.AddSimpleParameter(StepParameterNames.Mode, mode);
        return step;
    }

    private static StepParameter ContentParameter(params ContentItem[] content) => new()
    {
        Type = StepParameterTypes.Content,
        ContentValue = [.. content]
    };

    private static StepParameter SimpleParameter(string value) => new()
    {
        Type = StepParameterTypes.Simple,
        SimpleValue = value
    };

    private static QuestionEngineOptions CreateDefaultOptions() => new()
    {
        FalseStarts = FalseStartMode.Disabled,
        ShowSimpleRightAnswers = false,
        DefaultTypeName = QuestionTypes.Simple,
        PlaySpecials = true
    };

    private static void PlayToEnd(QuestionEngine engine)
    {
        var stepCount = 0;
        while (engine.PlayNext() && ++stepCount < 50)
        {
        }

        Assert.That(engine.CanNext, Is.False, "Question did not finish within the expected number of steps.");
    }

    private sealed class TestQuestionEnginePlayHandler : IQuestionEnginePlayHandler
    {
        public List<string> Events { get; } = [];
        public List<ContentItem[]> QuestionContents { get; } = [];
        public IReadOnlyList<ContentItem>? ContentStartItems { get; private set; }
        public Action<int>? MoveToContentCallback { get; private set; }
        public string[] StartRightAnswers { get; private set; } = [];
        public AnswerOption[]? LastAnswerOptions { get; private set; }
        public bool PauseOnButtonPressStart { get; init; }

        public void OnQuestionStart(bool buttonsRequired, ICollection<string> rightAnswers, Action skipQuestionCallback)
        {
            StartRightAnswers = rightAnswers.ToArray();
            Events.Add($"QuestionStart:{buttonsRequired}:{string.Join(",", rightAnswers)}");
        }

        public bool OnAnswerOptions(AnswerOption[] answerOptions, IReadOnlyList<ContentItem[]> screenContentSequence)
        {
            LastAnswerOptions = answerOptions;
            Events.Add($"AnswerOptions:{string.Join(",", answerOptions.Select(option => option.Label))}");
            return false;
        }

        public bool OnNumericAnswerType(int deviation)
        {
            Events.Add($"NumericAnswerType:{deviation}");
            return false;
        }

        public bool OnPointAnswerType(double deviation)
        {
            Events.Add($"PointAnswerType:{deviation.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            return false;
        }

        public bool OnClientAnswerType()
        {
            Events.Add("ClientAnswerType");
            return false;
        }

        public void OnQuestionContent(IReadOnlyCollection<ContentItem> content, bool isLast)
        {
            var items = content.ToArray();
            QuestionContents.Add(items);
            Events.Add($"QuestionContent:{string.Join(",", items.Select(item => $"{item.Type}:{item.Value}:{item.Placement}:{item.WaitForFinish}"))}:{isLast}");
        }

        public void OnAskAnswer(string mode, int duration) => Events.Add($"AskAnswer:{mode}:{duration}");

        public bool OnButtonPressStart()
        {
            Events.Add("ButtonPressStart");
            return PauseOnButtonPressStart;
        }

        public bool OnSetAnswerer(string mode, string? select, string? stakeVisibility)
        {
            Events.Add($"SetAnswerer:{mode}:{select}:{stakeVisibility}");
            return false;
        }

        public bool OnAnnouncePrice(NumberSet availableRange)
        {
            Events.Add($"AnnouncePrice:{availableRange.Minimum}:{availableRange.Maximum}:{availableRange.Step}");
            return false;
        }

        public bool OnSetPrice(string mode, NumberSet? availableRange)
        {
            var range = availableRange == null
                ? ""
                : $"{availableRange.Minimum}:{availableRange.Maximum}:{availableRange.Step}";
            Events.Add($"SetPrice:{mode}:{range}");
            return false;
        }

        public bool OnSetTheme(string themeName)
        {
            Events.Add($"SetTheme:{themeName}");
            return false;
        }

        public bool OnAccept()
        {
            Events.Add("Accept");
            return false;
        }

        public bool OnRightAnswer()
        {
            Events.Add("RightAnswer");
            return false;
        }

        public void OnContentStart(IReadOnlyList<ContentItem> contentItems, Action<int> moveToContentCallback)
        {
            ContentStartItems = contentItems.ToArray();
            MoveToContentCallback = moveToContentCallback;
            Events.Add($"ContentStart:{string.Join(",", contentItems.Select(item => item.Type))}");
        }

        public void OnSimpleRightAnswerStart() => Events.Add("SimpleRightAnswerStart");

        public bool OnRightAnswerOption(string rightOptionLabel)
        {
            Events.Add($"RightAnswerOption:{rightOptionLabel}");
            return false;
        }

        public bool OnRightAnswerPoint(string rightAnswer)
        {
            Events.Add($"RightAnswerPoint:{rightAnswer}");
            return false;
        }

        public void OnAnswerStart() => Events.Add("AnswerStart");
    }
}
