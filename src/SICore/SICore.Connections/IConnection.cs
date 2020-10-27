using System;
using System.Collections.Generic;

namespace SICore.Connections
{
    /// <summary>
    /// Ссылка на внешнее подключение
    /// </summary>
    public interface IConnection: IDisposable
    {
        object ClientsSync { get; }
        string ConnectionId { get; }
        string Id { get; }

        List<string> Clients { get; }

        event Action<IConnection, Message> MessageReceived;
        event Action<IConnection, bool> ConnectionClose;
        event Action<Exception, bool> Error;
        event Action<Message, Exception> SerializationError;

        void SendMessage(Message m);
        void Close();

        string RemoteAddress { get; }
        bool IsAuthenticated { get; set; }
        int GameId { get; set; }
        string UserName { get; set; }
    }
}
