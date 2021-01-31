using SICore.Connections;
using System;

namespace SICore.Network.Contracts
{
    /// <summary>
    /// Интерфейс клиента
    /// </summary>
    public interface IClient : IDisposable
    {
        /// <summary>
        /// Имя клиента
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Сервер, к которому относится клиент
        /// </summary>
        IServer CurrentServer { get; }

        /// <summary>
        /// Получить входящее сообщение
        /// </summary>
        void AddIncomingMessage(Message message);

        /// <summary>
        /// Отправить сообщение
        /// </summary>
        event Action<IClient, Message> SendingMessage;

        /// <summary>
        /// Подключиться к серверу
        /// </summary>
        /// <param name="s">Сервер, к которому подключается клиент</param>
        void ConnectTo(IServer s);
    }
}
