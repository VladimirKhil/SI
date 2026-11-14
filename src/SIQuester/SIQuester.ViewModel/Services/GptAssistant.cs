using OpenAI;
using OpenAI.Chat;
using SIQuester.Model;
using System.ClientModel;

namespace SIQuester.ViewModel.Services;

/// <summary>
/// ChatGPT (OpenAI API) chat for the assistant panel. Uses the GPT key and model from settings.
/// </summary>
internal static class GptAssistant
{
    /// <summary>
    /// Whether ChatGPT can be used (an API key is set).
    /// </summary>
    public static bool IsConfigured => !string.IsNullOrWhiteSpace(AppSettings.Default.GPTApiKey);

    /// <summary>
    /// Model name shown in the chat header.
    /// </summary>
    public static string ModelName => AppSettings.Default.GPTModel;

    /// <summary>
    /// Streams a chat answer. <paramref name="onText" /> receives text pieces as they arrive.
    /// </summary>
    /// <returns>Finish reason of the response.</returns>
    public static async Task<string?> StreamChatAsync(
        string system,
        IReadOnlyList<(bool IsUser, string Text)> history,
        Action<string> onText,
        CancellationToken cancellationToken)
    {
        var client = new ChatClient(
            AppSettings.Default.GPTModel,
            new ApiKeyCredential(AppSettings.Default.GPTApiKey.Trim()),
            new OpenAIClientOptions { NetworkTimeout = TimeSpan.FromMinutes(5) });

        var messages = new List<ChatMessage> { new SystemChatMessage(system) };

        foreach (var (isUser, text) in history)
        {
            messages.Add(isUser ? new UserChatMessage(text) : new AssistantChatMessage(text));
        }

        string? finishReason = null;

        await foreach (var update in client.CompleteChatStreamingAsync(messages, cancellationToken: cancellationToken))
        {
            foreach (var part in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(part.Text))
                {
                    onText(part.Text);
                }
            }

            if (update.FinishReason is { } reason)
            {
                finishReason = reason.ToString();
            }
        }

        return finishReason;
    }
}
