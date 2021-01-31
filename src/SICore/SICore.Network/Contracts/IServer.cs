using SICore.Connections;
using System;
using System.Collections.Generic;

namespace SICore.Network.Contracts
{
    /// <summary>
    /// Интерфейс сервера
    /// </summary>
    public interface IServer : IAsyncDisposable
    {
        bool IsMain { get; }

        IEnumerable<IConnection> Connections { get; }

        Lock ConnectionsLock { get; }

        void AddClient(IClient client);

        bool DeleteClient(string name);

        bool Contains(string name);

        void OnError(Exception exc, bool isWarning);

        event Action<Message, Exception> SerializationError;
    }
}
