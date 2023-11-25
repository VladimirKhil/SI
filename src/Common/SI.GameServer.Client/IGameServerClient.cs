using SI.GameServer.Contract;
using SIData;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SI.GameServer.Client;

/// <summary>
/// Provides a client to SIGame server.
/// </summary>
public interface IGameServerClient : IGameClient
{
    string ServiceUri { get; }

    event Action<GameInfo> GameCreated;
    event Action<int> GameDeleted;
    event Action<GameInfo> GameChanged;

    event Action<string> Joined;
    event Action<string> Leaved;
    event Action<string, string> Receieve;

    event Action<int> UploadProgress;

    Task OpenAsync(string userName, CancellationToken token = default);

    /// <summary>
    /// Gets game server configuration info.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
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

    Task<string> GetNewsAsync(CancellationToken cancellationToken = default);

    Task<string[]> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<Slice<GameInfo>> GetGamesAsync(int fromId, CancellationToken cancellationToken = default);

    Task<bool> HasPackageAsync(PackageKey packageKey, CancellationToken cancellationToken = default);

    Task UploadPackageAsync(FileKey packageHash, Stream stream, CancellationToken cancellationToken = default);

    Task<GameCreationResult> CreateGameAsync(
        GameSettingsCore<AppSettingsCore> gameSettings,
        PackageKey packageKey,
        ComputerAccountInfo[] computerAccounts,
        CancellationToken cancellationToken = default);

    Task<GameCreationResult> CreateGame2Async(
        GameSettingsCore<AppSettingsCore> gameSettings,
        PackageInfo packageInfo,
        ComputerAccountInfo[] computerAccounts,
        CancellationToken cancellationToken = default);

    Task<string> HasImageAsync(FileKey imageKey, CancellationToken cancellationToken = default);

    Task<string> UploadImageAsync(FileKey imageKey, Stream data, CancellationToken cancellationToken = default);

    Task SayAsync(string message);

    Task<GameCreationResult> JoinGameAsync(
        int gameId,
        GameRole role,
        bool isMale,
        string password,
        CancellationToken cancellationToken = default);

    Task SendMessageAsync(Message message, CancellationToken cancellationToken = default);

    Task<GameCreationResult> CreateAndJoinGameAsync(
        GameSettingsCore<AppSettingsCore> gameSettings,
        PackageKey packageKey,
        ComputerAccountInfo[] computerAccounts,
        bool isMale,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new game and joins its.
    /// </summary>
    /// <param name="gameSettings">Game settings.</param>
    /// <param name="packageInfo">Package info.</param>
    /// <param name="computerAccounts">Custom computer accounts info.</param>
    /// <param name="isMale">Male flag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<GameCreationResult> CreateAndJoinGame2Async(
        GameSettingsCore<AppSettingsCore> gameSettings,
        PackageInfo packageInfo,
        ComputerAccountInfo[] computerAccounts,
        bool isMale,
        CancellationToken cancellationToken = default);

    Task LeaveGameAsync(CancellationToken cancellationToken = default);
}
