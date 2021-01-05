using SICore.Connections;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SICore.Network.Contracts
{
    /// <summary>
    /// Интерфейс сервера
    /// </summary>
    public interface IServer : IAsyncDisposable
    {
        bool IsMain { get; }

        Lock ConnectionsLock { get; }

        void AddClient(IClient client);

        bool DeleteClient(string name);

        bool Contains(string name);

        void OnError(Exception exc, bool isWarning);

        void ReplaceInfo(string name, IAccountInfo computerAccount);

        event Action<Message, Exception> SerializationError;
    }
}
