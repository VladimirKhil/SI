using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SI.GameServer.Client.Properties;
using SI.GameServer.Contract;
using SIData;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
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

    private readonly CookieContainer _cookieContainer;
    private readonly HttpClientHandler _httpClientHandler;
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
    public event Action<string>? Joined;
    public event Action<string>? Leaved;
    public event Action<string, string>? Receieve;
    public event Action<Message>? IncomingMessage;

    public event Func<Exception?, Task>? Closed;
    public event Func<Exception?, Task>? Reconnecting;
    public event Func<string?, Task>? Reconnected;

    private readonly IUIThreadExecutor? _uIThreadExecutor;

    public GameServerClient(IOptions<GameServerClientOptions> options, IUIThreadExecutor? uIThreadExecutor = null)
    {
        _options = options.Value;
        _uIThreadExecutor = uIThreadExecutor;

        _cookieContainer = new CookieContainer();
        _httpClientHandler = new HttpClientHandler { CookieContainer = _cookieContainer };

        _client = new HttpClient(_httpClientHandler)
        {
            BaseAddress = new Uri(ServiceUri),
            Timeout = _options.Timeout,
            DefaultRequestVersion = HttpVersion.Version20
        };
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
        _httpClientHandler?.Dispose();
    }

    public Task JoinLobbyAsync(CancellationToken cancellationToken = default) =>
        _connection.InvokeAsync("JoinLobby2", Thread.CurrentThread.CurrentUICulture.Name, cancellationToken);

    public Task LeaveLobbyAsync(CancellationToken cancellationToken = default) =>
        _connection.InvokeAsync("LeaveLobby", cancellationToken);

    public Task<Slice<GameInfo>> GetGamesAsync(int fromId, CancellationToken cancellationToken = default) =>
        _connection.InvokeAsync<Slice<GameInfo>>("GetGamesSlice", fromId, cancellationToken);

    public Task<HostInfo> GetGamesHostInfoAsync(CancellationToken cancellationToken = default) =>
        _connection.InvokeAsync<HostInfo>("GetGamesHostInfoNew", Thread.CurrentThread.CurrentUICulture.Name, cancellationToken);

    public Task<string> GetNewsAsync(CancellationToken cancellationToken = default) =>
        _connection.InvokeAsync<string>("GetNews", cancellationToken);

    public Task<ChatMessage[]> GetLatestChatMessagesAsync(CancellationToken cancellationToken = default) =>
        _connection.InvokeAsync<ChatMessage[]>("GetLatestChatMessages", cancellationToken);

    public Task<string[]> GetUsersAsync(CancellationToken cancellationToken = default) =>
        _connection.InvokeAsync<string[]>("GetUsers", cancellationToken);

    private async Task AuthenticateUserAsync(
        string user,
        string password,
        CancellationToken cancellationToken = default)
    {
        var uri = "api/Account/LogOn";

        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["login"] = user,
                ["password"] = password
            });

        var response = await _client.PostAsync(uri, content, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw response.StatusCode switch
        {
            HttpStatusCode.Conflict => new Exception(Resources.OnlineUserConflict),
            HttpStatusCode.Forbidden => new Exception(Resources.LoginForbidden),
            _ => new Exception($"Error ({response.StatusCode}): {await response.Content.ReadAsStringAsync(cancellationToken)}"),
        };
    }

    public async Task OpenAsync(string userName, CancellationToken cancellationToken = default)
    {
        if (_isOpened)
        {
            throw new InvalidOperationException("Client has been already opened");
        }

        await AuthenticateUserAsync(userName, "", cancellationToken);

        _connection = new HubConnectionBuilder()
            .WithUrl(
                $"{ServiceUri}sionline",
                options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(Convert.ToBase64String(Encoding.UTF8.GetBytes(userName)));
                    options.Cookies = _cookieContainer;
                })
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

        _connection.On<string, string>("Say", (user, text) => OnUI(() => Receieve?.Invoke(user, text)));
        _connection.On<GameInfo>("GameCreated", (gameInfo) => OnUI(() => GameCreated?.Invoke(gameInfo)));
        _connection.On<int>("GameDeleted", (gameId) => OnUI(() => GameDeleted?.Invoke(gameId)));
        _connection.On<GameInfo>("GameChanged", (gameInfo) => OnUI(() => GameChanged?.Invoke(gameInfo)));
        _connection.On<string>("Joined", (user) => OnUI(() => Joined?.Invoke(user)));
        _connection.On<string>("Leaved", (user) => OnUI(() => Leaved?.Invoke(user)));
        _connection.On<Message>("Receive", (message) => IncomingMessage?.Invoke(message));

        _connection.On("Disconnect", async () =>
        {
            IncomingMessage?.Invoke(new Message(Resources.YourWereKicked, "@", isSystem: false));

            await _connection.StopAsync();
        });

        await _connection.StartAsync(cancellationToken);

        _isOpened = true;
    }

    public Task SayAsync(string message) => _connection.InvokeAsync("Say", message);

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
