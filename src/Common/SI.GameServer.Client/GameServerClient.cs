using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using SI.GameServer.Client.Properties;
using SI.GameServer.Contract;
using SICore;
using SICore.Connections;
using SIData;
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

namespace SI.GameServer.Client
{
    /// <summary>
    /// Represents SIGame server client.
    /// </summary>
    public sealed class GameServerClient : IGameServerClient
    {
        private const int _BufferSize = 80 * 1024;

        private bool _isOpened;
        private string _login;

        private readonly GameServerClientOptions _options;

        private CookieContainer _cookieContainer;
        private HttpClientHandler _httpClientHandler;
        private HttpClient _client;
        private ProgressMessageHandler _progressMessageHandler;

        private HubConnection _connection;

        public string ServiceUri => _options.ServiceUri;

        public event Action<SICore.GameInfo> GameCreated;
        public event Action<int> GameDeleted;
        public event Action<SICore.GameInfo> GameChanged;
        public event Action<string> Joined;
        public event Action<string> Leaved;
        public event Action<string, string> Receieve;
        public event Action<int> UploadProgress;
        public event Action<Message> IncomingMessage;

        public event Func<Exception, Task> Closed;
        public event Func<Exception, Task> Reconnecting;
        public event Func<string, Task> Reconnected;

        private readonly IUIThreadExecutor _uIThreadExecutor;

        public GameServerClient(GameServerClientOptions options, IUIThreadExecutor uIThreadExecutor = null)
        {
            _options = options;
            _uIThreadExecutor = uIThreadExecutor;
        }

        public Task<GameCreationResult> CreateGameAsync(
            GameSettingsCore<AppSettingsCore> gameSettings,
            PackageKey packageKey,
            ComputerAccountInfo[] computerAccounts,
            CancellationToken cancellationToken = default)
        {
            gameSettings.AppSettings.Culture = Thread.CurrentThread.CurrentUICulture.Name;

            return _connection.InvokeAsync<GameCreationResult>(
                "CreateGame",
                gameSettings,
                packageKey,
                computerAccounts.Select(ca => ca.Account).ToArray(),
                cancellationToken);
        }

        public Task<GameCreationResult> CreateAndJoinGameAsync(
            GameSettingsCore<AppSettingsCore> gameSettings,
            PackageKey packageKey,
            ComputerAccountInfo[] computerAccounts,
            bool isMale,
            CancellationToken cancellationToken = default)
        {
            gameSettings.AppSettings.Culture = Thread.CurrentThread.CurrentUICulture.Name;

            return _connection.InvokeAsync<GameCreationResult>(
                "CreateAndJoinGameNew",
                gameSettings,
                packageKey,
                computerAccounts.Select(ca => ca.Account).ToArray(),
                isMale,
                cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
            {
                _connection.Closed -= OnConnectionClosedAsync;

                await _connection.DisposeAsync();
                _connection = null;
            }

            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }

            if (_httpClientHandler != null)
            {
                _httpClientHandler.Dispose();
                _httpClientHandler = null;
            }
        }

        public async Task<Slice<SICore.GameInfo>> GetGamesAsync(int fromId, CancellationToken cancellationToken = default)
        {
            var slice = await _connection.InvokeAsync<Slice<Contract.GameInfo>>("GetGamesSlice", fromId, cancellationToken);

            return new Slice<SICore.GameInfo> { Data = slice.Data.Select(ToSICoreGame).ToArray(), IsLastSlice = slice.IsLastSlice };
        }

        private SICore.GameInfo ToSICoreGame(Contract.GameInfo gameInfo) => new SICore.GameInfo
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

        private static string BuildStage(Contract.GameInfo gameInfo) => gameInfo.Stage switch
        {
            GameStages.Created => Resources.GameStage_Created,
            GameStages.Started => Resources.GameStage_Started,
            GameStages.Round => $"{Resources.GameStage_Round}: {gameInfo.StageName}",
            GameStages.Final => Resources.GameStage_Final,
            _ => Resources.GameStage_Finished,
        };

        private static string BuildRules(Contract.GameInfo gameInfo)
        {
            var rules = gameInfo.Rules;
            var sb = new StringBuilder();

            if (gameInfo.Mode == SIEngine.GameModes.Sport)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(Resources.GameRule_Sport);
            }

            if ((rules & GameRules.FalseStart) == 0)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(Resources.GameRule_NoFalseStart);
            }

            if ((rules & GameRules.Oral) > 0)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(Resources.GameRule_Oral);
            }

            if ((rules & GameRules.IgnoreWrong) > 0)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(Resources.GameRule_IgnoreWrong);
            }

            return sb.ToString();
        }

        public Task<HostInfo> GetGamesHostInfoAsync(CancellationToken cancellationToken = default) =>
            _connection.InvokeAsync<HostInfo>("GetGamesHostInfo", cancellationToken);

        public Task<string> GetNewsAsync(CancellationToken cancellationToken = default) =>
            _connection.InvokeAsync<string>("GetNews", cancellationToken);

        public Task<string[]> GetUsersAsync(CancellationToken cancellationToken = default) =>
            _connection.InvokeAsync<string[]>("GetUsers", cancellationToken);

        public Task<bool> HasPackageAsync(PackageKey packageKey, CancellationToken cancellationToken = default) =>
            _connection.InvokeAsync<bool>("HasPackage", packageKey, cancellationToken);

        public Task<string> HasImageAsync(FileKey imageKey, CancellationToken cancellationToken = default) =>
            _connection.InvokeAsync<string>("HasPicture", imageKey, cancellationToken);

        private async Task<string> AuthenticateUserAsync(
            string user,
            string password,
            CancellationToken cancellationToken = default)
        {
            var uri = "api/Account/LogOn";

            using var content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["login"] = user,
                    ["password"] = password
                });

            var response = await _client.PostAsync(uri, content, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            throw response.StatusCode switch
            {
                HttpStatusCode.Conflict => new Exception(Resources.OnlineUserConflict),
                HttpStatusCode.Forbidden => new Exception(Resources.LoginForbidden),
                _ => new Exception($"Error ({response.StatusCode}): {await response.Content.ReadAsStringAsync()}"),
            };
        }

        public async Task OpenAsync(string userName, CancellationToken cancellationToken = default)
        {
            if (_isOpened)
            {
                throw new InvalidOperationException("Client has been already opened");
            }

            _cookieContainer = new CookieContainer();
            _httpClientHandler = new HttpClientHandler { CookieContainer = _cookieContainer };

            _progressMessageHandler = new ProgressMessageHandler(_httpClientHandler);
            _progressMessageHandler.HttpSendProgress += MessageHandler_HttpSendProgress;

            _client = new HttpClient(_progressMessageHandler)
            {
                BaseAddress = new Uri(ServiceUri),
                Timeout = _options.Timeout
            };
            
            var token = await AuthenticateUserAsync(userName, "", cancellationToken);

            _login = userName;
            
            _connection = new HubConnectionBuilder()
                .WithUrl(
                    $"{ServiceUri}sionline?token={token}",
                    options =>
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

            _connection.Closed += OnConnectionClosedAsync;

            _connection.HandshakeTimeout = TimeSpan.FromMinutes(2);
            
            _connection.On<string, string>("Say", (user, text) => OnUI(() => Receieve?.Invoke(user, text)));
            _connection.On<Contract.GameInfo>("GameCreated", (gameInfo) => OnUI(() => GameCreated?.Invoke(ToSICoreGame(gameInfo))));
            _connection.On<int>("GameDeleted", (gameId) => OnUI(() => GameDeleted?.Invoke(gameId)));
            _connection.On<Contract.GameInfo>("GameChanged", (gameInfo) => OnUI(() => GameChanged?.Invoke(ToSICoreGame(gameInfo))));
            _connection.On<string>("Joined", (user) => OnUI(() => Joined?.Invoke(user)));
            _connection.On<string>("Leaved", (user) => OnUI(() => Leaved?.Invoke(user)));
            _connection.On<Message>("Receive", (message) => IncomingMessage?.Invoke(message));

            await _connection.StartAsync(cancellationToken);

            _isOpened = true;
        }

        private Task OnConnectionClosedAsync(Exception exc) => Closed != null ? Closed(exc) : Task.CompletedTask;

        private void OnUI(Action action)
        {
            if (_uIThreadExecutor != null)
            {
                _uIThreadExecutor.ExecuteOnUIThread(action);
                return;
            }

            action();
        }

        private void MessageHandler_HttpSendProgress(object sender, HttpProgressEventArgs e) => UploadProgress?.Invoke(e.ProgressPercentage);

        public Task SayAsync(string message) => _connection.InvokeAsync("Say", message);

        public async Task UploadPackageAsync(FileKey packageKey, Stream stream, CancellationToken cancellationToken = default)
        {
            var url = "api/upload/package";
            using var content = new StreamContent(stream, _BufferSize);

            try
            {
                using var formData = new MultipartFormDataContent
                {
                    { content, "file", packageKey.Name }
                };

                formData.Headers.ContentMD5 = packageKey.Hash;
                using var response = await _client.PostAsync(url, formData, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await GetErrorMessageAsync(response);
                    throw new Exception(errorMessage);
                }
            }
            catch (TaskCanceledException exc)
            {
                if (!exc.CancellationToken.IsCancellationRequested)
                {
                    throw new Exception(Resources.UploadPackageTimeout, exc);
                }

                throw exc;
            }
        }

        private static async Task<string> GetErrorMessageAsync(HttpResponseMessage response)
        {
            var serverError = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.RequestEntityTooLarge ||
                response.StatusCode == HttpStatusCode.BadRequest && serverError == "Request body too large.")
            {
                return Resources.FileTooLarge;
            }
            
            if (response.StatusCode == HttpStatusCode.BadGateway)
            {
                return $"{response.StatusCode}: Bad Gateway";
            }

            return $"{response.StatusCode}: {serverError}";
        }

        public async Task<string> UploadImageAsync(FileKey imageKey, Stream data, CancellationToken cancellationToken = default)
        {
            var uri = "api/upload/image";

            var bytesContent = new StreamContent(data, _BufferSize);

            using var formData = new MultipartFormDataContent
            {
                { bytesContent, Convert.ToBase64String(imageKey.Hash), imageKey.Name }
            };

            formData.Headers.ContentMD5 = imageKey.Hash;

            var response = await _client.PostAsync(uri, formData, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(await response.Content.ReadAsStringAsync());
            }

            return await response.Content.ReadAsStringAsync();
        }

        public Task<GameCreationResult> JoinGameAsync(
            int gameId,
            GameRole role,
            bool isMale,
            string password,
            CancellationToken cancellationToken = default) =>
            _connection.InvokeAsync<GameCreationResult>("JoinGameNew", gameId, (int)role, isMale, password, cancellationToken);

        public Task SendMessageAsync(Message message, CancellationToken cancellationToken = default) =>
            _connection.InvokeAsync("SendMessage", message, cancellationToken);

        public Task LeaveGameAsync(CancellationToken cancellationToken = default) =>
            _connection.InvokeAsync("LeaveGame", cancellationToken);
    }
}
