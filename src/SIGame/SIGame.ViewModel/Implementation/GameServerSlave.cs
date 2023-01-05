using SICore.Network.Configuration;
using SICore.Network.Contracts;
using SICore.Network.Servers;

namespace SIGame.ViewModel.Implementation;

public sealed class GameServerSlave : SecondaryNode
{
    public GameServerSlave(
        NodeConfiguration serverConfiguration,
        INetworkLocalizer networkLocalizer)
        : base(serverConfiguration, networkLocalizer)
    {
    }

    public override ValueTask ConnectAsync(bool upgrade) => default;
}
