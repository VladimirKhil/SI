using SICore.Connections;
using SIData;
using Utils;

namespace SICore.Network.Contracts;

/// <summary>
/// Represent a SI network node. Nodes could be connected to each other.
/// Clients connect to some node and communicate with other client via it.
/// </summary>
public interface INode : IAsyncDisposable
{
    /// <summary>
    /// Main node flag. Other nodes are connected to it ("star" topology).
    /// </summary>
    bool IsMain { get; }

    /// <summary>
    /// Connections to other nodes. For a slave node this collection always contain a single connection.
    /// </summary>
    IEnumerable<IConnection> Connections { get; }

    /// <summary>
    /// A lock object to manage <see cref="Connections" /> collection.
    /// </summary>
    Lock ConnectionsLock { get; }

    /// <summary>
    /// Adds client to node.
    /// </summary>
    /// <param name="client">Client to add.</param>
    void AddClient(IClient client);

    /// <summary>
    /// Removes client from node.
    /// </summary>
    /// <param name="name">Client name.</param>
    /// <returns>Has the client been successfully removed.</returns>
    bool DeleteClient(string name);

    /// <summary>
    /// Checks if the node contains a client with provided name.
    /// </summary>
    /// <param name="name">Client name.</param>
    bool Contains(string name);

    /// <summary>
    /// Provides an error callback for node operations.
    /// </summary>
    /// <param name="exc">Happened exception.</param>
    /// <param name="isWarning">Is the a warning level exception (otherwise an error level exception).</param>
    void OnError(Exception exc, bool isWarning);

    /// <summary>
    /// Provides a serialization error callback for node operations.
    /// </summary>
    event Action<Message, Exception> SerializationError;
}
