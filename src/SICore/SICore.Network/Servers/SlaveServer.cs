using SICore.Connections;
using SICore.Network.Configuration;
using SICore.Network.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SICore.Network.Servers
{
    public abstract class SlaveServer : Server, ISlaveServer
    {
        public IConnection HostServer { get; set; }

        public override bool IsMain
        {
            get { return false; }
        }

        protected override IEnumerable<IConnection> Connections
        {
            get
            {
                if (HostServer == null)
                    yield break;

                yield return HostServer;
            }
        }

        protected SlaveServer(ServerConfiguration serverConfiguration, INetworkLocalizer localizer)
            : base(serverConfiguration, localizer)
        {

        }

        public override bool AddConnection(IConnection externalServer)
        {
            lock (_connectionsSync)
            {
                if (HostServer != null && HostServer != externalServer)
                {
                    RemoveConnection(HostServer, false);
                }

                HostServer = externalServer;

                return base.AddConnection(externalServer);
            }
        }

        public override void RemoveConnection(IConnection connection, bool withError)
        {
            lock (_connectionsSync)
            {
                if (HostServer == connection)
                {
                    HostServer = null;
                }
            }

            base.RemoveConnection(connection, withError);
        }

        public abstract Task Connect(bool upgrade);

        protected override void Dispose(bool disposing)
        {
            lock (_connectionsSync)
            {
                if (HostServer != null)
                {
                    HostServer.Dispose();
                    HostServer = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}
