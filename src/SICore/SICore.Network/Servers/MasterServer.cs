using SICore.Connections;
using SICore.Network.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        /// <summary>
        /// Остальные серверы
        /// </summary>
        public IEnumerable<IConnection> ExternalServers => _connections;

        protected override IEnumerable<IConnection> Connections => _connections;

        public MasterServer(INetworkLocalizer localizer)
			: base(localizer)
		{

		}

        public override bool AddConnection(IConnection connection)
        {
            if (_isDisposed)
            {
                return false;
            }

            var address = connection.RemoteAddress;

            if (_banned.TryGetValue(address, out DateTime date) && date > DateTime.Now)
            {
                connection.SendMessage(
					new Message(
						$"{SystemMessages.Refuse}\n{_localizer[nameof(R.ConnectionDenied)]}{(date == DateTime.MaxValue ? "" : ($" {_localizer[nameof(R.Until)]} " + date.ToString(_localizer.Culture)))}\r\n",
						Constants.GameName
					)
				);

				Task.Run(async () =>
                {
					try
					{
						await Task.Delay(4000);
						connection.Dispose();
					}
					catch (Exception exc)
					{
						OnError(exc, true);
					}
                });

                return false;
            }

            base.AddConnection(connection);

            lock (_connectionsSync)
            {
                _connections.Add(connection);
            }

            return true;
        }

        public override void RemoveConnection(IConnection connection, bool withError)
        {
            lock (_connectionsSync)
            {
                if (_connections.Contains(connection))
                {
                    _connections.Remove(connection);
                }
            }

            base.RemoveConnection(connection, withError);
        }

        public void Kick(string name, bool ban = false)
        {
			IConnection connectionToClose = null;
            lock (_connectionsSync)
            {
                foreach (var connection in _connections)
                {
                    if (connection.UserName == name)
                    {
                        var address = connection.RemoteAddress;
                        if (address.Length > 0)
                        {
                            _banned[address] = ban ? DateTime.MaxValue : DateTime.Now.AddMinutes(5.0);
                        }

                        connectionToClose = connection;
                        break;
                    }
                }
            }

			if (connectionToClose != null)
			{
				Connection_ConnectionClosed(connectionToClose, false);
			}
		}

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            var getLock = Monitor.TryEnter(_connectionsSync, 5000);
            if (!getLock)
            {
                Trace.TraceError($"Cannot get {nameof(_connectionsSync)} in Dispose()!");
            }

            try
            {
                foreach (var connection in _connections)
                {
                    ClearListeners(connection);
                    connection.Dispose();
                }

                _connections.Clear();
            }
            finally
            {
                if (getLock)
                {
                    Monitor.Exit(_connectionsSync);
                }
            }

            _isDisposed = true;

            base.Dispose(disposing);
        }
    }
}
