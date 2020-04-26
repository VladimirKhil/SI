
using SICore.Network.Contracts;

namespace SICore.Network.Servers
{
    /// <summary>
    /// Обычный (не сетевой) сервер
    /// </summary>
    public sealed class BasicServer : MasterServer
    {
        public BasicServer(INetworkLocalizer localizer)
            : base(localizer)
        {

        }
    }
}
