using SICore.Connections;
using SICore.Connections.Errors;
using SICore.Network.Configuration;
using SICore.Network.Contracts;
using System.Net.Sockets;
using R = SICore.Network.Properties.Resources;

namespace SICore.Network.Servers;

public sealed class TcpSlaveServer : SecondaryNode
{
    private readonly string _serverAddress;
    private readonly int _port = -1;

    private string _connectionId;

    /// <summary>
    /// Создание зависимого сервера, подключающегося к главному
    /// </summary>
    /// <param name="port">Имя порта для подключения</param>
    /// <param name="serverAddress">Адрес сервера</param>
    public TcpSlaveServer(int port, string serverAddress, NodeConfiguration serverConfiguration, INetworkLocalizer localizer)
        : base(serverConfiguration, localizer)
    {
        _serverAddress = serverAddress;
        _port = port;
    }

    public async override ValueTask ConnectAsync(bool upgrade)
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

        if (result.IsFaulted)
        {
            throw result.Exception!;
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

        await AddConnectionAsync(connection);

        connection.StartRead(false);
    }
}
