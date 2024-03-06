
namespace SICore;

public interface IConnector
{
    string ServerAddress { get; }

    string Error { get; }

    bool CanRetry { get; }

    bool IsReconnecting { get; }

    int GameId { get; }

    Uri? HostUri { get; }

    Task<bool> ReconnectToServer();

    Task RejoinGame();

    void SetHost(IViewerClient newHost);

    void SetGameID(int gameID);

    void SetHostUri(Uri? hostUri);
}
