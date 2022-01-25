using SICore.Connections;
using SICore.Network.Configuration;
using SICore.Network.Contracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SICore.Network.Servers
{
    public abstract class SlaveServer : Server, ISlaveServer
    {
        public IConnection HostServer { get; set; }

        public override bool IsMain => false;

        public override IEnumerable<IConnection> Connections
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

        public override ValueTask<bool> AddConnectionAsync(IConnection connection, CancellationToken cancellationToken = default) =>
            ConnectionsLock.WithLockAsync(
                async () =>
                {
                    if (HostServer != null && HostServer != connection)
                    {
                        await RemoveConnectionAsync(HostServer, false, cancellationToken);
                    }

                    HostServer = connection;

                    connection.Reconnecting += OnReconnecting;
                    connection.Reconnected += OnReconnected;

                    return await base.AddConnectionAsync(connection, cancellationToken);
                },
                cancellationToken);

        public override async ValueTask RemoveConnectionAsync(IConnection connection, bool withError, CancellationToken cancellationToken = default)
        {
            await ConnectionsLock.WithLockAsync(
                () =>
                {
                    if (HostServer == connection)
                    {
                        connection.Reconnecting -= OnReconnecting;
                        connection.Reconnected -= OnReconnected;

                        HostServer = null;
                    }
                },
                cancellationToken);

            await base.RemoveConnectionAsync(connection, withError, cancellationToken);
        }

        public abstract ValueTask ConnectAsync(bool upgrade);

        protected override async ValueTask DisposeAsync(bool disposing)
        {
            await ConnectionsLock.TryLockAsync(
                async () =>
                {
                    if (HostServer != null)
                    {
                        await HostServer.DisposeAsync();
                        HostServer = null;
                    }
                },
                5000,
                true);

            await base.DisposeAsync(disposing);
        }
    }
}
