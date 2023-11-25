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

    public event Action<Message>? IncomingMessage;

    public event Func<Exception?, Task>? Reconnecting;
    public event Func<string?, Task>? Reconnected;
    public event Func<Exception?, Task>? Closed;

    public SIHostClient(HubConnection connection, SIHostClientOptions options)
    {
        _connection = connection;
        _options = options;

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

        _connection.On<Message>("Receive", (message) => IncomingMessage?.Invoke(message));

        _connection.On("Disconnect", async () =>
        {
            IncomingMessage?.Invoke(new Message(Resources.YourWereKicked, "@", isSystem: false));

            await _connection.StopAsync();
        });
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
                .WithAutomaticReconnect()
                .AddMessagePackProtocol()
                .Build();

        connection.HandshakeTimeout = options.HandshakeTimeout;

        await connection.StartAsync(cancellationToken);

        return new SIHostClient(connection, options);
    }

    public Task<JoinGameResponse> JoinGameAsync(
        JoinGameRequest joinGameRequest,
        CancellationToken cancellationToken = default) =>
        _connection.InvokeAsync<JoinGameResponse>("JoinGame", joinGameRequest, cancellationToken);

    public Task SendMessageAsync(Message message, CancellationToken cancellationToken = default) =>
        _connection.InvokeAsync("SendMessage", message, cancellationToken);

    public Task LeaveGameAsync(CancellationToken cancellationToken = default) =>
        _connection.InvokeAsync("LeaveGame", cancellationToken);

    private Task OnConnectionClosedAsync(Exception? exc)
    {
        // TODO: recreate connection and retry

        return Closed != null ? Closed(exc) : Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
