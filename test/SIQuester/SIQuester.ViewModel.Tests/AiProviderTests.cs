using SIQuester.Model;

namespace SIQuester.ViewModel.Tests;

/// <summary>
/// Tests for choosing the AI that generates questions and themes.
/// </summary>
[TestFixture]
internal sealed class AiProviderTests
{
    [Test]
    public void Default_KeepsChatGpt()
    {
        var settings = AppSettings.Create();

        Assert.That(settings.AiProvider, Is.EqualTo(AiProvider.ChatGPT));
    }

    [Test]
    public void CanGenerate_ChatGpt_FollowsUseGpt()
    {
        var settings = AppSettings.Create();
        settings.AiProvider = AiProvider.ChatGPT;

        settings.UseGPT = false;
        Assert.That(settings.CanGenerate, Is.False);

        settings.UseGPT = true;
        Assert.That(settings.CanGenerate, Is.True);
    }

    [Test]
    public void CanGenerate_Claude_NeedsKey()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
        {
            Assert.Ignore("ANTHROPIC_API_KEY is set on this machine");
        }

        var settings = AppSettings.Create();
        settings.AiProvider = AiProvider.Claude;
        settings.UseGPT = false;

        settings.ClaudeApiKey = "";
        Assert.That(settings.CanGenerate, Is.False);

        settings.ClaudeApiKey = "test-key";
        Assert.That(settings.CanGenerate, Is.True);
    }
}
