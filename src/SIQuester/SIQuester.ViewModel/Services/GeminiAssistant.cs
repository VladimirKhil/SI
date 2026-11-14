using SIQuester.Model;
using SIQuester.ViewModel.Properties;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SIQuester.ViewModel.Services;

/// <summary>
/// Gemini (Google AI) for the assistant chat and for question / theme generation.
/// Uses the REST API (models.generateContent / streamGenerateContent) directly, without extra packages.
/// </summary>
public static class GeminiAssistant
{
    private const string ApiBase = "https://generativelanguage.googleapis.com/v1beta/models/";

    /// <summary>
    /// Default model: the current stable Flash model.
    /// </summary>
    public const string DefaultModel = AppSettings.DefaultGeminiModel;

    /// <summary>
    /// Models offered in the settings.
    /// </summary>
    public static readonly string[] Models = [DefaultModel, "gemini-3.6-flash", "gemini-3.5-flash-lite"];

    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(5) };

    private static string ApiKey
    {
        get
        {
            var key = AppSettings.Default.GeminiApiKey;
            return string.IsNullOrWhiteSpace(key) ? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "" : key.Trim();
        }
    }

    /// <summary>
    /// Whether Gemini can be used (a key is set in the settings or in GEMINI_API_KEY).
    /// </summary>
    public static bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    /// <summary>
    /// Model name shown in the chat header.
    /// </summary>
    public static string ModelName => Model;

    private static string Model =>
        string.IsNullOrWhiteSpace(AppSettings.Default.GeminiModel) ? DefaultModel : AppSettings.Default.GeminiModel.Trim();

    /// <summary>
    /// Streams a chat answer. <paramref name="onText" /> receives text pieces as they arrive.
    /// </summary>
    /// <returns>Finish reason: "refusal" when Gemini blocked the request or the answer, "max_tokens" when it was cut.</returns>
    public static async Task<string?> StreamChatAsync(
        string system,
        IReadOnlyList<(bool IsUser, string Text)> history,
        Action<string> onText,
        CancellationToken cancellationToken)
    {
        var contents = new JsonArray();

        foreach (var (isUser, text) in history)
        {
            contents.Add(Content(isUser ? "user" : "model", text));
        }

        var body = new JsonObject
        {
            ["systemInstruction"] = Content(null, system),
            ["contents"] = contents
        };

        using var request = CreateRequest("streamGenerateContent?alt=sse", body);
        using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        string? finishReason = null;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            // Server-sent events: each "data:" line holds a partial GenerateContentResponse
            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var chunk = ParseChunk(line[5..].Trim());

            if (chunk.Text.Length > 0)
            {
                onText(chunk.Text);
            }

            finishReason = chunk.FinishReason ?? finishReason;
        }

        return finishReason;
    }

    /// <summary>
    /// Gets an answer that is JSON matching <paramref name="jsonSchema" />.
    /// </summary>
    public static async Task<string> CompleteJsonAsync(string system, string user, string jsonSchema, CancellationToken cancellationToken = default)
    {
        var body = new JsonObject
        {
            ["systemInstruction"] = Content(null, system),
            ["contents"] = new JsonArray { Content("user", user) },
            ["generationConfig"] = new JsonObject
            {
                ["responseMimeType"] = "application/json",
                ["responseJsonSchema"] = JsonNode.Parse(jsonSchema)
            }
        };

        using var request = CreateRequest("generateContent", body);
        using var response = await Client.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var result = ParseChunk(await response.Content.ReadAsStringAsync(cancellationToken));

        return result.FinishReason switch
        {
            "refusal" => throw new Exception(Resources.GeminiRefused),
            "max_tokens" => throw new Exception(Resources.GeminiTooLong),
            _ => result.Text
        };
    }

    /// <summary>
    /// Reads the text and the finish reason of one (partial) GenerateContentResponse.
    /// </summary>
    public static (string Text, string? FinishReason) ParseChunk(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return ("", null);
        }

        var root = JsonNode.Parse(json);

        // The whole request was blocked
        if (root?["promptFeedback"]?["blockReason"] is not null)
        {
            return ("", "refusal");
        }

        var candidate = root?["candidates"]?[0];
        var text = new StringBuilder();

        if (candidate?["content"]?["parts"] is JsonArray parts)
        {
            foreach (var part in parts)
            {
                // Thought summaries are not part of the answer
                if (part?["thought"] is JsonValue thought && thought.TryGetValue<bool>(out var isThought) && isThought)
                {
                    continue;
                }

                if (part?["text"] is JsonValue value && value.TryGetValue<string>(out var partText))
                {
                    text.Append(partText);
                }
            }
        }

        var reason = candidate?["finishReason"] is JsonValue reasonValue && reasonValue.TryGetValue<string>(out var r) ? r : null;

        var finishReason = reason switch
        {
            null or "FINISH_REASON_UNSPECIFIED" => null,
            "STOP" => "stop",
            "MAX_TOKENS" => "max_tokens",
            "SAFETY" or "RECITATION" or "BLOCKLIST" or "PROHIBITED_CONTENT" or "SPII" or "LANGUAGE" => "refusal",
            _ => reason.ToLowerInvariant()
        };

        return (text.ToString(), finishReason);
    }

    private static JsonObject Content(string? role, string text)
    {
        var content = new JsonObject { ["parts"] = new JsonArray { new JsonObject { ["text"] = text } } };

        if (role != null)
        {
            content["role"] = role;
        }

        return content;
    }

    private static HttpRequestMessage CreateRequest(string method, JsonObject body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}{Uri.EscapeDataString(Model)}:{method}")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };

        // The key goes in a header, not in the URL (URLs end up in logs)
        request.Headers.Add("x-goog-api-key", ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var details = await response.Content.ReadAsStringAsync(cancellationToken);
        string? message = null;

        try
        {
            message = JsonNode.Parse(details)?["error"]?["message"] is JsonValue value && value.TryGetValue<string>(out var m) ? m : null;
        }
        catch (JsonException)
        {
            // Not a JSON error body
        }

        throw new Exception($"Gemini: {(int)response.StatusCode} {message ?? response.ReasonPhrase}");
    }
}
