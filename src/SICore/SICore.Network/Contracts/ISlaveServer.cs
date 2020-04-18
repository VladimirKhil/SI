using SICore.Connections;

namespace SICore.Network.Contracts
{
    public interface ISlaveServer: IServer
    {
        IConnection HostServer { get; }
    }
}
