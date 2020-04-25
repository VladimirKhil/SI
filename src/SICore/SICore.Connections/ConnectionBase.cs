using System;
using System.Collections.Generic;

namespace SICore.Connections
{
    /// <summary>
    /// Базовый класс для всех внешних серверов
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

        public event Action<Message> SerializationError;

		public string UserName { get; set; } = Guid.NewGuid().ToString();

		public int GameId { get; set; }

		public abstract void SendMessage(Message m);

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
			System.Threading.Tasks.Task.Run(() =>
			{
				ConnectionClose?.Invoke(this, withError);
			}).ContinueWith(task =>
			{
				if (task.IsFaulted)
				{
					OnError(task.Exception.InnerException, true);
				}
			});
        }

        public void OnError(Exception exc, bool isWarning) => Error?.Invoke(exc, isWarning);

        protected void OnSerializationError(Message message) => SerializationError?.Invoke(message);

        protected abstract void Dispose(bool disposing);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
