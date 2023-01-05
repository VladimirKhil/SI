using SIData;

namespace SICore.Network.Contracts;

/// <summary>
/// Represents a node client.
/// </summary>
public interface IClient : IDisposable
{
    /// <summary>
    /// Client name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Client node.
    /// </summary>
    INode CurrentServer { get; }

    /// <summary>
    /// Receives incoming message.
    /// </summary>
    void AddIncomingMessage(in Message message);

    /// <summary>
    /// Message received event.
    /// </summary>
    event Func<Message, ValueTask> MessageReceived;

    /// <summary>
    /// Отправить сообщение
    /// </summary>
    event Action<IClient, Message> SendingMessage;

    /// <summary>
    /// Connects to node.
    /// </summary>
    /// <param name="s">Node to connect.</param>
    void ConnectTo(INode s);
}
