using SIPackages.Core;
using SIPackages;

namespace SIEngine.Core;

/// <summary>
/// Provides helper methods for working with false starts.
/// </summary>
public static class FalseStartHelper
{
    /// <summary>
    /// Gets index of the first step that can be played together with allowing players to press the button (non-false start mode).
    /// </summary>
    /// <param name="script">Question script.</param>
    /// <param name="parameters">Question parameters.</param>
    /// <param name="falseStartMode"></param>
    /// <returns></returns>
    public static int? GetAskAnswerStartIndex(
        Script script,
        StepParameters? parameters,
        FalseStartMode falseStartMode)
    {
        if (falseStartMode == FalseStartMode.Enabled)
        {
            return null;
        }

        var stepCount = script.Steps.Count;

        for (var i = 0; i < stepCount; i++)
        {
            var step = script.Steps[i];

            if (step.Type == StepTypes.AskAnswer)
            {
                var mode = step.TryGetSimpleParameter(StepParameterNames.Mode);

                if (mode == StepParameterValues.AskAnswerMode_Button)
                {
                    return MoveAskAnswerStepUpper(i, script, parameters, falseStartMode);
                }
            }
        }

        return null;
    }

    private static int? MoveAskAnswerStepUpper(
        int askAnswerStepIndex,
        Script script,
        StepParameters? parameters,
        FalseStartMode falseStartMode)
    {
        var j = askAnswerStepIndex - 1;

        for (; j >= 0; j--)
        {
            var upperStep = script.Steps[j];

            if (!ValidateThatStepIsFalseStartable(upperStep, parameters, falseStartMode))
            {
                break;
            }
        }

        if (j == askAnswerStepIndex - 1)
        {
            return null;
        }

        return j + 1;
    }

    private static bool ValidateThatStepIsFalseStartable(
        Step step,
        StepParameters? parameters,
        FalseStartMode falseStartMode)
    {
        if (step.Type != StepTypes.ShowContent)
        {
            return false;
        }

        if (!step.Parameters.TryGetValue(StepParameterNames.Content, out var content))
        {
            return false;
        }

        if (content.IsRef)
        {
            var refId = content.SimpleValue;
            content = null;

            if (refId != null)
            {
                _ = parameters?.TryGetValue(refId, out content);
            }

            if (content == null)
            {
                return false;
            }
        }

        if (content.ContentValue == null)
        {
            return false;
        }

        if (falseStartMode == FalseStartMode.Disabled)
        {
            return true;
        }

        // FalseStartMode.TextContentOnly

        for (var k = 0; k < content.ContentValue.Count; k++)
        {
            var contentItem = content.ContentValue[k];

            if (contentItem.Type != AtomTypes.Text)
            {
                return true;
            }
        }

        return false;
    }
}
