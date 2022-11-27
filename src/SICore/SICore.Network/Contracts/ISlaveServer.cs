using SICore.Connections;

namespace SICore.Network.Contracts
{
    public interface ISlaveServer: INode
    {
        IConnection HostServer { get; }
    }
}
