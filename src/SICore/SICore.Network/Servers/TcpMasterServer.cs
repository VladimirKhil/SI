using SICore.Connections;
using SICore.Network.Configuration;
using SICore.Network.Contracts;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using R = SICore.Network.Properties.Resources;

namespace SICore.Network.Servers
{
    /// <summary>
    /// Сервер, обеспечивающий взаимодействие с клиентами по TCP
    /// </summary>
    public sealed class TcpMasterServer: MasterServer
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
        private readonly object _listenerSync = new object();

        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

        /// <summary>
        /// Создание основного сервера
        /// </summary>
        /// <param name="port">Порт для прослушивания</param>
        public TcpMasterServer(int port, ServerConfiguration serverConfiguration, INetworkLocalizer localizer)
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
                        break;
                }

                try
                {
                    var tcpClient = await _listener.AcceptTcpClientAsync();
                    AddConnection(tcpClient);

                    lock (_listenerSync)
                    {
                        if (_cancellation.IsCancellationRequested)
                        {
                            _listener.Stop();
                        }
                    }
                }
                catch (InvalidOperationException) { }
                catch (SocketException) { }
                catch (Exception e)
                {
                    OnError(e, false);
                }
            }
        }

        protected override void Dispose(bool disposing)
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

            base.Dispose(disposing);
        }

        public void AddConnection(TcpClient tcpClient)
        {
            var connection = new Connection(tcpClient, null);
            AddConnection(connection);
            connection.StartRead(false);
        }
    }
}
