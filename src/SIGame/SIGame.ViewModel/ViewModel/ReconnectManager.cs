using SICore;
using SICore.Network;
using SICore.Network.Clients;
using SICore.Network.Servers;
using SIData;
using SIGame.ViewModel.Properties;

namespace SIGame.ViewModel;

internal sealed class ReconnectManager : IConnector
{
    public string ServerAddress { get; set; }

    public string Error { get; set; }

    public bool CanRetry { get; set; }

    public bool IsReconnecting { get; set; }

    private readonly SecondaryNode _server;
    private readonly Client _client;
    private IViewerClient _host;
    private Connector _connector;

    private readonly HumanAccount _human;
    private readonly GameRole _role;

    private readonly string _credentials;
    private readonly bool _upgrade;

    public int GameId { get; set; } = -1;

    public ReconnectManager(
        SecondaryNode server,
        Client client,
        IViewerClient host,
        HumanAccount human,
        GameRole role,
        string credentials,
        bool upgrade)
    {
        _server = server;
        _client = client;
        _host = host;
        _human = human;
        _role = role;
        _credentials = credentials;
        _upgrade = upgrade;
    }

    public void SetGameID(int gameId) => GameId = gameId;

    public async Task<bool> ReconnectToServer()
    {
        Error = "";
        CanRetry = true;
        IsReconnecting = true;
        try
        {
            await _server.ConnectAsync(_upgrade);
            _connector = new Connector(_server, _client);

            return true;
        }
        catch (Exception exc)
        {
            try
            {
                Error = exc.Message;

                if (_connector != null)
                {
                    _connector.Dispose();
                    _connector = null;
                }
            }
            catch { }

            return false;
        }
    }

    private async ValueTask JoinGameCompletedAsync()
    {
        await _server.ConnectionsLock.WithLockAsync(() =>
        {
            var externalServer = _server.HostServer;

            if (externalServer != null)
            {
                lock (externalServer.ClientsSync)
                {
                    externalServer.Clients.Add(NetworkConstants.GameName);
                }
            }
            else
            {
                Error = Resources.RejoinError;
                return;
            }
        });

        _connector.Dispose();

        _host.GetInfo();

        Error = null;
    }

    public async Task RejoinGame()
    {
        await JoinGameAsync();
    }

    private async Task JoinGameAsync()
    {
        try
        {
            if (GameId > -1)
            {
                var result = await _connector.SetGameIdAsync(GameId);

                if (!result)
                {
                    Error = Resources.GameClosedCauseEverybodyLeft;
                    CanRetry = false;
                    return;
                }
            }

            var name = _human.Name;
            var sex = _human.IsMale ? 'm' : 'f';
            var command = $"{Messages.Connect}\n{_role.ToString().ToLowerInvariant()}\n{name}\n{sex}\n{0}{_credentials}";

            var message = await _connector.JoinGameAsync(command);
            await JoinGameCompletedAsync();
            IsReconnecting = false;
        }
        catch (TaskCanceledException)
        {
            Error = Resources.CannotJoinGame;
        }
        catch (Exception exc)
        {
            Error = exc.Message;
        }
    }

    public void SetHost(IViewerClient newHost) => _host = newHost;
}
