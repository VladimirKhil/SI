using SICore.Connections;
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

            if (upgrade)
            {
                _connectionId = await Upgrade(tcp, _connectionId);
            }

            var extServer = new Connection(tcp, null, upgrade) { IsAuthenticated = true };
            AddConnection(extServer);

            try
            {
                extServer.StartRead(false);
            }
            catch (Exception exc)
            {
                RemoveConnection(extServer, true);
                throw exc;
            }
        }

        private async Task<string> Upgrade(TcpClient tcp, string connectionId = null)
        {
            var connectionIdHeader = connectionId != null ? $"\nConnectionId: {connectionId}" : "";

            var upgradeText = $"GET / HTTP/1.1\nHost: {_serverAddress}\nConnection: Upgrade{connectionIdHeader}\nUpgrade: sigame\n\n";
            var bytes = Encoding.UTF8.GetBytes(upgradeText);
            await tcp.GetStream().WriteAsync(bytes, 0, bytes.Length);

            var buffer = new byte[5000];

            var upgradeMessage = new StringBuilder();
            do
            {
                var bytesRead = await tcp.GetStream().ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead < 1)
                {
                    // Нормальное закрытие соединения
                    tcp.Close();
                    throw new Exception($"{_localizer[nameof(R.CannotConnectToServer)]} {_serverAddress}:{_port}!!!!!");
                }

                upgradeMessage.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
            } while (!upgradeMessage.ToString().EndsWith("\n\n") && !upgradeMessage.ToString().EndsWith("\r\n\r\n"));

            var headers = upgradeMessage
                .ToString()
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Skip(1)
                .Select(headerString =>
                {
                    var split = headerString.Split(new[] { ": " }, StringSplitOptions.None);
                    return new { Name = split[0], Value = split[1] };
                })
                .ToDictionary(val => val.Name, val => val.Value);

            if (!headers.TryGetValue("ConnectionId", out string connectionIdFromServer))
            {
                connectionIdFromServer = connectionId;
            }

            return connectionIdFromServer;
        }
    }
}
