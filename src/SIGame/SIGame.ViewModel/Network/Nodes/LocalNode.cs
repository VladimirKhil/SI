using SICore.Network.Configuration;
using SICore.Network.Contracts;

namespace SICore.Network.Servers;

/// <summary>
/// Defines a local node without any external connections.
/// </summary>
public sealed class LocalNode : PrimaryNode
{
    public LocalNode(NodeConfiguration serverConfiguration, INetworkLocalizer localizer)
        : base(serverConfiguration, localizer) { }
}
