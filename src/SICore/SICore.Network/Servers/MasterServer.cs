using SICore.Connections;
using SICore.Network.Configuration;
using SICore.Network.Contracts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using R = SICore.Network.Properties.Resources;

namespace SICore.Network.Servers
{
    /// <summary>
    /// Основной сервер
    /// </summary>
    public class MasterServer : Server, IMasterServer
    {
        private bool _isDisposed;

        /// <summary>
        /// Остальные серверы
        /// </summary>
        protected List<IConnection> _connections = new List<IConnection>();
        
        private readonly Dictionary<string, DateTime> _banned = new Dictionary<string, DateTime>();

        public override bool IsMain => true;

        public override IEnumerable<IConnection> Connections => _connections;

        public MasterServer(ServerConfiguration serverConfiguration, INetworkLocalizer localizer)
            : base(serverConfiguration, localizer)
        {

        }

        public override async ValueTask<bool> AddConnectionAsync(IConnection connection, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
            {
                return false;
            }

            var address = connection.RemoteAddress;

            if (_banned.TryGetValue(address, out DateTime date) && date > DateTime.UtcNow)
            {
                await connection.SendMessageAsync(
                    new Message(
                        $"{SystemMessages.Refuse}\n{_localizer[nameof(R.ConnectionDenied)]}{(date == DateTime.MaxValue ? "" : ($" {_localizer[nameof(R.Until)]} " + date.ToString(_localizer.Culture)))}\r\n",
                        NetworkConstants.GameName)
                );

                DropConnection(connection);
                return false;
            }

            await base.AddConnectionAsync(connection, cancellationToken);

            await ConnectionsLock.WithLockAsync(
                () =>
                {
                    _connections.Add(connection);
                },
                cancellationToken);

            return true;
        }

        /// <summary>
        /// Waits and kills the connection. A wait is needed to prevent the client flooding.
        /// </summary>
        /// <remarks>It must be performed in a separate thread (`async void`) not to slow down the common connections listener.</remarks>
        /// <param name="connection">Connection to kill.</param>
        private async void DropConnection(IConnection connection)
        {
            try
            {
                await Task.Delay(4000);
                await connection.DisposeAsync();
            }
            catch (Exception exc)
            {
                OnError(exc, true);
            }
        }

        public override async ValueTask RemoveConnectionAsync(
            IConnection connection,
            bool withError,
            CancellationToken cancellationToken = default)
        {
            await ConnectionsLock.WithLockAsync(
                () =>
                {
                    if (_connections.Contains(connection))
                    {
                        _connections.Remove(connection);
                    }
                },
                cancellationToken);

            await base.RemoveConnectionAsync(connection, withError, cancellationToken);
        }

        public async ValueTask KickAsync(string name, bool ban = false)
        {
            IConnection connectionToClose = null;
            await ConnectionsLock.WithLockAsync(() =>
            {
                foreach (var connection in _connections)
                {
                    if (connection.UserName == name)
                    {
                        var address = connection.RemoteAddress;
                        if (address.Length > 0)
                        {
                            _banned[address] = ban ? DateTime.MaxValue : DateTime.UtcNow.AddMinutes(5.0);
                        }

                        connectionToClose = connection;
                        break;
                    }
                }
            });

            if (connectionToClose != null)
            {
                Connection_ConnectionClosed(connectionToClose, false);
            }
        }

        protected override async ValueTask DisposeAsync(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            await ConnectionsLock.TryLockAsync(
                async () =>
                {
                    foreach (var connection in _connections)
                    {
                        ClearListeners(connection);
                        await connection.DisposeAsync();
                    }

                    _connections.Clear();
                },
                5000,
                true);

            _isDisposed = true;

            await base.DisposeAsync(disposing);
        }
    }
}
