using SIPackages;
using SIPackages.Core;

namespace SIEngine.Core.Tests;

[TestFixture]
public sealed class FalseStartHelperTests
{
    [Test]
    public void FalseStart_Enabled_ShouldNotAllowToPressBeforeTextStep()
    {
        Script script = CreateScript();

        var index = FalseStartHelper.GetAskAnswerStartIndex(script, null, FalseStartMode.Enabled);

        Assert.That(index, Is.Null);
    }

    [Test]
    public void FalseStart_Disabled_ShouldAllowToPressFromStart()
    {
        Script script = CreateScript();

        var index = FalseStartHelper.GetAskAnswerStartIndex(script, null, FalseStartMode.Disabled);

        Assert.That(index, Is.EqualTo(0));
    }

    [Test]
    public void FalseStart_TextOnly_ShouldNotAllowToPressBeforeMultimedia()
    {
        Script script = CreateScript();

        var index = FalseStartHelper.GetAskAnswerStartIndex(script, null, FalseStartMode.TextContentOnly);

        Assert.That(index, Is.EqualTo(1));
    }

    private static Script CreateScript()
    {
        var contentStep = new Step { Type = StepTypes.ShowContent };

        contentStep.Parameters.Add(
            StepParameterNames.Content,
            new StepParameter
            {
                Type = StepParameterTypes.Content,
                ContentValue = new List<ContentItem>
                {
                    new ContentItem { Type = AtomTypes.Text, Value = "question text" }
                }
            });

        var content2Step = new Step { Type = StepTypes.ShowContent };

        content2Step.Parameters.Add(
            StepParameterNames.Content,
            new StepParameter
            {
                Type = StepParameterTypes.Content,
                ContentValue = new List<ContentItem>
                {
                    new ContentItem { Type = AtomTypes.Audio, Value = "http://fake-audio-link" }
                }
            });

        var askAnswerStep = new Step { Type = StepTypes.AskAnswer };
        askAnswerStep.AddSimpleParameter(StepParameterNames.Mode, StepParameterValues.AskAnswerMode_Button);

        var answerStep = new Step { Type = StepTypes.ShowContent };

        answerStep.Parameters.Add(
            StepParameterNames.Content,
            new StepParameter
            {
                Type = StepParameterTypes.Content,
                ContentValue = new List<ContentItem>
                {
                    new ContentItem { Type = AtomTypes.Text, Value = "question answer" }
                }
            });

        var script = new Script();
        script.Steps.Add(contentStep);
        script.Steps.Add(content2Step);
        script.Steps.Add(askAnswerStep);
        script.Steps.Add(answerStep);
        return script;
    }
}