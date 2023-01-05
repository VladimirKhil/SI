using SICore.Network.Contracts;
using SIData;
using System.Diagnostics;
using System.Threading.Channels;

namespace SICore.Network.Clients;

/// <inheritdoc cref="IClient" />
public sealed class Client : IClient
{
    /// <summary>
    /// Входящие сообщения
    /// </summary>
    private readonly Channel<Message> _inMessages = Channel.CreateUnbounded<Message>(
        new UnboundedChannelOptions
        {
            SingleReader = true
        });

    private bool _isDisposed;

    /// <summary>
    /// Имя клиента
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// A node that the client is currently connected to.
    /// </summary>
    public INode Node { get; private set; }

    /// <summary>
    /// Получение сообщения. Гарантируется обработка сообщений строго по одному в том порядке, в котором они были получены
    /// </summary>
    public event Func<Message, ValueTask>? MessageReceived;

    public event Action<IClient, Message>? SendingMessage;

    public event Action? Disposed;

    /// <summary>
    /// Создание клиента
    /// </summary>
    /// <param name="name">Имя клиента</param>
    public Client(string name)
    {
        Name = name;
        StartMessageLoop();
    }

    private Client(string name, INode node)
    {
        Name = name;
        Node = node;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Client" /> connected to a node.
    /// </summary>
    /// <param name="name">Client name.</param>
    /// <param name="node">A node that client is connected to.</param>
    public static Client Create(string name, INode node)
    {
        var client = new Client(name, node);
        node.AddClient(client);
        client.StartMessageLoop();

        return client;
    }

    /// <summary>
    /// Adds a new message to incoming messages.
    /// </summary>
    public void AddIncomingMessage(in Message message) => _inMessages.Writer.TryWrite(message);

    /// <summary>
    /// Begins listening for incoming messages.
    /// </summary>
    private async void StartMessageLoop()
    {
        try
        {
            while (await _inMessages.Reader.WaitToReadAsync())
            {
                while (_inMessages.Reader.TryRead(out var message))
                {
                    var task = MessageReceived?.Invoke(message);
                    if (task.HasValue)
                    {
                        await task.Value;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            Trace.TraceWarning($"{Name}: WaitForMessages cancelled");
        }
        catch (Exception exc)
        {
            Trace.TraceError($"{Name}: WaitForMessages error: {exc}");
            Node.OnError(exc, true);
        }
    }

    /// <summary>
    /// Текущий сервер
    /// </summary>
    public INode CurrentServer => Node;

    /// <summary>
    /// Подсоединение к серверу
    /// </summary>
    /// <param name="node"></param>
    public void ConnectTo(INode node)
    {
        node.AddClient(this);
        Node = node;
    }

    /// <summary>
    /// Отправка сообщения
    /// </summary>
    /// <param name="text">Текст сообщения</param>
    /// <param name="isSystem">Системное ли</param>
    /// <param name="receiver">Получатель</param>
    /// <param name="isPrivate">Приватное ли</param>
    public void SendMessage(string text, bool isSystem = true, string receiver = NetworkConstants.Everybody, bool isPrivate = false) =>
        SendingMessage?.Invoke(this, new Message(text, Name, receiver, isSystem, isPrivate));

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        try
        {
            _inMessages.Writer.Complete();
        }
        catch (ChannelClosedException)
        {

        }

        _isDisposed = true;

        Disposed?.Invoke();
    }
}
