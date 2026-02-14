using Microsoft.Extensions.Options;
using SI.GameServer.Contract;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SI.GameServer.Client;

/// <summary>
/// Represents SIGame server client.
/// </summary>
public sealed class GameServerClient : IGameServerClient
{
    private readonly GameServerClientOptions _options;

    private readonly HttpClient _client;

    public string ServiceUri => _options.ServiceUri!;

    public event Action<GameInfo>? GameCreated;
    public event Action<int>? GameDeleted;
    public event Action<GameInfo>? GameChanged;
    public event Action? GamesLoaded;
    public event Action? GamesClear;

    public event Func<Exception?, Task>? Closed;
    public event Func<Exception?, Task>? Reconnecting;
    public event Func<string?, Task>? Reconnected;

    private readonly IUIThreadExecutor? _uIThreadExecutor;

    public IInfoApi Info { get; }

    public IGamesApi Games { get; }

    public GameServerClient(IOptions<GameServerClientOptions> options, IUIThreadExecutor? uIThreadExecutor = null)
    {
        _options = options.Value;
        _uIThreadExecutor = uIThreadExecutor;

        if (!_options.ServiceUri!.EndsWith("/", StringComparison.Ordinal))
        {
            _options.ServiceUri += "/";
        }

        _client = new HttpClient
        {
            BaseAddress = new Uri(ServiceUri),
            Timeout = _options.Timeout,
            DefaultRequestVersion = HttpVersion.Version20
        };

        if (_options.Culture != null)
        {
            _client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(_options.Culture));
        }

        Info = new InfoApi(_client);
        Games = new GamesApi(_client);
    }

    /// <summary>
    /// Opens a Server-Sent Events stream for receiving game updates.
    /// Automatically reconnects on connection drops.
    /// </summary>
    public async Task OpenGamesStreamAsync(CancellationToken cancellationToken = default)
    {
        var retryDelay = TimeSpan.FromSeconds(1);
        var maxRetryDelay = TimeSpan.FromSeconds(30);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ProcessGamesStreamAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                if (Reconnecting != null)
                {
                    await Reconnecting(ex);
                }

                // Wait before reconnecting with exponential backoff
                try
                {
                    await Task.Delay(retryDelay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                retryDelay = TimeSpan.FromTicks(Math.Min(retryDelay.Ticks * 2, maxRetryDelay.Ticks));
            }
        }
    }

    private async Task ProcessGamesStreamAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/v2/games/stream");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        string? eventType = null;
        var isFirstSnapshot = true;

        // Clear games before receiving new snapshot
        OnUI(() => GamesClear?.Invoke());

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync() ?? throw new IOException("SSE stream ended unexpectedly");
            if (line.StartsWith("event:", StringComparison.Ordinal))
            {
                eventType = line.Substring(6).Trim();
            }
            else if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                var data = line.Substring(5).Trim();
                ProcessGameEvent(eventType, data, jsonOptions);

                // Fire Reconnected after first successful snapshot
                if (isFirstSnapshot && eventType == "snapshot")
                {
                    isFirstSnapshot = false;

                    if (Reconnected != null)
                    {
                        await Reconnected(null);
                    }
                }
            }
        }
    }

    private void ProcessGameEvent(string? eventType, string data, JsonSerializerOptions jsonOptions)
    {
        Trace.TraceInformation("Received game event: {0} - {1}", eventType, data);

        var gameEvent = JsonSerializer.Deserialize<GameEvent>(data, jsonOptions);

        if (gameEvent == null)
        {
            return;
        }

        switch (eventType)
        {
            case "snapshot":
                if (gameEvent.Games != null)
                {
                    foreach (var game in gameEvent.Games)
                    {
                        OnUI(() => GameCreated?.Invoke(game));
                    }
                }

                if (gameEvent.IsLastChunk)
                {
                    OnUI(() => GamesLoaded?.Invoke());
                }
                break;

            case "created":
                if (gameEvent.GameInfo != null)
                {
                    OnUI(() => GameCreated?.Invoke(gameEvent.GameInfo));
                }
                break;

            case "changed":
                if (gameEvent.GameInfo != null)
                {
                    OnUI(() => GameChanged?.Invoke(gameEvent.GameInfo));
                }
                break;

            case "deleted":
                if (gameEvent.GameId.HasValue)
                {
                    OnUI(() => GameDeleted?.Invoke(gameEvent.GameId.Value));
                }
                break;
        }
    }

    public ValueTask DisposeAsync()
    {
        _client?.Dispose();
        return ValueTask.CompletedTask;
    }

    private void OnUI(Action action)
    {
        if (_uIThreadExecutor != null)
        {
            _uIThreadExecutor.ExecuteOnUIThread(action);
            return;
        }

        action();
    }
}
