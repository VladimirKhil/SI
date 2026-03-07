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

    private readonly ProxyFailoverHandler? _proxyFailoverHandler;

    public string ServiceUri => _options.ServiceUri!;

    public string? ProxyUri => _options.ProxyUri;

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

        if (!string.IsNullOrEmpty(_options.ProxyUri) && !_options.ProxyUri.EndsWith("/", StringComparison.Ordinal))
        {
            _options.ProxyUri += "/";
        }

        var serviceUri = new Uri(_options.ServiceUri!, UriKind.Absolute);

        Uri? proxyUri = null;

        if (!string.IsNullOrEmpty(_options.ProxyUri))
        {
            proxyUri = new Uri(_options.ProxyUri, UriKind.Absolute);
            _proxyFailoverHandler = new ProxyFailoverHandler(proxyUri, serviceUri);
        }

        _client = _proxyFailoverHandler != null ? new HttpClient(_proxyFailoverHandler) : new HttpClient();
        _client.BaseAddress = proxyUri ?? serviceUri;
        _client.Timeout = _options.Timeout;
        _client.DefaultRequestVersion = HttpVersion.Version20;

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

    private sealed class ProxyFailoverHandler : DelegatingHandler
    {
        private readonly Uri _proxyBaseUri;
        private readonly Uri _serviceBaseUri;
        private int _useProxy = 1;

        public string CurrentServiceUri => Volatile.Read(ref _useProxy) == 1
            ? _proxyBaseUri.AbsoluteUri
            : _serviceBaseUri.AbsoluteUri;

        public ProxyFailoverHandler(Uri proxyBaseUri, Uri serviceBaseUri)
            : base(new HttpClientHandler())
        {
            _proxyBaseUri = proxyBaseUri;
            _serviceBaseUri = serviceBaseUri;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Volatile.Read(ref _useProxy) == 0)
            {
                RewriteRequestUriIfNeeded(request);
                return await base.SendAsync(request, cancellationToken);
            }

            HttpRequestMessage? failoverRequest = null;

            try
            {
                failoverRequest = await CloneAndBufferRequestAsync(request, cancellationToken);
                var response = await base.SendAsync(request, cancellationToken);

                if (!IsProxyFailure(response))
                {
                    failoverRequest.Dispose();
                    return response;
                }

                response.Dispose();
            }
            catch (Exception exc) when (IsProxyException(exc, cancellationToken))
            {
                // fall through to failover
            }

            Interlocked.Exchange(ref _useProxy, 0);

            if (failoverRequest == null)
            {
                failoverRequest = await CloneAndBufferRequestAsync(request, cancellationToken);
            }

            RewriteRequestUriIfNeeded(failoverRequest);
            Trace.TraceWarning("Proxy request failed, falling back to direct server: {0}", failoverRequest.RequestUri);
            var failoverResponse = await base.SendAsync(failoverRequest, cancellationToken);
            failoverRequest.Dispose();
            return failoverResponse;
        }

        private void RewriteRequestUriIfNeeded(HttpRequestMessage request)
        {
            if (request.RequestUri == null)
            {
                return;
            }

            if (!request.RequestUri.IsAbsoluteUri)
            {
                return;
            }

            if (!_proxyBaseUri.IsBaseOf(request.RequestUri))
            {
                return;
            }

            var relative = _proxyBaseUri.MakeRelativeUri(request.RequestUri);
            request.RequestUri = new Uri(_serviceBaseUri, relative);
        }

        private static bool IsProxyException(Exception exc, CancellationToken cancellationToken) =>
            exc is HttpRequestException
            || (exc is TaskCanceledException && !cancellationToken.IsCancellationRequested);

        private static bool IsProxyFailure(HttpResponseMessage response) =>
            response.StatusCode == HttpStatusCode.BadGateway
            || response.StatusCode == HttpStatusCode.ServiceUnavailable
            || response.StatusCode == HttpStatusCode.GatewayTimeout
            || response.StatusCode == HttpStatusCode.ProxyAuthenticationRequired;

        private static async Task<HttpRequestMessage> CloneAndBufferRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Version = request.Version,
                VersionPolicy = request.VersionPolicy
            };

            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (request.Content != null)
            {
                var originalContent = request.Content;
                var contentBytes = await originalContent.ReadAsByteArrayAsync(cancellationToken);
                var originalHeaders = originalContent.Headers;

                request.Content = CreateBufferedContent(contentBytes, originalHeaders);
                clone.Content = CreateBufferedContent(contentBytes, originalHeaders);
                originalContent.Dispose();
            }

            return clone;
        }

        private static HttpContent CreateBufferedContent(byte[] contentBytes, HttpContentHeaders originalHeaders)
        {
            var content = new ByteArrayContent(contentBytes);

            foreach (var header in originalHeaders)
            {
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return content;
        }
    }
}
