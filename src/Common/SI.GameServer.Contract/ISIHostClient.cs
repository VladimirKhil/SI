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

    /// <summary>
    /// Notifies that game persons have been changed.
    /// </summary>
    /// <param name="gameId">Game identifier.</param>
    /// <param name="persons">Game persons info.</param>
    Task GamePersonsChanged(int gameId, ConnectionPersonData[] persons);
}
