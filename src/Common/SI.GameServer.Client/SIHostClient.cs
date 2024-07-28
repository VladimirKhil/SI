using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using SI.GameServer.Client.Properties;
using SI.GameServer.Contract;
using SIData;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SI.GameServer.Client;

/// <summary>
/// Defines SIHost client.
/// </summary>
public sealed class SIHostClient : IGameClient
{
    private readonly HubConnection _connection;
    private readonly SIHostClientOptions _options;

    private JoinGameRequest? _joinGameRequest;

    public event Action<Message>? IncomingMessage;

    public event Action<int, ConnectionPersonData[]>? PersonChanged;

    public event Func<Exception?, Task>? Reconnecting;
    public event Func<string?, Task>? Reconnected;
    public event Func<Exception?, Task>? Closed;

    public SIHostClient(HubConnection connection, SIHostClientOptions options)
    {
        _connection = connection;
        _options = options;

        _connection.Reconnecting += OnReconnecting;
        _connection.Reconnected += OnReconnected;
        _connection.Closed += OnConnectionClosedAsync;

        _connection.On<Message>(nameof(ISIHostClient.Receive), (message) => IncomingMessage?.Invoke(message));

        _connection.On(nameof(ISIHostClient.Disconnect), async () =>
        {
            IncomingMessage?.Invoke(new Message(Resources.YourWereKicked, "@", isSystem: false));

            await _connection.StopAsync();
        });

        _connection.On<int, ConnectionPersonData[]>(
            nameof(ISIHostClient.GamePersonsChanged),
            (gameId, persons) => PersonChanged?.Invoke(gameId, persons));
    }

    private async Task OnReconnecting(Exception? ex)
    {
        if (Reconnecting != null)
        {
            await Reconnecting(ex);
        }
    }

    private async Task OnReconnected(string? connectionId)
    {
        if (Reconnected != null)
        {
            await Reconnected(connectionId);
        }
    }

    public static async Task<SIHostClient> CreateAsync(SIHostClientOptions options, CancellationToken cancellationToken)
    {
        var serviceUri = options.ServiceUri?.ToString() ?? "";

        if (!serviceUri.EndsWith('/'))
        {
            serviceUri += "/";
        }

        var connection = new HubConnectionBuilder()
            .WithUrl($"{serviceUri}sihost")
                .WithAutomaticReconnect(new ReconnectPolicy())
                .AddMessagePackProtocol()
                .Build();

        connection.HandshakeTimeout = options.HandshakeTimeout;

        await connection.StartAsync(cancellationToken);

        return new SIHostClient(connection, options);
    }

    public async Task<JoinGameResponse> JoinGameAsync(
        JoinGameRequest joinGameRequest,
        CancellationToken cancellationToken = default)
    {
        var response = await _connection.InvokeAsync<JoinGameResponse>("JoinGame", joinGameRequest, cancellationToken);

        if (response.IsSuccess)
        {
            _joinGameRequest = joinGameRequest;
        }

        return response;
    }

    public Task SendMessageAsync(Message message, CancellationToken cancellationToken = default) =>
        _connection.SendAsync("SendMessage", message, cancellationToken);

    public Task LeaveGameAsync(CancellationToken cancellationToken = default) =>
        _connection.SendAsync("LeaveGame", cancellationToken);

    private async Task OnConnectionClosedAsync(Exception? exc)
    {
        if (await TryReconnectAsync())
        {
            return;
        }

        if (Closed != null)
        {
            await Closed(exc);
        }
    }

    private async Task<bool> TryReconnectAsync()
    {
        if (_joinGameRequest == null)
        {
            return false;
        }

        try
        {
            await Task.Delay(5000);
            await _connection.StartAsync();
            await JoinGameAsync(_joinGameRequest);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _connection.Reconnecting -= OnReconnecting;
        _connection.Reconnected -= OnReconnected;
        _connection.Closed -= OnConnectionClosedAsync;

        await _connection.DisposeAsync();
    }

    private sealed class ReconnectPolicy : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext) => TimeSpan.FromSeconds(5);
    }
}
