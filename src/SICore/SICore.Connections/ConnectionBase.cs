using SIData;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SICore.Connections
{
    /// <summary>
    /// Базовый класс для всех внешних подключений
    /// </summary>
    public abstract class ConnectionBase : IConnection
    {
        protected bool IsClosed { get; private set; } = false;

        public string ConnectionId { get; protected set; }

        /// <summary>
        /// Уникальный идентификатор внешнего сервера
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Доступные клиенты на внешнем сервере
        /// </summary>
        public List<string> Clients { get; } = new List<string>();

        public object ClientsSync { get; } = new object();

        /// <summary>
        /// Адрес удалённого подключения
        /// </summary>
        public abstract string RemoteAddress { get; }

        /// <summary>
        /// Авторизован ли удалённый клиент
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// Получено сообщение
        /// </summary>
        public event Action<IConnection, Message> MessageReceived;

        /// <summary>
        /// Соединение закрылось
        /// </summary>
        public event Action<IConnection, bool> ConnectionClose;

        /// <summary>
        /// Ошибка
        /// </summary>
        public event Action<Exception, bool> Error;

        public event Action<Message, Exception> SerializationError;
        public event Action Reconnecting;
        public event Action Reconnected;

        protected void OnReconnecting() => Reconnecting?.Invoke();
        protected void OnReconnected() => Reconnected?.Invoke();

        public string UserName { get; set; } = Guid.NewGuid().ToString();

        public int GameId { get; set; }

        public abstract ValueTask SendMessageAsync(Message m);

        public virtual void Close()
        {

        }

        protected virtual void CloseCore(bool informServer, bool withError = false)
        {
            if (IsClosed)
            {
                return;
            }

            Close();

            IsClosed = true;

            if (informServer)
            {
                OnConnectionClose(withError);
            }
        }

        protected void OnMessageReceived(Message message) => MessageReceived?.Invoke(this, message);

        protected void OnConnectionClose(bool withError)
        {
            try
            {
                ConnectionClose?.Invoke(this, withError);
            }
            catch (Exception exc)
            {
                OnError(exc, true);
            }
        }

        public void OnError(Exception exc, bool isWarning) => Error?.Invoke(exc, isWarning);

        protected void OnSerializationError(Message message, Exception exc) => SerializationError?.Invoke(message, exc);

        protected abstract ValueTask DisposeAsync(bool disposing);

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true);
            GC.SuppressFinalize(this);
        }
    }
}
