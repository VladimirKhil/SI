using SIPackages;
using SIPackages.Core;

namespace SIEngine.Core;

/// <summary>
/// Performs SI question playing (new engine version).
/// </summary>
public sealed class QuestionEngine
{
    private readonly Question _question;
    private readonly IQuestionEnginePlayHandler _playHandler;
    private int _stepIndex = 0;
    private int _contentIndex = 0;

    private bool _started = false;

    public QuestionEngine(Question question, bool isFinal, IQuestionEnginePlayHandler playHandler)
    {
        question.Upgrade(isFinal);

        _question = question;
        _playHandler = playHandler;
    }

    public bool PlayNext()
    {
        if (_question.Script == null)
        {
            return false;
        }

        if (!_started)
        {
            _playHandler.OnQuestionStart();
            _started = true;
        }

        while (_stepIndex < _question.Script.Steps.Count)
        {
            var step = _question.Script.Steps[_stepIndex];

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

                        var select = step.TryGetSimpleParameter(StepParameterNames.Select);
                        var stakeVisibility = step.TryGetSimpleParameter(StepParameterNames.StakeVisibity);

                        _playHandler.OnSetAnswerer(mode, select, stakeVisibility);

                        _stepIndex++;
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
                            ? step.TryGetParameter(StepParameterNames.Content)?.NumberSetValue
                            : null;

                        _playHandler.OnSetPrice(mode, availableRange);
                        _stepIndex++;
                    }
                    break;

                case StepTypes.SetTheme:
                    var themeName = step.TryGetSimpleParameter(StepParameterNames.Content);

                    if (themeName == null)
                    {
                        _stepIndex++;
                        continue;
                    }

                    _playHandler.OnSetTheme(themeName);
                    _stepIndex++;
                    break;

                case StepTypes.Accept:
                    _playHandler.OnAccept();
                    _stepIndex++;
                    break;
                // Preambula part end

                case StepTypes.ShowContent:
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
                                    content = new StepParameter
                                    {
                                        ContentValue = new List<ContentItem>
                                        {
                                            new ContentItem
                                            {
                                                Placement = ContentPlacements.Screen,
                                                Type = AtomTypes.Text,
                                                Value = _question.Right.FirstOrDefault() ?? ""
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
                    else if (content.ContentValue == null)
                    {
                        _stepIndex++;
                        continue;
                    }

                    while (_contentIndex < content.ContentValue.Count)
                    {
                        var contentItem = content.ContentValue[_contentIndex++];

                        if (contentItem == null)
                        {
                            continue;
                        }

                        _playHandler.OnQuestionContentItem(contentItem);

                        if (contentItem.WaitForFinish)
                        {
                            return true;
                        }
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
                        _stepIndex++;
                    }

                    return true;

                default:
                    _stepIndex++;
                    break;
            }
        }

        return false;
    }
}
