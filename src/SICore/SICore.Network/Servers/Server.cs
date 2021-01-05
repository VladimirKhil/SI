using Notions;
using SICore.Connections;
using SICore.Network.Configuration;
using SICore.Network.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using R = SICore.Network.Properties.Resources;

namespace SICore.Network.Servers
{
    /// <summary>
    /// Класс, имитирующий работу сервера. Обеспечивает пересылку сообщений между клиентами
    /// </summary>
    public abstract class Server : IServer
    {
        private const char AnonymousSenderPrefix = '\n';

        private readonly ServerConfiguration _serverConfiguration;

        /// <summary>
        /// Список доступных клиентов
        /// </summary>
        private readonly ConcurrentDictionary<string, IClient> _clients = new ConcurrentDictionary<string, IClient>();

        public Lock ConnectionsLock { get; } = new Lock(nameof(ConnectionsLock));

        /// <summary>
        /// Является ли сервер главным
        /// </summary>
        public abstract bool IsMain { get; }

        public event Action<Exception, bool> Error;

        public event Action<Message, Exception> SerializationError;

        public event Action Reconnecting;
        public event Action Reconnected;

        protected void OnReconnecting() => Reconnecting?.Invoke();
        protected void OnReconnected() => Reconnected?.Invoke();

        public void OnError(Exception exc, bool isWarning) => Error?.Invoke(exc, isWarning);

        protected abstract IEnumerable<IConnection> Connections { get; }

        public virtual ValueTask<bool> AddConnectionAsync(IConnection connection)
        {
            connection.MessageReceived += Connection_MessageReceived;
            connection.ConnectionClose += Connection_ConnectionClosed;
            connection.Error += OnError;
            connection.SerializationError += Connection_SerializationError;

            return new ValueTask<bool>(true);
        }

        public virtual async ValueTask RemoveConnectionAsync(IConnection connection, bool withError)
        {
            ClearListeners(connection);
            await connection.DisposeAsync();

            ConnectionClosed?.Invoke(withError);
        }

        protected void ClearListeners(IConnection connection)
        {
            connection.MessageReceived -= Connection_MessageReceived;
            connection.ConnectionClose -= Connection_ConnectionClosed;
            connection.Error -= OnError;
            connection.SerializationError -= Connection_SerializationError;
        }

        protected readonly INetworkLocalizer _localizer;

        protected Server(ServerConfiguration serverConfiguration, INetworkLocalizer localizer)
        {
            _serverConfiguration = serverConfiguration;
            _localizer = localizer;
        }

        public event Action<bool> ConnectionClosed;

        protected async void Connection_ConnectionClosed(IConnection connection, bool withError)
        {
            try
            {
                await RemoveConnectionAsync(connection, withError);
            }
            catch (Exception exc)
            {
                OnError(exc, false);
            }

            if (IsMain)
            {
                string clientName = null;
                lock (connection.ClientsSync)
                {
                    if (connection.Clients.Count > 0)
                    {
                        clientName = connection.Clients[0];
                    }
                }

                if (clientName != null)
                {
                    var m = new Message(string.Join(Message.ArgsSeparator, SystemMessages.Disconnect, clientName, (withError ? "+" : "-")), "", NetworkConstants.GameName);
                    await ProcessOutgoingMessageAsync(m);
                }
            }
            else
            {
                foreach (var client in _clients.Values)
                {
                    var m = new Message(SystemMessages.Disconnect, "", client.Name);
                    await ProcessOutgoingMessageAsync(m);
                }
            }
        }

        /// <summary>
        /// Получено сообщение от внешнего сервера
        /// </summary>
        /// <param name="connection">Сервер, от которого пришло сообщение</param>
        /// <param name="m">Присланное сообщение</param>
        private async void Connection_MessageReceived(IConnection connection, Message m)
        {
            try
            {
                string sender = m.Sender, receiver = m.Receiver;
                if (string.IsNullOrEmpty(receiver))
                {
                    receiver = IsMain ? NetworkConstants.GameName : NetworkConstants.Everybody;
                }

                var emptySender = string.IsNullOrEmpty(sender);

                if (emptySender)
                {
                    if (!IsMain)
                    {
                        OnError(new Exception($"{_localizer[nameof(R.UnknownSenderMessage)]}: {m.Text}"), true);
                        return;
                    }

                    sender = AnonymousSenderPrefix + connection.Id;
                }
                else
                {
                    lock (connection.ClientsSync)
                    {
                        if (sender != NetworkConstants.GameName && !connection.Clients.Contains(sender))
                        {
                            return; // Защита от подлога
                        }
                    }
                }

                var messageText = m.Text;
                if (!m.IsSystem)
                {
                    messageText = messageText.Shorten(_serverConfiguration.MaxChatMessageLength);
                }

                if (sender != m.Sender || receiver != m.Receiver || messageText != m.Text)
                {
                    m = new Message(messageText, sender, receiver, m.IsSystem, m.IsPrivate);
                }

                await ProcessIncomingMessageAsync(m);
            }
            catch (Exception exc)
            {
                OnError(new Exception($"Message: {m.Text}", exc), false);
            }
        }

        private async ValueTask ProcessIncomingMessageAsync(Message message)
        {
            foreach (var client in _clients.Values)
            {
                if (message.Receiver == client.Name || message.Receiver == NetworkConstants.Everybody || string.IsNullOrEmpty(client.Name) || !message.IsSystem && !message.IsPrivate)
                {
                    client.AddIncomingMessage(message);
                }
            }

            if (IsMain)
            {
                // Надо переслать это сообщение остальным
                await ConnectionsLock.WithLockAsync(async () =>
                {
                    foreach (var connection in Connections)
                    {
                        bool send;

                        if (IsMain)
                        {
                            send = (connection.UserName != message.Sender)
                                && ((connection.UserName == message.Receiver)
                                || message.Receiver == NetworkConstants.Everybody && connection.IsAuthenticated);
                        }
                        else
                        {
                            lock (connection.ClientsSync)
                            {
                                send = !connection.Clients.Contains(message.Sender)
                                    && (connection.Clients.Contains(message.Receiver)
                                    || message.Receiver == NetworkConstants.Everybody && connection.IsAuthenticated);
                            }
                        }

                        if (send)
                        {
                            await connection.SendMessageAsync(message);
                        }
                    }
                });
            }
        }

        private async ValueTask ProcessOutgoingMessageAsync(Message message)
        {
            foreach (var client in _clients.Values)
            {
                if ((message.Receiver == client.Name || client.Name.Length == 0 || message.Receiver == NetworkConstants.Everybody || !message.IsSystem && !message.IsPrivate) && client.Name != message.Sender)
                {
                    client.AddIncomingMessage(message);
                }
            }

            await ConnectionsLock.WithLockAsync(async () =>
            {
                foreach (var connection in Connections)
                {
                    bool send;

                    if (IsMain)
                    {
                        send = (connection.UserName != message.Sender)
                            && (message.Receiver == NetworkConstants.Everybody && connection.IsAuthenticated
                            || (connection.UserName == message.Receiver));
                    }
                    else
                    {
                        lock (connection.ClientsSync)
                        {
                            send = !connection.Clients.Contains(message.Sender) &&
                                (message.Receiver == NetworkConstants.Everybody && connection.IsAuthenticated
                                || connection.Clients.Contains(message.Receiver));
                        }
                    }

                    if (send)
                    {
                        await connection.SendMessageAsync(message);
                    }
                }
            });
        }

        public bool Contains(string name) => _clients.ContainsKey(name);

        /// <summary>
        /// Добавление нового клиента
        /// </summary>
        /// <param name="client">Добавляемый клиент</param>
        public void AddClient(IClient client)
        {
            if (_clients.Values.Contains(client))
            {
                return;
            }

            if (_clients.ContainsKey(client.Name))
            {
                throw new Exception(_localizer[nameof(R.ClientWithThisNameAlreadyExists)]);
            }

            _clients.AddOrUpdate(client.Name, client, (key, oldValue) => oldValue);

            client.SendingMessage += Client_SendingMessage;
        }

        private void Connection_SerializationError(Message message, Exception exc) => SerializationError?.Invoke(message, exc);

        public bool DeleteClient(string name) => _clients.TryRemove(name, out _);

        public void ReplaceInfo(string name, IAccountInfo computerAccount)
        {
            if (_clients.TryGetValue(name, out var client))
            {
                client.ReplaceInfo(computerAccount);
            }
        }

        /// <summary>
        /// Клиент отправляет сообщение
        /// </summary>
        /// <param name="obj">Отправляемое сообщение</param>
        private async void Client_SendingMessage(IClient sender, Message m)
        {
            if (string.IsNullOrWhiteSpace(m.Receiver))
            {
                return;
            }

            if (m.Receiver[0] == AnonymousSenderPrefix)
            {
                // Анонимное сообщение (серверу)
                var connection = await ConnectionsLock.WithLockAsync(() =>
                    Connections.FirstOrDefault(conn => conn.Id == m.Receiver.Substring(1)));

                if (connection != null)
                {
                    await connection.SendMessageAsync(
                        new Message(m.Text, m.Sender, NetworkConstants.Everybody, m.IsSystem, m.IsPrivate));
                }
            }
            else
            {
                await ProcessOutgoingMessageAsync(m);
            }
        }

        /// <summary>
        /// Закрытие сервера
        /// </summary>
        protected virtual ValueTask DisposeAsync(bool disposing)
        {
            var clientArray = _clients.Values.ToArray();
            _clients.Clear();

            foreach (var client in clientArray)
            {
                client.Dispose();
            }

            ConnectionsLock.Dispose();

            return default;
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true);
            GC.SuppressFinalize(this);
        }
    }
}
