using SICore.Connections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SICore.Network.Contracts
{
    public interface IMasterServer: IServer
    {
        IEnumerable<IConnection> ExternalServers { get; }

        ValueTask KickAsync(string name, bool ban = false);
    }
}
