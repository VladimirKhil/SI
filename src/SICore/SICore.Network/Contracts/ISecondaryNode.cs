using SICore.Connections;

namespace SICore.Network.Contracts;

public interface ISecondaryNode : INode
{
    IConnection HostServer { get; }
}
