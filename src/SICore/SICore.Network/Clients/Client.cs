using SICore.Connections;
using SICore.Network.Contracts;
using System;
using System.Diagnostics;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SICore.Network.Clients
{
    /// <summary>
    /// Класс-клиент
    /// </summary>
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
        /// Текущий сервер
        /// </summary>
        public IServer Server { get; private set; }

        /// <summary>
        /// Создание клиента
        /// </summary>
        /// <param name="name">Имя клиента</param>
        public Client(string name)
        {
            Name = name;
            WaitForMessages();
        }

        /// <summary>
        /// Получить входящее сообщение
        /// </summary>
        public void AddIncomingMessage(Message message) => _inMessages.Writer.TryWrite(message);

        /// <summary>
        /// Процедура обработки входящих сообщений
        /// </summary>
        private async void WaitForMessages()
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
                Server.OnError(exc, true);
            }
        }

        /// <summary>
        /// Текущий сервер
        /// </summary>
        public IServer CurrentServer => Server;

        /// <summary>
        /// Подсоединение к серверу
        /// </summary>
        /// <param name="server"></param>
        public void ConnectTo(IServer server)
        {
            server.AddClient(this);
            Server = server;
        }

        /// <summary>
        /// Отправка сообщения
        /// </summary>
        /// <param name="text">Текст сообщения</param>
        /// <param name="isSystem">Системное ли</param>
        /// <param name="receiver">Получатель</param>
        /// <param name="isPrivate">Приватное ли</param>
        public void SendMessage(string text, bool isSystem = true, string receiver = NetworkConstants.Everybody, bool isPrivate = false)
        {
            SendingMessage?.Invoke(this, new Message(text, Name, receiver, isSystem, isPrivate));
        }

        /// <summary>
        /// Получение сообщения. Гарантируется обработка сообщений строго по одному в том порядке, в котором они были получены
        /// </summary>
        public event Func<Message, ValueTask> MessageReceived;

        public event Action Disposed;

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

        public event Action<IClient, Message> SendingMessage;
    }
}
