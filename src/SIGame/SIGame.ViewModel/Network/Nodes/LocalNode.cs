using SICore.Network.Configuration;

namespace SICore.Network.Servers;

/// <summary>
/// Defines a local node without any external connections.
/// </summary>
public sealed class LocalNode : PrimaryNode
{
    public LocalNode(NodeConfiguration serverConfiguration) : base(serverConfiguration) { }
}
