using SICore.Network.Configuration;
using SICore.Network.Contracts;
using SICore.Network.Servers;
using System.Threading.Tasks;

namespace SIGame.ViewModel.Implementation
{
    public sealed class GameServerSlave : SlaveServer
    {
        public GameServerSlave(
            ServerConfiguration serverConfiguration,
            INetworkLocalizer networkLocalizer)
            : base(serverConfiguration, networkLocalizer)
        {
        }

        public override ValueTask ConnectAsync(bool upgrade) => default;
    }
}
