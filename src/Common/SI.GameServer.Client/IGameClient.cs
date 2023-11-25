using SIData;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SI.GameServer.Client;

/// <summary>
/// Defines common SI server client.
/// </summary>
public interface IGameClient : IAsyncDisposable
{
    /// <summary>
    /// Message received event.
    /// </summary>
    event Action<Message> IncomingMessage;

    /// <summary>
    /// Reconnecting event.
    /// </summary>
    event Func<Exception?, Task> Reconnecting;

    /// <summary>
    /// Reconnected event.
    /// </summary>
    event Func<string?, Task> Reconnected;

    /// <summary>
    /// Connection closed event.
    /// </summary>
    event Func<Exception?, Task> Closed;

    /// <summary>
    /// Sends message to server.
    /// </summary>
    /// <param name="message">Message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendMessageAsync(Message message, CancellationToken cancellationToken = default);
}
