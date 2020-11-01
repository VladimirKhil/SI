using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using SI.GameServer.Contract;
using SICore;
using SIData;
using SIGame.ViewModel.PlatformSpecific;
using SIGame.ViewModel.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIGame.ViewModel.ViewModel.Data
{
    /// <summary>
    /// Клиент для SignalR.Core
    /// </summary>
    public sealed class GameServerClientNew : IGameServerClient
    {
        private const double PackageUploadTimelimitInMinutes = 4.0;

        public static string GameServerPredefinedUri = null;

        private const int _BufferSize = 80 * 1024;

        private string _login;

        private CookieContainer _cookieContainer;
        private HttpClientHandler _httpClientHandler;
        private HttpClient _client;
        private ProgressMessageHandler _progressMessageHandler;

        private HubConnection _connection;

        public string ServerAddress { get; }

        public event Action<SICore.GameInfo> GameCreated;
        public event Action<int> GameDeleted;
        public event Action<SICore.GameInfo> GameChanged;
        public event Action<string> Joined;
        public event Action<string> Leaved;
        public event Action<string, string> Receieve;
        public event Action<int> UploadProgress;

        public event Func<Exception, Task> Closed;
        public event Func<Exception, Task> Reconnecting;
        public event Func<string, Task> Reconnected;

        public GameServerClientNew(string serverAddress)
        {
            if (!string.IsNullOrEmpty(GameServerPredefinedUri))
                ServerAddress = GameServerPredefinedUri;
            else if (serverAddress != null)
                ServerAddress = serverAddress;
        }

        public Task<GameCreationResult> CreateGame(GameSettingsCore<AppSettingsCore> gameSettings, PackageKey packageKey, ComputerAccountInfo[] computerAccounts, FileKey background)
        {
            gameSettings.AppSettings.Culture = Thread.CurrentThread.CurrentUICulture.Name;

            return _connection.InvokeAsync<GameCreationResult>("CreateGame", gameSettings, packageKey, computerAccounts.Select(ca => ca.Account).ToArray());
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
            {
                _connection.Closed -= OnConnectionClosed;

                await _connection.DisposeAsync();
                _connection = null;
            }

            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }

        public async Task<SICore.GameInfo[]> GetFilteredGamesAsync(GamesFilter filter, CancellationToken cancellationToken = default)
        {
            var games = await _connection.InvokeAsync<SI.GameServer.Contract.GameInfo[]>("GetFilteredGames", filter, cancellationToken);

            return games.Select(ToSICoreGame).ToArray();
        }

        public async Task<SICore.GameInfo[]> GetFilteredGamesNewAsync(GamesFilter filter, CancellationToken cancellationToken = default)
        {
            var games = await _connection.InvokeAsync<SimpleGameInfo[]>("GetFilteredGamesNew", filter, cancellationToken);

            return games.Select(ToSICoreGame).ToArray();
        }

        public async Task<Slice<SICore.GameInfo>> GetGamesAsync(int fromId, CancellationToken cancellationToken = default)
        {
            var slice = await _connection.InvokeAsync<Slice<SI.GameServer.Contract.GameInfo>>("GetGamesSlice", fromId, cancellationToken);

            return new Slice<SICore.GameInfo> { Data = slice.Data.Select(ToSICoreGame).ToArray(), IsLastSlice = slice.IsLastSlice };
        }

        private SICore.GameInfo ToSICoreGame(SI.GameServer.Contract.GameInfo gameInfo) => new SICore.GameInfo
        {
            GameID = gameInfo.GameID,
            GameName = gameInfo.GameName,
            Mode = gameInfo.Mode,
            Owner = gameInfo.Owner,
            PackageName = gameInfo.PackageName,
            PasswordRequired = gameInfo.PasswordRequired,
            Persons = gameInfo.Persons,
            RealStartTime = gameInfo.RealStartTime == DateTime.MinValue ? DateTime.MinValue : gameInfo.RealStartTime.ToLocalTime(),
            Rules = BuildRules(gameInfo),
            Stage = BuildStage(gameInfo),
            Started = gameInfo.Started,
            StartTime = gameInfo.StartTime.ToLocalTime()
        };

        private SICore.GameInfo ToSICoreGame(SimpleGameInfo gameInfo) => new SICore.GameInfo
        {
            GameID = gameInfo.GameID,
            GameName = gameInfo.GameName,
            PasswordRequired = gameInfo.PasswordRequired
        };

        private static string BuildStage(SI.GameServer.Contract.GameInfo gameInfo)
        {
            switch (gameInfo.Stage)
            {
                case GameStages.Created:
                    return Resources.GameStage_Created;
                case GameStages.Started:
                    return Resources.GameStage_Started;
                case GameStages.Round:
                    return Resources.GameStage_Round + ": " + gameInfo.StageName;
                case GameStages.Final:
                    return Resources.GameStage_Final;
                case GameStages.Finished:
                default:
                    return Resources.GameStage_Finished;
            }
        }

        private static string BuildRules(SI.GameServer.Contract.GameInfo gameInfo)
        {
            var rules = gameInfo.Rules;
            var sb = new StringBuilder();

            if (gameInfo.Mode == SIEngine.GameModes.Sport)
            {
                if (sb.Length > 0)
                    sb.Append(", ");

                sb.Append(Resources.GameRule_Sport);
            }

            if ((rules & GameRules.FalseStart) == 0)
            {
                if (sb.Length > 0)
                    sb.Append(", ");

                sb.Append(Resources.GameRule_NoFalseStart);
            }

            if ((rules & GameRules.Oral) > 0)
            {
                if (sb.Length > 0)
                    sb.Append(", ");

                sb.Append(Resources.GameRule_Oral);
            }

            if ((rules & GameRules.IgnoreWrong) > 0)
            {
                if (sb.Length > 0)
                    sb.Append(", ");

                sb.Append(Resources.GameRule_IgnoreWrong);
            }

            return sb.ToString();
        }

        public Task<HostInfo> GetGamesHostInfoAsync(CancellationToken cancellationToken = default) =>
            _connection.InvokeAsync<HostInfo>("GetGamesHostInfo", cancellationToken);

        public Task<string> GetNews() => _connection.InvokeAsync<string>("GetNews");

        public async Task<string[]> GetUsers() => await _connection.InvokeAsync<string[]>("GetUsers");

        public Task<bool> HasPackage(PackageKey packageKey) => _connection.InvokeAsync<bool>("HasPackage", packageKey);

        public Task<string> HasPicture(FileKey pictureKey) => _connection.InvokeAsync<string>("HasPicture", pictureKey);

        private async Task<string> AuthenticateUserAsync(string user, string password, CancellationToken cancellationToken)
        {
            var uri = ServerAddress + "/api/Account/LogOn";
            using (var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["login"] = user,
                ["password"] = password
            }))
            {
                var response = await _client.PostAsync(uri, content, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }

                switch (response.StatusCode)
                {
                    case HttpStatusCode.Conflict:
                        throw new Exception(Resources.OnlineUserConflict);

                    case HttpStatusCode.Forbidden:
                        throw new Exception(Resources.LoginForbidden);

                    default:
                        throw new Exception(await response.Content.ReadAsStringAsync());
                }
            }
        }

        public async Task OpenAsync(string userName, CancellationToken cancellationToken)
        {
            _cookieContainer = new CookieContainer();
            _httpClientHandler = new HttpClientHandler { CookieContainer = _cookieContainer };

            _progressMessageHandler = new ProgressMessageHandler(_httpClientHandler);
            _progressMessageHandler.HttpSendProgress += MessageHandler_HttpSendProgress;

            _client = new HttpClient(_progressMessageHandler) { Timeout = TimeSpan.FromMinutes(PackageUploadTimelimitInMinutes) };
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36");

            var token = await AuthenticateUserAsync(userName, "", cancellationToken);

            _login = userName;
            
            _connection = new HubConnectionBuilder()
                .WithUrl($"{ServerAddress}/sionline?token={token}", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(Convert.ToBase64String(Encoding.UTF8.GetBytes(_login)));
                })
                .WithAutomaticReconnect()
                .AddMessagePackProtocol()
                .Build();

            _connection.Reconnecting += async e =>
            {
                if (Reconnecting != null)
                {
                    await Reconnecting(e);
                }
            };

            _connection.Reconnected += async s =>
            {
                if (Reconnected != null)
                {
                    await Reconnected(s);
                }
            };

            _connection.Closed += OnConnectionClosed;

            _connection.HandshakeTimeout = TimeSpan.FromMinutes(1);
            
            _connection.On<string, string>("Say", (user, text) => OnUI(() => Receieve?.Invoke(user, text)));
            _connection.On<SI.GameServer.Contract.GameInfo>("GameCreated", (gameInfo) => OnUI(() => GameCreated?.Invoke(ToSICoreGame(gameInfo))));
            _connection.On<int>("GameDeleted", (gameId) => OnUI(() => GameDeleted?.Invoke(gameId)));
            _connection.On<SI.GameServer.Contract.GameInfo>("GameChanged", (gameInfo) => OnUI(() => GameChanged?.Invoke(ToSICoreGame(gameInfo))));
            _connection.On<string>("Joined", (user) => OnUI(() => Joined?.Invoke(user)));
            _connection.On<string>("Leaved", (user) => OnUI(() => Leaved?.Invoke(user)));
            
            await _connection.StartAsync(cancellationToken);
        }

        private async Task OnConnectionClosed(Exception exc)
        {
            if (Closed != null)
            {
                await Closed(exc);
            }
        }

        private void OnUI(Action action) => PlatformManager.Instance.ExecuteOnUIThread(action)();

        private void MessageHandler_HttpSendProgress(object sender, HttpProgressEventArgs e) => UploadProgress?.Invoke(e.ProgressPercentage);

        public async Task Say(string message) => await _connection.InvokeAsync("Say", message);

        public async Task UploadPackage(FileKey packageKey, Stream stream, CancellationToken cancellationToken)
        {
            var url = $"{ServerAddress}/api/upload/package";
            var content = new StreamContent(stream, _BufferSize);

            try
            {
                using (var formData = new MultipartFormDataContent())
                {
                    formData.Add(content, "file", packageKey.Name);
                    formData.Headers.ContentMD5 = packageKey.Hash;
                    using (var response = await _client.PostAsync(url, formData, cancellationToken))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            var errorMessage = await response.Content.ReadAsStringAsync();

                            if (response.StatusCode == HttpStatusCode.RequestEntityTooLarge)
                            {
                                errorMessage = Resources.FileTooLarge;
                            }

                            throw new Exception(errorMessage);
                        }
                    }
                }
            }
            catch (TaskCanceledException exc)
            {
                if (!exc.CancellationToken.IsCancellationRequested)
                    throw new Exception(Resources.UploadPackageTimeout, exc);

                throw exc;
            }
        }

        public async Task<string> UploadPicture(FileKey pictureHash, Stream data, CancellationToken cancellationToken)
        {
            var url = ServerAddress + "/api/upload/image";
            var bytesContent = new StreamContent(data, _BufferSize);

            using (var formData = new MultipartFormDataContent())
            {
                formData.Add(bytesContent, Convert.ToBase64String(pictureHash.Hash)/*"file"*/, pictureHash.Name);

                var response = await _client.PostAsync(url, formData, cancellationToken);
                return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null;
            }
        }
    }
}
