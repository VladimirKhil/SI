using SICore.Connections;
using SICore.Network.Configuration;
using SICore.Network.Contracts;
using System.Net;
using System.Net.Sockets;
using R = SICore.Network.Properties.Resources;

namespace SICore.Network.Servers;

/// <summary>
/// Сервер, обеспечивающий взаимодействие с клиентами по TCP
/// </summary>
public sealed class TcpMasterServer : PrimaryNode
{
    /// <summary>
    /// Порт для прослушивания
    /// </summary>
    private readonly int _port = -1;

    private bool _disposing = false;

    /// <summary>
    /// Слушатель внешней сети
    /// </summary>
    private TcpListener _listener;

    /// <summary>
    /// Поток прослушивания
    /// </summary>
    private readonly object _listenerSync = new();

    private readonly CancellationTokenSource _cancellation = new();

    /// <summary>
    /// Создание основного сервера
    /// </summary>
    /// <param name="port">Порт для прослушивания</param>
    public TcpMasterServer(int port, NodeConfiguration serverConfiguration, INetworkLocalizer localizer)
        : base(serverConfiguration, localizer)
    {
        _port = port;
    }
    
    public void StartListen()
    {
        try
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
        }
        catch (Exception e)
        {
            throw new Exception(string.Format(_localizer[nameof(R.NetworkOpeningError)], e.Message));
        }

        Listening();
    }

    /// <summary>
    /// Прослушивание порта
    /// </summary>
    private async void Listening()
    {
        while (true)
        {
            lock (_listenerSync)
            {
                if (_cancellation.IsCancellationRequested)
                {
                    break;
                }
            }

            try
            {
                var tcpClient = await _listener.AcceptTcpClientAsync(_cancellation.Token);
                await AddConnectionAsync(tcpClient, _cancellation.Token);

                lock (_listenerSync)
                {
                    if (_cancellation.IsCancellationRequested)
                    {
                        _listener.Stop();
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (InvalidOperationException) { }
            catch (SocketException) { }
            catch (Exception e)
            {
                OnError(e, false);
            }
        }
    }

    protected override ValueTask DisposeAsync(bool disposing)
    {
        lock (_listenerSync)
        {
            if (!_disposing)
            {
                _disposing = true;

                _cancellation.Cancel();
                if (_listener != null)
                    _listener.Stop();

                _cancellation.Dispose();
            }
        }

        return base.DisposeAsync(disposing);
    }

    public async Task AddConnectionAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        var connection = new Connection(tcpClient, null);
        await AddConnectionAsync(connection, cancellationToken);
        connection.StartRead(false);
    }
}
