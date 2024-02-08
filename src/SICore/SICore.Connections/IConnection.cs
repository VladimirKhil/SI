using SIData;

namespace SICore.Connections;

/// <summary>
/// Represents a connection to an external node.
/// </summary>
public interface IConnection : IAsyncDisposable
{
    object ClientsSync { get; }

    string ConnectionId { get; }

    string Id { get; }

    List<string> Clients { get; }

    string RemoteAddress { get; }

    bool IsAuthenticated { get; set; }

    int GameId { get; set; }

    string UserName { get; set; }

    event Action<IConnection, Message> MessageReceived;
    event Action<IConnection, bool> ConnectionClose;
    event Action<Exception, bool> Error;
    event Action<Message, Exception> SerializationError;

    event Action Reconnecting;
    event Action Reconnected;

    ValueTask SendMessageAsync(Message m);
    void Close();
}
