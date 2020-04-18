using SICore.Connections;
using System.Collections.Generic;

namespace SICore.Network.Contracts
{
    public interface IMasterServer: IServer
    {
        IEnumerable<IConnection> ExternalServers { get; }

        void Kick(string name, bool ban = false);
    }
}
