using SICore.Connections;
using SICore.Connections.Errors;
using SICore.Network.Contracts;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using R = SICore.Network.Properties.Resources;

namespace SICore.Network.Servers
{
    public sealed class TcpSlaveServer: SlaveServer
    {
        private readonly string _serverAddress;
        private readonly int _port = -1;

        private string _connectionId;

        /// <summary>
        /// Создание зависимого сервера, подключающегося к главному
        /// </summary>
        /// <param name="port">Имя порта для подключения</param>
        /// <param name="serverAddress">Адрес сервера</param>
        public TcpSlaveServer(int port, string serverAddress, INetworkLocalizer localizer)
            : base(localizer)
        {
            _serverAddress = serverAddress;
            _port = port;
        }

        public async override Task Connect(bool upgrade)
        {
            var tcp = new TcpClient
            {
                SendTimeout = 5000
            };

            var task = tcp.ConnectAsync(_serverAddress, _port);

            var result = await Task.WhenAny(task, Task.Delay(15000));
            if (result != task)
            {
                throw new Exception($"{_localizer[nameof(R.CannotConnectToServer)]} {_serverAddress}:{_port}!");
            }

            var connection = new Connection(tcp, null, upgrade) { IsAuthenticated = true };
            if (upgrade)
            {
                try
                {
                    _connectionId = await connection.UpgradeAsync(_serverAddress, _connectionId);
                }
                catch (ConnectionException exc)
                {
                    throw new Exception($"{_localizer[nameof(R.CannotConnectToServer)]} {_serverAddress}:{_port}!!!!!", exc);
                }
            }

            AddConnection(connection);
            connection.StartRead(false);
        }
    }
}
