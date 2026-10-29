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

        var index = FalseStartHelper.GetAskAnswerStartIndex(script, [], FalseStartMode.Enabled);

        Assert.That(index, Is.Null);
    }

    [Test]
    public void FalseStart_Disabled_ShouldAllowToPressFromStart()
    {
        Script script = CreateScript();

        var index = FalseStartHelper.GetAskAnswerStartIndex(script, [], FalseStartMode.Disabled);

        Assert.That(index, Is.EqualTo(0));
    }

    [Test]
    public void FalseStart_TextOnly_ShouldNotAllowToPressBeforeMultimedia()
    {
        Script script = CreateScript();

        var index = FalseStartHelper.GetAskAnswerStartIndex(script, [], FalseStartMode.TextContentOnly);

        Assert.That(index, Is.EqualTo(1));
    }

    [Test]
    public void FalseStart_ImageOnlyWithTextContentOnly_ShouldAllowPressBeforeImage()
    {
        var script = CreateScript(
            new ContentItem { Type = ContentTypes.Image, Value = "picture.png" });

        var index = FalseStartHelper.GetAskAnswerStartIndex(script, [], FalseStartMode.TextContentOnly);

        Assert.That(index, Is.EqualTo(0));
    }

    [Test]
    public void FalseStart_MixedContentWithDelays_ShouldAllowPressBeforeMultimedia()
    {
        var script = CreateScript(
            new ContentItem { Type = ContentTypes.Text, Value = "question text", WaitForFinish = true },
            new ContentItem { Type = ContentTypes.Audio, Value = "audio.mp3", WaitForFinish = false });

        var index = FalseStartHelper.GetAskAnswerStartIndex(script, [], FalseStartMode.TextContentOnly);

        Assert.That(index, Is.EqualTo(0));
    }

    private static Script CreateScript()
    {
        var script = new Script();
        script.Steps.Add(CreateContentStep(new ContentItem { Type = ContentTypes.Text, Value = "question text" }));
        script.Steps.Add(CreateContentStep(new ContentItem { Type = ContentTypes.Audio, Value = "audio.mp3" }));
        script.Steps.Add(CreateAskAnswerStep());
        script.Steps.Add(CreateAnswerStep());
        return script;
    }

    private static Script CreateScript(params ContentItem[] contentItems)
    {
        var script = new Script();
        script.Steps.Add(CreateContentStep(contentItems));
        script.Steps.Add(CreateAskAnswerStep());
        script.Steps.Add(CreateAnswerStep());
        return script;
    }

    private static Step CreateContentStep(params ContentItem[] contentItems)
    {
        var step = new Step { Type = StepTypes.ShowContent };
        step.Parameters.Add(StepParameterNames.Content, new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = [.. contentItems]
        });
        return step;
    }

    private static Step CreateAskAnswerStep()
    {
        var step = new Step { Type = StepTypes.AskAnswer };
        step.AddSimpleParameter(StepParameterNames.Mode, StepParameterValues.AskAnswerMode_Button);
        return step;
    }

    private static Step CreateAnswerStep() => CreateContentStep(
        new ContentItem { Type = ContentTypes.Text, Value = "question answer" });
}