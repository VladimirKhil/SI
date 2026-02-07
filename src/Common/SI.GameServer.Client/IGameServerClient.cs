using SI.GameServer.Contract;

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

    /// <summary>
    /// Fired when initial games snapshot loading is complete.
    /// </summary>
    event Action? GamesLoaded;

    /// <summary>
    /// Fired when game list should be cleared (before receiving new snapshot on reconnect).
    /// </summary>
    event Action? GamesClear;

    Task OpenAsync(CancellationToken token = default);

    /// <summary>
    /// Opens a Server-Sent Events stream for receiving game updates.
    /// First receives initial games snapshot in chunks, then streams updates indefinitely.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop the stream.</param>
    Task OpenGamesStreamAsync(CancellationToken cancellationToken = default);

    Task<Slice<GameInfo>> GetGamesAsync(int fromId, CancellationToken cancellationToken = default);

    Task<RunGameResponse> RunGameAsync(RunGameRequest runGameRequest, CancellationToken cancellationToken = default);
}
