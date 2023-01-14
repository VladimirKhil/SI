using SICore.Connections.Errors;
using SIData;
using System.Buffers;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace SICore.Connections;

/// <summary>
/// Представляет внешний сервер и доступных на нём клиентов
/// </summary>
public sealed class Connection : TcpReadConnection
{
    /// <summary>
    /// Пинг
    /// </summary>
    public const string PingMessage = "PING";

    private Timer? _timer;

    private DateTime _lastMessageTime = DateTime.UtcNow;

    private readonly Channel<Message> _outMessages = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions
    {
        SingleReader = true
    });

    private bool _isDisposed;

    /// <summary>
    /// Создание внешнего серевера на основе TCP-соединения
    /// </summary>
    /// <param name="client">TCP-соединение</param>
    public Connection(TcpClient client, IConnectionLogger? gameLogger, bool usePing = false)
        : base(client, gameLogger)
    {
        if (usePing)
        {
            _timer = new Timer(SendPing, null, 30000, 30000);
        }

        StartMessageLoop();
    }

    /// <summary>
    /// Процедура обработки исходящих сообщений
    /// </summary>
    private async void StartMessageLoop()
    {
        try
        {
            while (await _outMessages.Reader.WaitToReadAsync())
            {
                while (_outMessages.Reader.TryRead(out var message))
                {
                    await ProcessMessageAsync(message);
                }
            }
        }
        catch (OperationCanceledException)
        {

        }
        catch (Exception exc)
        {
            Trace.TraceError($"WaitForOutMessages error: {exc}");
        }
    }

    public async Task<string> UpgradeAsync(string serverAddress, string connectionId)
    {
        var connectionIdHeader = connectionId != null ? $"\nConnectionId: {connectionId}" : "";

        var upgradeText = $"GET / HTTP/1.1\nHost: {serverAddress}\nConnection: Upgrade{connectionIdHeader}\nUpgrade: sigame2\n\n";
        var bytes = Encoding.UTF8.GetBytes(upgradeText);
        // TODO: use Memory
        await _tcpClient.GetStream().WriteAsync(bytes, 0, bytes.Length);

        var buffer = new byte[5000];

        var upgradeMessage = new StringBuilder();
        do
        {
            // TODO: use Memory
            var bytesRead = await _tcpClient.GetStream().ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead < 1)
            {
                // Нормальное закрытие соединения
                _tcpClient.Close();
                throw new ConnectionException();
            }

            upgradeMessage.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
        } while (!upgradeMessage.ToString().EndsWith("\n\n") && !upgradeMessage.ToString().EndsWith("\r\n\r\n"));

        var headers = upgradeMessage
            .ToString()
            .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .Select(headerString =>
            {
                var split = headerString.Split(new[] { ": " }, StringSplitOptions.None);
                return new { Name = split[0], Value = split[1] };
            })
            .ToDictionary(val => val.Name, val => val.Value);

        if (!headers.TryGetValue("ConnectionId", out string connectionIdFromServer))
        {
            connectionIdFromServer = connectionId;
        }

        if (headers.TryGetValue("Upgrade", out var protocol) && protocol == "sigame2")
        {
            ProtocolVersion = 2;
        }

        return connectionIdFromServer;
    }

    private async Task ProcessMessageAsync(Message message)
    {
        try
        {
            if (IsClosed)
            {
                return;
            }

            if (ProtocolVersion == 2)
            {
                var bufferSize = MessageSerializer.GetBufferSizeForMessage(message);
                var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                try
                {
                    MessageSerializer.SerializeMessage(message, buffer);
                    // TODO: use Memory
                    await _tcpClient.GetStream().WriteAsync(buffer, 0, bufferSize);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            else
            {
                var data = message.Serialize();

                var sizeData = BitConverter.GetBytes(data.Length);

                lock (_tcpClient)
                {
                    var stream = _tcpClient.GetStream();

                    // TODO: PERF: async Write. Use Pipelines

                    stream.Write(sizeData, 0, sizeof(int));
                    stream.Write(data, 0, data.Length);
                }
            }

            _lastMessageTime = DateTime.UtcNow;
        }
        catch (IOException)
        {
            // Соединение было закрыто
            CloseCore(true, true);
        }
        catch (InvalidOperationException)
        {
            // Соединение было закрыто
            CloseCore(true, true);
        }
        catch (ArgumentException e) when (e.Message.Contains("surrogate") || e.Message.Contains("is an invalid character"))
        {
            OnSerializationError(message, e);
        }
        catch (Exception e)
        {
            OnError(new Exception($"{message.Sender}|{message.Receiver}|{message.Text}", e), false);
        }
    }

    /// <summary>
    /// Pings the server.
    /// </summary>
    /// <remarks>Not Async because it is called by the timer.</remarks>
    private async void SendPing(object? state)
    {
        try
        {
            if (DateTime.UtcNow.Subtract(_lastMessageTime).TotalSeconds > 30)
            {
                await SendMessageAsync(new Message(PingMessage, UserName));
            }
        }
        catch (Exception exc)
        {
            Trace.TraceError("SendPing error: " + exc);
        }
    }

    /// <summary>
    /// Оправка сообщения на внешний сервер
    /// </summary>
    /// <param name="m">Отправляемое сообщение</param>
    public override ValueTask SendMessageAsync(Message m) => _outMessages.Writer.WriteAsync(m);

    public override void Close()
    {
        base.Close();

        if (_timer != null)
        {
            _timer.Dispose();
            _timer = null;
        }
    }

    protected override ValueTask DisposeAsync(bool disposing)
    {
        if (_isDisposed)
        {
            return default;
        }

        try
        {
            _outMessages.Writer.Complete();
        }
        catch (ChannelClosedException)
        {

        }

        _isDisposed = true;

        CloseCore(false);

        return default;
    }
}
