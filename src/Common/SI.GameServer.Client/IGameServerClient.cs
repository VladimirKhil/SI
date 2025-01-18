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

    Task OpenAsync(string userName, CancellationToken token = default);

    Task<Slice<GameInfo>> GetGamesAsync(int fromId, CancellationToken cancellationToken = default);

    Task<RunGameResponse> RunGameAsync(RunGameRequest runGameRequest, CancellationToken cancellationToken = default);
}
