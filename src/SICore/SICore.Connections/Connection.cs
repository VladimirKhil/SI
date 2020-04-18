using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;

namespace SICore.Connections
{
    /// <summary>
    /// Представляет внешний сервер и доступных на нём клиентов
    /// </summary>
    public sealed class Connection: TcpReadConnection
    {
        /// <summary>
        /// Пинг
        /// </summary>
        private const string PingMessage = "PING";

        private Timer _timer;
        private DateTime _lastMessageTime = DateTime.Now;

        private readonly Channel<Message> _outMessages = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions
        {
            SingleReader = true
        });

        private bool _isDisposed;

        /// <summary>
        /// Создание внешнего серевера на основе TCP-соединения
        /// </summary>
        /// <param name="client">TCP-соединение</param>
        public Connection(TcpClient client, IConnectionLogger gameLogger, bool usePing = false)
            : base(client, gameLogger)
        {
            if (usePing)
            {
                _timer = new Timer(SendPing, null, 30000, 30000);
            }

            WaitForOutMessages();
        }

        /// <summary>
        /// Процедура обработки исходящих сообщений
        /// </summary>
        private async void WaitForOutMessages()
        {
            try
            {
                while (await _outMessages.Reader.WaitToReadAsync())
                {
                    while (_outMessages.Reader.TryRead(out var message))
                    {
                        ProcessMessage(message);
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

        private void ProcessMessage(Message message)
        {
            try
            {
                if (IsClosed)
                    return;

                var data = message.Serialize();

                var sizeData = BitConverter.GetBytes(data.Length);

                lock (_tcpClient)
                {
                    var stream = _tcpClient.GetStream();

                    // TODO: PERF: async Write. Use Pipelines

                    stream.Write(sizeData, 0, sizeof(int));
                    stream.Write(data, 0, data.Length);
                }

                _lastMessageTime = DateTime.Now;
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
            catch (Exception e)
            {
                OnError(new Exception($"{message.Sender}|{message.Receiver}|{message.Text}", e), false);
            }
        }

        private void SendPing(object state)
        {
            if (DateTime.Now.Subtract(_lastMessageTime).TotalSeconds > 30)
            {
                SendMessage(new Message(PingMessage, UserName));
            }
        }

        /// <summary>
        /// Оправка сообщения на внешний сервер
        /// </summary>
        /// <param name="m">Отправляемое сообщение</param>
        public override void SendMessage(Message m) => _outMessages.Writer.TryWrite(m);

        public override void Close()
        {
            base.Close();

            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
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
        }
    }
}
