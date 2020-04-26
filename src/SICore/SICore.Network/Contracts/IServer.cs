using SICore.Connections;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SICore.Network.Contracts
{
    /// <summary>
    /// Интерфейс сервера
    /// </summary>
    public interface IServer : IDisposable
    {
        bool IsMain { get; }
        IEnumerable<string> AllClients { get; }

        object ClientsSync { get; }
        object ConnectionsSync { get; }

        void AddClient(IClient client);
        Task DeleteClientAsync(string name);
        bool IsOnline(string name);
        bool IsOnlineInternal(string name);
        string IsOnlineString(string name);
        bool Contains(string name);

        void OnError(Exception exc, bool isWarning);
        void ReplaceInfo(string name, IAccountInfo computerAccount);

        event Action<Message> SerializationError;
    }
}
