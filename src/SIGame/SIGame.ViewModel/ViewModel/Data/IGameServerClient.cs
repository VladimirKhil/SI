using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SICore;
using SIData;

namespace SIGame.ViewModel.ViewModel.Data
{
    public interface IGameServerClient: IAsyncDisposable
    {
        string ServerAddress { get; }

        event Action<GameInfo> GameCreated;
        event Action<int> GameDeleted;
        event Action<GameInfo> GameChanged;

        event Action<string> Joined;
        event Action<string> Leaved;
        event Action<string, string> Receieve;

        event Func<Exception, Task> Reconnecting;
        event Func<string, Task> Reconnected;

        event Func<Exception, Task> Closed;

        event Action<int> UploadProgress;

        Task OpenAsync(string userName, CancellationToken token);
        Task<SI.GameServer.Contract.HostInfo> GetGamesHostInfoAsync(CancellationToken cancellationToken = default);
        Task<string> GetNews();
        Task<string[]> GetUsers();
        Task<GameInfo[]> GetFilteredGamesAsync(GamesFilter filter, CancellationToken cancellationToken = default);

        Task<SI.GameServer.Contract.Slice<GameInfo>> GetGamesAsync(int fromId, CancellationToken cancellationToken = default);

        Task<bool> HasPackage(PackageKey packageKey);
        Task UploadPackage(FileKey packageHash, Stream stream, CancellationToken cancellationToken);

        Task<SI.GameServer.Contract.GameCreationResult> CreateGame(GameSettingsCore<AppSettingsCore> gameSettings, PackageKey packageKey, ComputerAccountInfo[] computerAccounts, FileKey background);

        Task<string> HasPicture(FileKey pictureKey);
        Task<string> UploadPicture(FileKey pictureHash, Stream data, CancellationToken cancellationToken);

        Task Say(string message);
    }
}
