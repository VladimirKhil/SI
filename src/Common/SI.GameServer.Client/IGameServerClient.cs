using SI.GameServer.Contract;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SI.GameServer.Client;

/// <summary>
/// Provides a client to SIGame server.
/// </summary>
public interface IGameServerClient : IAsyncDisposable
{
    string ServiceUri { get; }

    IInfoApi Info { get; }

    IGamesApi Games { get; }

    /// <summary>
    /// Reconnecting event.
    /// </summary>
    event Func<Exception?, Task> Reconnecting;

    /// <summary>
    /// Reconnected event.
    /// </summary>
    event Func<string?, Task> Reconnected;

    /// <summary>
    /// Connection closed event.
    /// </summary>
    event Func<Exception?, Task> Closed;

    event Action<GameInfo> GameCreated;
    event Action<int> GameDeleted;
    event Action<GameInfo> GameChanged;

    [Obsolete]
    event Action<string> Joined;
    [Obsolete]
    event Action<string> Leaved;
    [Obsolete]
    event Action<string, string> Receieve;

    Task OpenAsync(string userName, CancellationToken token = default);

    /// <summary>
    /// Gets game server configuration info.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Obsolete("Use Info.GetHostInfoAsync")]
    Task<HostInfo> GetGamesHostInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Joins game lobby.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task JoinLobbyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Leaves game lobby.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LeaveLobbyAsync(CancellationToken cancellationToken = default);

    Task<Slice<GameInfo>> GetGamesAsync(int fromId, CancellationToken cancellationToken = default);

    Task<RunGameResponse> RunGameAsync(RunGameRequest runGameRequest, CancellationToken cancellationToken = default);
}
