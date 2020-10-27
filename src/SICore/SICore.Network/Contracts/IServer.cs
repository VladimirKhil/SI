using SICore.Connections;
using System;
using System.Threading.Tasks;

namespace SICore.Network.Contracts
{
    /// <summary>
    /// Интерфейс сервера
    /// </summary>
    public interface IServer : IDisposable
    {
        bool IsMain { get; }

        object ClientsSync { get; }
        object ConnectionsSync { get; }

        void AddClient(IClient client);
        Task DeleteClientAsync(string name);
        bool Contains(string name);

        void OnError(Exception exc, bool isWarning);
        void ReplaceInfo(string name, IAccountInfo computerAccount);

        event Action<Message, Exception> SerializationError;
    }
}
