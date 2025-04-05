using SICore.Network.Configuration;
using SICore.Network.Servers;

namespace SIGame.ViewModel.Implementation;

public sealed class GameServerSlave : SecondaryNode
{
    public GameServerSlave(NodeConfiguration serverConfiguration) : base(serverConfiguration)
    {
    }

    public override ValueTask ConnectAsync(bool upgrade) => default;
}
