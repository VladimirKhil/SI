using SIData;

namespace SI.GameServer.Contract;

/// <summary>
/// Defines SIHost client (messages pushed from host).
/// </summary>
public interface ISIHostClient
{
    /// <summary>
    /// Receives incoming message.
    /// </summary>
    /// <param name="message">Incoming message.</param>
    Task Receive(Message message);

    /// <summary>
    /// Forces client to disconnect from game (client has been kicked).
    /// </summary>
    Task Disconnect();
}
