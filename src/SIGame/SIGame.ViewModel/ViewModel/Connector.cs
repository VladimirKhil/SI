using SICore;
using SICore.Connections;
using SICore.Network;
using SICore.Network.Clients;
using SICore.Network.Servers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SIGame.ViewModel
{
    public sealed class Connector: IDisposable
    {
        private readonly SlaveServer _server;
        private readonly Client _client;

        private TaskCompletionSource<string[]> _tcs;
        private TaskCompletionSource<bool> _tcs2;

        public Connector(SlaveServer server, Client client)
        {
            this._server = server;
            this._client = client;

            client.MessageReceived += ProcessMessage;
        }

        public Task<string[]> GetGameInfo()
        {
            _tcs = new TaskCompletionSource<string[]>();

            lock (_server.ConnectionsSync)
            {
                _server.HostServer.SendMessage(new Message(Messages.GameInfo, "", ""));
            }

            return _tcs.Task;
        }

        public Task<string[]> JoinGame(string command)
        {
            _tcs = new TaskCompletionSource<string[]>();

            var m = new Message(command, "", "");
            lock (_server.ConnectionsSync)
            {
                _server.HostServer.SendMessage(m);
            }

            return _tcs.Task;
        }

        private void ProcessMessage(Message m)
        {
            var text = m.Text?.Split(Message.ArgsSeparatorChar);
            if (text?.Length == 0)
                return;

            if (_tcs2 != null)
            {
                switch (text[0])
                {
                    case Messages.Game:
                        _tcs2.TrySetResult(true);
                        break;

                    case Messages.NoGame:
                        _tcs2.TrySetResult(false);
                        break;
                }
            }

            if (_tcs != null)
            {
                switch (text[0])
                {
                    case Messages.GameInfo:
                        _tcs.TrySetResult(text);
                        break;

                    case Messages.Accepted:
                        _tcs.TrySetResult(text);
                        break;

                    case SystemMessages.Refuse:
                        if (text.Length > 1)
                            _tcs.TrySetException(new Exception(text[1]));
                        break;
                }
            }
        }

        public void Dispose()
        {
            _client.MessageReceived -= ProcessMessage;
        }

        internal Task<bool> SetGameID(int gameID)
        {
            _tcs2 = new TaskCompletionSource<bool>();

            var ct = new CancellationTokenSource(10000);
            ct.Token.Register(() => _tcs2.TrySetCanceled(), useSynchronizationContext: false);

            lock (_server.ConnectionsSync)
            {
                if (_server.HostServer != null)
                {
                    _server.HostServer.SendMessage(new Message(Messages.Game + Message.ArgsSeparatorChar + gameID, "", ""));
                }
            }

            return _tcs2.Task;
        }
    }
}
