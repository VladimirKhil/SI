using SIQuester.ViewModel.Properties;
using Anthropic;
using Anthropic.Models.Beta.Messages;
using SIQuester.Model;
using System.Text;
using System.Text.Json;

namespace SIQuester.ViewModel.Services;

/// <summary>
/// Claude (Anthropic API) access: JSON generation for the "generate questions / themes" buttons
/// and a streaming chat for the assistant panel.
/// </summary>
/// <remarks>
/// The API key is taken from settings (Claude API key) or from the ANTHROPIC_API_KEY environment variable.
/// Requests opt into server-side refusal fallbacks (fallbacks: "default"): if a safety classifier wrongly declines
/// a request, the API re-runs it on Anthropic's recommended fallback model.
/// </remarks>
internal static class ClaudeAssistant
{
    private const string ModelId = "claude-opus-5-5";

    private const string FallbackBeta = "server-side-fallback-2026-07-01";

    private static string? ApiKey =>
        !string.IsNullOrWhiteSpace(AppSettings.Default.ClaudeApiKey)
            ? AppSettings.Default.ClaudeApiKey.Trim()
            : Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

    /// <summary>
    /// Whether Claude can be used (an API key is available).
    /// </summary>
    public static bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    private static AnthropicClient CreateClient()
    {
        var apiKey = ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(Resources.ClaudeNoKey);
        }

        return new AnthropicClient { ApiKey = apiKey, Timeout = TimeSpan.FromMinutes(5) };
    }

    /// <summary>
    /// Sends a request and returns the JSON text that matches <paramref name="schema" />.
    /// </summary>
    public static async Task<string> CompleteJsonAsync(string system, string user, Dictionary<string, JsonElement> schema)
    {
        var client = CreateClient();

        var response = await client.Beta.Messages.Create(new MessageCreateParams
        {
            Model = ModelId,
            MaxTokens = 16000,
            Betas = [FallbackBeta],
            Fallbacks = new Default(), // fallbacks: "default" - Anthropic picks the fallback model by refusal category
            System = system,
            OutputConfig = new BetaOutputConfig
            {
                Format = new BetaJsonOutputFormat { Schema = schema },
            },
            Messages = [new BetaUserMessageParam { Content = user }],
        });

        if (response.StopReason == "refusal")
        {
            throw new InvalidOperationException(Resources.ClaudeRefused);
        }

        if (response.StopReason == "max_tokens")
        {
            throw new InvalidOperationException(Resources.ClaudeTooLong);
        }

        var text = new StringBuilder();

        foreach (var block in response.Content)
        {
            if (block.TryPickText(out var textBlock))
            {
                text.Append(textBlock.Text);
            }
        }

        return text.ToString();
    }

    /// <summary>
    /// Converts a JSON schema text into the SDK schema representation.
    /// </summary>
    public static Dictionary<string, JsonElement> ParseSchema(string schemaJson) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(schemaJson) ?? throw new InvalidOperationException("Invalid schema");

    /// <summary>
    /// Streams a chat answer. <paramref name="onText" /> receives text pieces as they arrive.
    /// </summary>
    /// <param name="system">System prompt (includes the current package context).</param>
    /// <param name="history">Conversation so far: (isUser, text) pairs, ending with the new user message.</param>
    /// <returns>Stop reason of the response.</returns>
    public static async Task<string?> StreamChatAsync(
        string system,
        IReadOnlyList<(bool IsUser, string Text)> history,
        Action<string> onText,
        CancellationToken cancellationToken)
    {
        var client = CreateClient();

        var messages = new List<BetaMessageParam>();

        foreach (var (isUser, text) in history)
        {
            messages.Add(isUser ? new BetaUserMessageParam { Content = text } : new BetaAssistantMessageParam { Content = text });
        }

        var parameters = new MessageCreateParams
        {
            Model = ModelId,
            MaxTokens = 32000,
            Betas = [FallbackBeta],
            Fallbacks = new Default(),
            System = system,
            Messages = messages,
        };

        string? stopReason = null;

        await foreach (var streamEvent in client.Beta.Messages.CreateStreaming(parameters, cancellationToken))
        {
            if (streamEvent.TryPickContentBlockDelta(out var delta) && delta.Delta.TryPickText(out var text))
            {
                onText(text.Text);
            }
            else if (streamEvent.TryPickDelta(out var messageDelta) && messageDelta.Delta.StopReason is { } reason)
            {
                stopReason = reason.ToString();
            }
        }

        return stopReason;
    }
}
