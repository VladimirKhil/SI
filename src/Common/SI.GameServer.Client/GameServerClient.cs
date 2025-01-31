using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SI.GameServer.Contract;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SI.GameServer.Client;

/// <summary>
/// Represents SIGame server client.
/// </summary>
public sealed class GameServerClient : IGameServerClient
{
    private bool _isOpened;

    private readonly GameServerClientOptions _options;

    private readonly HttpClient _client;

    private HubConnection? _connection;

    private HubConnection Connection
    {
        get
        {
            if (_connection == null)
            {
                throw new InvalidOperationException("Not connected");
            }

            return _connection;
        }
    }

    public string ServiceUri => _options.ServiceUri!;

    public event Action<GameInfo>? GameCreated;
    public event Action<int>? GameDeleted;
    public event Action<GameInfo>? GameChanged;

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

    public Task<RunGameResponse> RunGameAsync(RunGameRequest runGameRequest, CancellationToken cancellationToken = default) =>
        Connection.InvokeAsync<RunGameResponse>("RunGame", runGameRequest, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            _connection.Closed -= OnConnectionClosedAsync;

            await _connection.DisposeAsync();
            _connection = null;
        }

        _client?.Dispose();
    }

    public Task<Slice<GameInfo>> GetGamesAsync(int fromId, CancellationToken cancellationToken = default) =>
        Connection.InvokeAsync<Slice<GameInfo>>("GetGamesSlice", fromId, cancellationToken);

    public async Task OpenAsync(string userName, CancellationToken cancellationToken = default)
    {
        if (_isOpened)
        {
            throw new InvalidOperationException("Client has been already opened");
        }

        _connection = new HubConnectionBuilder()
            .WithUrl($"{ServiceUri}sionline")
            .WithAutomaticReconnect(new ReconnectPolicy())
            .AddMessagePackProtocol()
            .Build();

        _connection.Reconnecting += async e =>
        {
            if (Reconnecting != null)
            {
                await Reconnecting(e);
            }
        };

        _connection.Reconnected += async s =>
        {
            if (Reconnected != null)
            {
                await Reconnected(s);
            }
        };

        _connection.Closed += OnConnectionClosedAsync;
        _connection.HandshakeTimeout = TimeSpan.FromMinutes(2);

        _connection.On<GameInfo>("GameCreated", (gameInfo) => OnUI(() => GameCreated?.Invoke(gameInfo)));
        _connection.On<int>("GameDeleted", (gameId) => OnUI(() => GameDeleted?.Invoke(gameId)));
        _connection.On<GameInfo>("GameChanged", (gameInfo) => OnUI(() => GameChanged?.Invoke(gameInfo)));
        _connection.On("Disconnect", () => _connection.StopAsync());

        await _connection.StartAsync(cancellationToken);

        _isOpened = true;
    }

    private Task OnConnectionClosedAsync(Exception? exc) => Closed != null ? Closed(exc) : Task.CompletedTask;

    private void OnUI(Action action)
    {
        if (_uIThreadExecutor != null)
        {
            _uIThreadExecutor.ExecuteOnUIThread(action);
            return;
        }

        action();
    }

    private sealed class ReconnectPolicy : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext) => TimeSpan.FromSeconds(5);
    }
}
