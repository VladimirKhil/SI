using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace SICore.Connections
{
    /// <summary>
    /// Клиент, получающий сообщения от сервера по каналу TCP
    /// </summary>
    public abstract class TcpReadConnection: ConnectionBase
    {
        protected TcpClient _tcpClient = null;

        private readonly byte[] _buffer = new byte[5000];
        private readonly List<byte> _bufferCache = new List<byte>();
        private int _messageSize = -1;

        protected int ProtocolVersion { get; set; } = 1;
        
        protected readonly IConnectionLogger _logger;

        public override string RemoteAddress
        {
            get
            {
                try
                {
                    var address = _tcpClient.Client.RemoteEndPoint.ToString();
                    var index = address.IndexOf(':');
                    return index > -1 ? address.Substring(0, index) : address;
                }
                catch (ObjectDisposedException)
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// Создание внешнего серевера на основе TCP-соединения
        /// </summary>
        /// <param name="client">TCP-соединение</param>
        internal TcpReadConnection(TcpClient client, IConnectionLogger gameLogger)
        {
            _tcpClient = client;
            _logger = gameLogger;
        }

        public async void StartRead(bool waitForUpgrade, CancellationToken cancellationToken = default)
        {
            try
            {
                if (waitForUpgrade)
                {
                    await WaitForConnectionUpgradeAsync(_tcpClient);
                }

                if (ProtocolVersion == 2)
                {
                    var pipe = new Pipe();
                    var writing = FillPipeAsync(pipe.Writer, cancellationToken);
                    var reading = ReadPipeAsync(pipe.Reader, cancellationToken);

                    await Task.WhenAll(reading, writing);
                    // Нормальное закрытие соединения
                    CloseCore(true, false);
                    return;
                }

                var ns = _tcpClient.GetStream();
                ns.ReadTimeout = 20 * 1000;
                while (true)
                {
                    var bytesRead = await ns.ReadAsync(_buffer, 0, _buffer.Length);
                    if (bytesRead < 1)
                    {
                        // Нормальное закрытие соединения
                        CloseCore(true, false);
                        return;
                    }

                    _bufferCache.AddRange(_buffer.Take(bytesRead));

                    while (_messageSize > -1 && _bufferCache.Count >= _messageSize || _messageSize == -1 && _bufferCache.Count >= sizeof(int))
                    {
                        if (_messageSize == -1)
                        {
                            var sizeData = _bufferCache.Take(sizeof(int)).ToArray();
                            _bufferCache.RemoveRange(0, sizeof(int));
                            _messageSize = BitConverter.ToInt32(sizeData, 0);
                        }
                        else
                        {
                            var data = _bufferCache.Take(_messageSize).ToArray();
                            _bufferCache.RemoveRange(0, _messageSize);

                            _messageSize = -1;
                            if (Deserialize(data, out Message message))
                                OnMessageReceived(message);
                        }
                    }
                }
            }
            catch (IOException)
            {
                // Connection break
                CloseCore(true, true);
            }
            catch (InvalidOperationException)
            {
                CloseCore(true, true);
            }
            catch (SocketException exc)
            {
                CloseCore(true, true);
            }
            catch (Exception e)
            {
                OnError(e, false);
                CloseCore(true, true);
            }
        }

        private async Task FillPipeAsync(PipeWriter writer, CancellationToken cancellationToken = default)
        {
            var ns = _tcpClient.GetStream();
            ns.ReadTimeout = 20 * 1000;

            while (!cancellationToken.IsCancellationRequested)
            {
                //var memory = writer.GetMemory(_buffer.Length);
                try
                {
                    var bytesRead = await ns.ReadAsync(_buffer, 0, _buffer.Length, cancellationToken);
                    if (bytesRead < 1)
                    {
                        break;
                    }

                    await writer.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, bytesRead), cancellationToken);
                    //writer.Advance(bytesRead);
                }
                catch (ObjectDisposedException)
                {
                    // Normal dispose
                    break;
                }
                catch (Exception ex)
                {
                    OnError(ex, true);
                    break;
                }

                var result = await writer.FlushAsync(cancellationToken);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            writer.Complete();
        }

        private async Task ReadPipeAsync(PipeReader reader, CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await reader.ReadAsync(cancellationToken);

                var buffer = result.Buffer;
                SequencePosition? position;
                do
                {
                    position = buffer.PositionOf((byte)0);

                    if (position != null)
                    {
                        OnMessageReceived(MessageSerializer.DeserializeMessage(buffer.Slice(0, position.Value)));
                        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                    }
                } while (position != null);

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            reader.Complete();
        }

        private async Task WaitForConnectionUpgradeAsync(TcpClient tcpClient)
        {
            var buffer = new byte[5000];
            var networkStream = tcpClient.GetStream();

            var upgradeMessage = new StringBuilder();
            do
            {
                var bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead < 1)
                {
                    // Нормальное закрытие соединения
                    CloseCore(true, false);
                    return;
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

            string connectionIdHeader = "";
            if (!headers.TryGetValue("ConnectionId", out string connectionId))
            {
                connectionId = Guid.NewGuid().ToString();
                connectionIdHeader = $"\nConnectionId: {connectionId}";
            }

            if (headers.TryGetValue("Upgrade", out var protocol))
            {
                if (protocol == "sigame2")
                {
                    ProtocolVersion = 2;
                }
            }
            else
            {
                protocol = "sigame";
            }

            ConnectionId = connectionId;

            var response = $"HTTP/1.1 101 Switching Protocols\nUpgrade: {protocol}\nConnection: Upgrade{connectionIdHeader}\n\n";
            var bytes = Encoding.UTF8.GetBytes(response);
            await networkStream.WriteAsync(bytes, 0, bytes.Length);
        }

        protected bool Deserialize(byte[] data, out Message msg)
        {
            try
            {
                if (_logger != null)
                {
                    _logger.Log(GameId, $"{UserName} in: {Encoding.UTF8.GetString(data)}");
                }

                using var ms = new MemoryStream(data);
                using var reader = XmlReader.Create(ms);
                reader.Read();
                msg = Message.ReadXml(reader);
                return true;
            }
            catch (Exception exc)
            {
                OnError(new Exception($"Deserialization error of text: {Encoding.UTF8.GetString(data)}", exc), true);
            }

            msg = default;
            return false;
        }

        public override void Close() => _tcpClient.Dispose();
    }
}
