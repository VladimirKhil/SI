using SIPackages;
using SIPackages.Core;

namespace SIEngine.Core;

/// <summary>
/// Performs SI question playing.
/// </summary>
public sealed class QuestionEngine
{
    private readonly Question _question;
    private readonly QuestionEngineOptions _options;
    private readonly IQuestionEnginePlayHandler _playHandler;
    private int _stepIndex = 0;
    private int _contentIndex = 0;

    private bool _started = false;

    private int? _enableButtonsStepIndex = null;
    private bool _isAskingAnswer = false;
    private bool _areButtonsEnabled = false;

    private bool _isAnswerTypeSelect = false;

    private readonly Script? _script;

    public bool CanNext => _script != null && _stepIndex < _script.Steps.Count;

    public string QuestionTypeName { get; } = "";

    /// <summary>
    /// Initializes a new instance of <see cref="QuestionEngine" /> class.
    /// </summary>
    /// <param name="question">Question to play.</param>
    /// <param name="options">Engine options.</param>
    /// <param name="playHandler">Engine events handler.</param>
    public QuestionEngine(Question question, QuestionEngineOptions options, IQuestionEnginePlayHandler playHandler)
    {
        _question = question;
        _options = options;
        _playHandler = playHandler;
        _script = _question.Script;

        if (_script == null)
        {
            QuestionTypeName = _question.TypeName == QuestionTypes.Default || !options.PlaySpecials ? options.DefaultTypeName : _question.TypeName;
            ScriptsLibrary.Scripts.TryGetValue(QuestionTypeName, out _script);
        }
    }

    public bool PlayNext()
    {
        if (_script == null) // Unsupported question type
        {
            return false;
        }

        if (!_started)
        {
            _enableButtonsStepIndex = FalseStartHelper.GetAskAnswerStartIndex(_script, _question.Parameters, _options.FalseStarts);
            _playHandler.OnQuestionStart(ScriptHasAskAnswerButtonsStep(_script));
            _started = true;
        }

        if (_isAskingAnswer)
        {
            _playHandler.OnAnswerStart();
            _isAskingAnswer = false;
        }

        while (CanNext)
        {
            var step = _script.Steps[_stepIndex];

            switch (step.Type)
            {
                // Preambula part start
                case StepTypes.SetAnswerer:
                    {
                        var mode = step.TryGetSimpleParameter(StepParameterNames.Mode);

                        if (mode == null)
                        {
                            _stepIndex++;
                            continue;
                        }

                        var select = TryGetParameter(step, StepParameterNames.Select)?.SimpleValue;
                        var stakeVisibility = step.TryGetSimpleParameter(StepParameterNames.StakeVisibity);

                        var setAnswererResult = _playHandler.OnSetAnswerer(mode, select, stakeVisibility);

                        _stepIndex++;

                        if (setAnswererResult)
                        {
                            return true;
                        }
                    }

                    break;

                case StepTypes.AnnouncePrice:
                    {
                        var availableRange = TryGetParameter(step, StepParameterNames.Content)?.NumberSetValue;

                        var announcePriceResult = _playHandler.OnAnnouncePrice(availableRange);
                        _stepIndex++;

                        if (announcePriceResult)
                        {
                            return true;
                        }
                    }

                    break;

                case StepTypes.SetPrice:
                    {
                        var mode = step.TryGetSimpleParameter(StepParameterNames.Mode);

                        if (mode == null)
                        {
                            _stepIndex++;
                            continue;
                        }

                        var availableRange = mode == StepParameterValues.SetPriceMode_Select
                            ? TryGetParameter(step, StepParameterNames.Content)?.NumberSetValue
                            : null;

                        var setPriceResult = _playHandler.OnSetPrice(mode, availableRange);
                        _stepIndex++;

                        if (setPriceResult)
                        {
                            return true;
                        }
                    }

                    break;

                case StepTypes.SetTheme:
                    var themeName = TryGetParameter(step, StepParameterNames.Content)?.SimpleValue;

                    if (themeName == null)
                    {
                        _stepIndex++;
                        continue;
                    }

                    var setThemeResult = _playHandler.OnSetTheme(themeName);
                    _stepIndex++;

                    if (setThemeResult)
                    {
                        return true;
                    }

                    break;

                case StepTypes.Accept:
                    var acceptResult = _playHandler.OnAccept();
                    _stepIndex++;

                    if (acceptResult)
                    {
                        return true;
                    }

                    break;

                case StepTypes.SetAnswerType:
                    var answerType = TryGetParameter(step, StepParameterNames.Type)?.SimpleValue;

                    if (answerType != StepParameterValues.SetAnswerTypeType_Select) // Only "select" type is currently supported
                    {
                        _stepIndex++;
                        continue;
                    }

                    var answerOptionsValue = TryGetParameter(step, StepParameterNames.Options)?.GroupValue;

                    if (answerOptionsValue == null)
                    {
                        _stepIndex++;
                        continue;
                    }

                    var answerOptions = new List<AnswerOption>();

                    foreach (var (label, value) in answerOptionsValue)
                    {
                        if (value == null || value.ContentValue == null || !value.ContentValue.Any())
                        {
                            continue;
                        }

                        var contentValue = value.ContentValue[0]; // only first item is used
                        answerOptions.Add(new AnswerOption(label, contentValue));
                    }

                    if (answerOptions.Count < 2) // At least 2 options are required
                    {
                        _stepIndex++;
                        continue;
                    }

                    var allTypes = new List<ContentItem[]>();

                    if (_question.Parameters != null)
                    {
                        foreach (var param in _question.Parameters)
                        {
                            if (param.Value.ContentValue != null)
                            {
                                var types = new List<ContentItem>();

                                foreach (var contentItem in param.Value.ContentValue)
                                {
                                    if (contentItem.Placement != ContentPlacements.Screen)
                                    {
                                        continue;
                                    }

                                    types.Add(contentItem);

                                    if (contentItem.WaitForFinish)
                                    {
                                        allTypes.Add(types.ToArray());
                                        types.Clear();
                                    }
                                }

                                if (types.Count > 0)
                                {
                                    allTypes.Add(types.ToArray());
                                }
                            }
                        }
                    }

                    _isAnswerTypeSelect = true;
                    var setAnswerOptions = _playHandler.OnAnswerOptions(answerOptions.ToArray(), allTypes);
                    _stepIndex++;
                    
                    if (setAnswerOptions)
                    {
                        return true;
                    }

                    break;

                // Preambula part end

                case StepTypes.ShowContent:
                    if (_stepIndex == _enableButtonsStepIndex)
                    {
                        if (_playHandler.OnButtonPressStart())
                        {
                            return true;
                        }

                        _areButtonsEnabled = true;
                        _enableButtonsStepIndex = null;
                    }

                    if (!step.Parameters.TryGetValue(StepParameterNames.Content, out var content))
                    {
                        _stepIndex++;
                        continue;
                    }

                    if (content.IsRef)
                    {
                        var refId = content.SimpleValue;
                        content = null;

                        if (refId != null)
                        {
                            _ = _question.Parameters?.TryGetValue(refId, out content);
                        }

                        if (content == null)
                        {
                            var fallbackRefId = step.TryGetSimpleParameter(StepParameterNames.FallbackRefId);

                            if (fallbackRefId != null)
                            {
                                if (fallbackRefId == StepParameterValues.FallbackStepIdRef_Right)
                                {
                                    var rightAnswer = _question.Right.FirstOrDefault() ?? "";

                                    if (_isAnswerTypeSelect && rightAnswer.Length > 0)
                                    {
                                        var handled = _playHandler.OnRightAnswerOption(rightAnswer);
                                        _stepIndex++;

                                        if (handled)
                                        {
                                            return true;
                                        }

                                        continue;
                                    }

                                    if (!_options.ShowSimpleRightAnswers)
                                    {
                                        _stepIndex++;
                                        continue;
                                    }

                                    content = new StepParameter
                                    {
                                        ContentValue = new List<ContentItem>
                                        {
                                            new()
                                            {
                                                Placement = ContentPlacements.Screen,
                                                Type = ContentTypes.Text,
                                                Value = rightAnswer
                                            }
                                        }
                                    };

                                    _playHandler.OnSimpleRightAnswerStart();
                                }
                            }
                        }

                        if (content == null)
                        {
                            _stepIndex++;
                            continue;
                        }
                    }
                    
                    if (content.ContentValue == null)
                    {
                        _stepIndex++;
                        continue;
                    }

                    var contentItems = new List<ContentItem>();

                    while (_contentIndex < content.ContentValue.Count)
                    {
                        if (_contentIndex == 0)
                        {
                            var currentStepIndex = _stepIndex;

                            _playHandler.OnContentStart(
                                content.ContentValue,
                                index => MoveToContent(currentStepIndex, content.ContentValue.Count, index));
                        }

                        var contentItem = content.ContentValue[_contentIndex++];

                        if (contentItem == null)
                        {
                            continue;
                        }

                        contentItems.Add(contentItem);

                        if (contentItem.WaitForFinish)
                        {
                            if (contentItems.Any())
                            {
                                _playHandler.OnQuestionContent(contentItems);
                            }

                            if (_contentIndex == content.ContentValue.Count)
                            {
                                _stepIndex++;
                                _contentIndex = 0;
                            }

                            return true;
                        }
                    }

                    if (contentItems.Any())
                    {
                        _playHandler.OnQuestionContent(contentItems);
                    }

                    _stepIndex++;
                    _contentIndex = 0;
                    continue;

                case StepTypes.AskAnswer:
                    {
                        var mode = step.TryGetSimpleParameter(StepParameterNames.Mode);

                        if (mode == null)
                        {
                            _stepIndex++;
                            continue;
                        }

                        _playHandler.OnAskAnswer(mode);
                        _isAskingAnswer = true;
                        _stepIndex++;
                    }

                    return true;

                default:
                    _stepIndex++;
                    continue;
            }
        }

        return false;
    }

    private void MoveToContent(int stepIndex, int maxContentIndex, int contentIndex)
    {
        if (contentIndex > -1 && contentIndex < maxContentIndex)
        {
            _stepIndex = stepIndex;
            _contentIndex = contentIndex;
            PlayNext();
        }
    }

    private StepParameter? TryGetParameter(Step step, string parameter)
    {
        var value = step.TryGetParameter(parameter);

        if (value == null)
        {
            return null;
        }

        if (!value.IsRef)
        {
            return value;
        }

        var refId = value.SimpleValue;

        if (refId != null)
        {
            _ = _question.Parameters?.TryGetValue(refId, out value);
        }

        return value;
    }

    private static bool ScriptHasAskAnswerButtonsStep(Script script)
    {
        for (var i = 0; i < script.Steps.Count; i++)
        {
            var step = script.Steps[i];

            if (step.Type == StepTypes.AskAnswer)
            {
                var mode = step.TryGetSimpleParameter(StepParameterNames.Mode);

                if (mode == StepParameterValues.AskAnswerMode_Button)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Moves to the first step after the last of AskAnswer steps.
    /// </summary>
    public void MoveToAnswer()
    {
        if (_script == null)
        {
            return;
        }

        var nextStepIndex = _script.Steps.Count - 1;
        var askAnswerFound = false;

        while (nextStepIndex >= _stepIndex)
        {
            var nextStep = _script.Steps[nextStepIndex];

            if (nextStep.Type == StepTypes.AskAnswer)
            {
                askAnswerFound = true;
                break;
            }

            nextStepIndex--;
        }

        if (askAnswerFound && nextStepIndex + 1 > _stepIndex)
        {
            _stepIndex = nextStepIndex + 1;
            _contentIndex = 0;

            if (_areButtonsEnabled || _isAskingAnswer)
            {
                _playHandler.OnAnswerStart();
                _areButtonsEnabled = false;
                _isAskingAnswer = false;
            }
        }
    }
}
