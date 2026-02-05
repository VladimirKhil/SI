using SIPackages;
using SIPackages.Core;

namespace SIEngine.Core;

/// <summary>
/// Performs SI question playing.
/// </summary>
public sealed class QuestionEngine : IQuestionEngine
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
    private bool _isAnswerTypePoint = false;

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
            _playHandler.OnQuestionStart(ScriptHasAskAnswerButtonsStep(_script), _question.Right, SkipQuestion);
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

                        if (availableRange == null)
                        {
                            _stepIndex++;
                            continue;
                        }

                        var announcePriceResult = _playHandler.OnAnnouncePrice(PreprocessRange(availableRange));
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

                        if (availableRange != null)
                        {
                            availableRange = PreprocessRange(availableRange);
                        }

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

                    if (answerType == StepParameterValues.SetAnswerTypeType_Number)
                    {
                        var deviation = 0;
                        
                        if (_question.Parameters.TryGetValue(
                            QuestionParameterNames.AnswerDeviation,
                            out var answerDeviation) &&
                            answerDeviation.SimpleValue != null)
                        {
                            _ = int.TryParse(answerDeviation.SimpleValue, out deviation);
                        }
                        
                        var setNumericAnswerResult = _playHandler.OnNumericAnswerType(deviation);
                        _stepIndex++;
                        
                        if (setNumericAnswerResult)
                        {
                            return true;
                        }
                        
                        break;
                    }

                    if (answerType == StepParameterValues.SetAnswerTypeType_Point)
                    {
                        var deviation = 0.0;

                        if (_question.Parameters.TryGetValue(
                            QuestionParameterNames.AnswerDeviation,
                            out var answerDeviation) &&
                            answerDeviation.SimpleValue != null)
                        {
                            _ = double.TryParse(answerDeviation.SimpleValue, out deviation);
                        }

                        _isAnswerTypePoint = true;
                        var setPointAnswerResult = _playHandler.OnPointAnswerType(deviation);
                        _stepIndex++;
                        
                        if (setPointAnswerResult)
                        {
                            return true;
                        }
                        
                        break;
                    }

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
                            _ = _question.Parameters.TryGetValue(refId, out content);
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

                                    if (_isAnswerTypePoint && rightAnswer.Length > 0)
                                    {
                                        var handled = _playHandler.OnRightAnswerPoint(rightAnswer);
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

                        var durationValue = TryGetParameter(step, StepParameterNames.Duration)?.SimpleValue;

                        if (!int.TryParse(durationValue, out var duration) || duration < 0)
                        {
                            duration = 0;
                        }

                        _playHandler.OnAskAnswer(mode, duration);
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

    private static NumberSet PreprocessRange(NumberSet range)
    {
        if (range.Maximum < 0)
        {
            return new NumberSet { Minimum = 0, Maximum = 0, Step = 0 };
        }

        if (range.Minimum < 0)
        {
            return new NumberSet { Minimum = 0, Maximum = range.Maximum, Step = 0 };
        }

        if (range.Maximum < range.Minimum)
        {
            return new NumberSet { Minimum = range.Minimum, Maximum = range.Minimum, Step = 0 };
        }

        if (range.Step < 0 || range.Step > range.Maximum - range.Minimum)
        {
            return new NumberSet { Minimum = range.Minimum, Maximum = range.Maximum, Step = 0 };
        }

        return range;
    }

    private void SkipQuestion() => _stepIndex = _script?.Steps.Count ?? 0;

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
            _ = _question.Parameters.TryGetValue(refId, out value);
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

        // Rewind to the last AskAnswer step and move one step forward
        var askAnswerStepIndex = LastIndexOfAskAnswerStep(_script, _stepIndex, _script.Steps.Count - 2); // -2 because we want to have one step after AskAnswer
        var askAnswerFound = askAnswerStepIndex != -1;

        var nextStepIndex = askAnswerFound ? askAnswerStepIndex + 1 : _script.Steps.Count;

        if (nextStepIndex <= _stepIndex)
        {
            return;
        }

        _stepIndex = nextStepIndex;
        _contentIndex = 0;
        _areButtonsEnabled = false;
        _isAskingAnswer = false;

        if (askAnswerFound)
        {
            _playHandler.OnAnswerStart();
        }
    }

    private static int LastIndexOfAskAnswerStep(Script script, int fromIndex, int toIndex)
    {
        var index = toIndex;
        
        while (index >= fromIndex)
        {
            var step = script.Steps[index];

            if (step.Type == StepTypes.AskAnswer)
            {
                return index;
            }

            index--;
        }

        return -1;
    }
}
