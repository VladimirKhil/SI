using SIQuester.Model;
using SIQuester.ViewModel.Services;

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

    [Test]
    public void CanGenerate_Gemini_NeedsKey()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GEMINI_API_KEY")))
        {
            Assert.Ignore("GEMINI_API_KEY is set on this machine");
        }

        var settings = AppSettings.Create();
        settings.AiProvider = AiProvider.Gemini;
        settings.UseGPT = false;

        settings.GeminiApiKey = "";
        Assert.That(settings.CanGenerate, Is.False);

        settings.GeminiApiKey = "test-key";
        Assert.That(settings.CanGenerate, Is.True);
    }

    [Test]
    public void GeminiChunk_TextAndStop()
    {
        var (text, reason) = GeminiAssistant.ParseChunk(
            """{"candidates":[{"content":{"role":"model","parts":[{"text":"Привет, "},{"text":"мир"}]},"finishReason":"STOP"}]}""");

        Assert.That(text, Is.EqualTo("Привет, мир"));
        Assert.That(reason, Is.EqualTo("stop"));
    }

    [Test]
    public void GeminiChunk_SkipsThoughts()
    {
        var (text, reason) = GeminiAssistant.ParseChunk(
            """{"candidates":[{"content":{"parts":[{"text":"thinking...","thought":true},{"text":"Ответ"}]}}]}""");

        Assert.That(text, Is.EqualTo("Ответ"));
        Assert.That(reason, Is.Null);
    }

    [TestCase("""{"candidates":[{"content":{"parts":[{"text":"обрезано"}]},"finishReason":"MAX_TOKENS"}]}""", "max_tokens")]
    [TestCase("""{"candidates":[{"finishReason":"SAFETY"}]}""", "refusal")]
    [TestCase("""{"promptFeedback":{"blockReason":"PROHIBITED_CONTENT"}}""", "refusal")]
    public void GeminiChunk_FinishReasons(string json, string expected)
    {
        Assert.That(GeminiAssistant.ParseChunk(json).FinishReason, Is.EqualTo(expected));
    }

    [Test]
    public void Gemini_IsAddedAfterExistingProviders()
    {
        // The provider is stored as a number in the settings file: existing values must not change
        Assert.That((int)AiProvider.Claude, Is.EqualTo(0));
        Assert.That((int)AiProvider.ChatGPT, Is.EqualTo(1));
        Assert.That((int)AiProvider.Gemini, Is.EqualTo(2));
    }
}
