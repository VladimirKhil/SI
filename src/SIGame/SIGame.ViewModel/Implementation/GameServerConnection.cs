using SI.GameServer.Client;
using SICore.Connections;
using System;
using System.Threading.Tasks;

namespace SIGame.ViewModel.Implementation
{
    internal sealed class GameServerConnection : ConnectionBase
    {
        private readonly IGameServerClient _gameServerClient;

        public GameServerConnection(
            IGameServerClient gameServerClient)
        {
            _gameServerClient = gameServerClient;
            _gameServerClient.IncomingMessage += OnMessageReceived;
            _gameServerClient.Reconnecting += GameServerClient_Reconnecting;
            _gameServerClient.Reconnected += GameServerClient_Reconnected;
        }

        private Task GameServerClient_Reconnecting(Exception arg)
        {
            OnReconnecting();
            return Task.CompletedTask;
        }

        private Task GameServerClient_Reconnected(string arg)
        {
            OnReconnected();
            return Task.CompletedTask;
        }

        public override string RemoteAddress => throw new NotImplementedException();

        public override ValueTask SendMessageAsync(Message m) => new ValueTask(_gameServerClient.SendMessageAsync(m));

        protected override ValueTask DisposeAsync(bool disposing)
        {
            _gameServerClient.IncomingMessage -= OnMessageReceived;
            _gameServerClient.Reconnecting -= GameServerClient_Reconnecting;
            _gameServerClient.Reconnected -= GameServerClient_Reconnected;

            return _gameServerClient.DisposeAsync();
        }
    }
}
