using SICore;
using SICore.Network.Servers;
using SIData;
using SIGame.ViewModel.Properties;
using SIGame.ViewModel.ViewModel.Data;
using SIUI.ViewModel.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SIGame.ViewModel
{
    public sealed class SIOnlineViewModel : ConnectionDataViewModel
    {
        private SI.GameServer.Contract.HostInfo _gamesHostInfo;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private GameInfo _currentGame = null;

        public GameInfo CurrentGame
        {
            get { return _currentGame; }
            set
            {
                if (_currentGame != value)
                {
                    _currentGame = value;
                    OnPropertyChanged();
                    if (value != null)
                        UpdateJoinCommand(value.Persons);

                    CheckJoin();
                }
            }
        }

        private bool _canJoin;

        public bool CanJoin
        {
            get { return _canJoin; }
            set
            {
                if (_canJoin != value)
                {
                    _canJoin = value;
                    OnPropertyChanged();
                }
            }
        }
        
        private void CheckJoin()
        {
            CanJoin = _currentGame != null && (!_currentGame.PasswordRequired || !string.IsNullOrEmpty(_password));
        }

        public CustomCommand Cancel { get; set; }

        public CustomCommand AddEmoji { get; set; }

        public GamesFilter GamesFilter
        {
            get { return _userSettings.GamesFilter; }
            set
            {
                if (_userSettings.GamesFilter != value)
                {
                    _userSettings.GamesFilter = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(GamesFilterValue));

                    lock (_serverGamesLock)
                    {
                        RecountGames();
                    }
                }
            }
        }

        public string GamesFilterValue
        {
            get
            {
                var value = "";
                var currentFilter = GamesFilter;

                var onlyNew = (currentFilter & GamesFilter.New) > 0;
                var sport = (currentFilter & GamesFilter.Sport) > 0;
                var tv = (currentFilter & GamesFilter.Tv) > 0;
                var noPassword = (currentFilter & GamesFilter.NoPassword) > 0;

                if ((sport && tv || !sport && !tv) && !onlyNew && !noPassword)
                {
                    value = Resources.GamesFilter_All;
                }
                else
                {
                    if (onlyNew)
                    {
                        value += Resources.GamesFilter_New;
                    }

                    if (sport && !tv)
                    {
                        if (value.Length > 0)
                        {
                            value += ", ";
                        }

                        value += Resources.GamesFilter_Sport;
                    }

                    if (tv && !sport)
                    {
                        if (value.Length > 0)
                        {
                            value += ", ";
                        }

                        value += Resources.GamesFilter_Tv;
                    }

                    if (noPassword)
                    {
                        if (value.Length > 0)
                        {
                            value += ", ";
                        }

                        value += Resources.GamesFilter_NoPassword;
                    }
                }

                return value;
            }
        }

        public bool IsNew
        {
            get { return (GamesFilter & GamesFilter.New) > 0; }
            set
            {
                if (value)
                    GamesFilter |= GamesFilter.New;
                else
                    GamesFilter &= ~GamesFilter.New;
            }
        }

        public bool IsSport
        {
            get { return (GamesFilter & GamesFilter.Sport) > 0; }
            set
            {
                if (value)
                    GamesFilter |= GamesFilter.Sport;
                else
                    GamesFilter &= ~GamesFilter.Sport;
            }
        }

        public bool IsTv
        {
            get { return (GamesFilter & GamesFilter.Tv) > 0; }
            set
            {
                if (value)
                    GamesFilter |= GamesFilter.Tv;
                else
                    GamesFilter &= ~GamesFilter.Tv;
            }
        }

        public bool IsNoPassword
        {
            get { return (GamesFilter & GamesFilter.NoPassword) > 0; }
            set
            {
                if (value)
                    GamesFilter |= GamesFilter.NoPassword;
                else
                    GamesFilter &= ~GamesFilter.NoPassword;
            }
        }

        protected override bool IsOnline => true;

        public ObservableCollection<GameInfo> ServerGames { get; } = new ObservableCollection<GameInfo>();
        public List<GameInfo> ServerGamesCache { get; private set; } = new List<GameInfo>();

        private readonly object _serverGamesLock = new object();

        private string _password = "";

        public string Password
        {
            get { return _password; }
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged();
                    CheckJoin();
                }
            }
        }

        private bool _showSearchBox;

        public bool ShowSearchBox
        {
            get { return _showSearchBox; }
            set
            {
                if (_showSearchBox != value)
                {
                    _showSearchBox = value;
                    OnPropertyChanged();

                    if (!_showSearchBox)
                    {
                        SearchFilter = null;
                    }
                }
            }
        }

        private string _searchFilter;

        public string SearchFilter
        {
            get { return _searchFilter; }
            set
            {
                if (_searchFilter != value)
                {
                    _searchFilter = value;
                    OnPropertyChanged();

                    lock (_serverGamesLock)
                    {
                        RecountGames();
                    }
                }
            }
        }

        private bool _showProgress;

        public bool ShowProgress
        {
            get { return _showProgress; }
            set
            {
                if (_showProgress != value)
                {
                    _showProgress = value;
                    OnPropertyChanged();
                }
            }
        }
        
        private int _uploadProgress;

        public int UploadProgress
        {
            get { return _uploadProgress; }
            set
            {
                if (_uploadProgress != value)
                {
                    _uploadProgress = value;
                    OnPropertyChanged();
                }
            }
        }

        public string[] Emoji { get; } = new string[] { "😃", "😁", "😪", "🎄", "🎓" };

        private string _chatText;

        public string ChatText
        {
            get { return _chatText; }
            set
            {
                if (_chatText != value)
                {
                    _chatText = value;
                    OnPropertyChanged();
                }
            }
        }

        protected override string PackagesPublicBaseUrl => _gamesHostInfo.PackagesPublicBaseUrl;

        protected override string[] ContentPublicBaseUrls => _gamesHostInfo.ContentPublicBaseUrls;

        /// <summary>
        /// Подключение к игровому серверу
        /// </summary>
        private readonly IGameServerClient _gameServerClient;

        public ObservableCollection<string> Users { get; } = new ObservableCollection<string>();

        private readonly object _usersLock = new object();
        
        public SIOnlineViewModel(ConnectionData connectionData, IGameServerClient gameServerclient, CommonSettings commonSettings, UserSettings userSettings)
            : base(connectionData, commonSettings, userSettings)
        {
            _gameServerClient = gameServerclient;
            _gameServerClient.GameCreated += GameServerClient_GameCreated;
            _gameServerClient.GameDeleted += GameServerClient_GameDeleted;
            _gameServerClient.GameChanged += GameServerClient_GameChanged;

            _gameServerClient.Joined += GameServerClient_Joined;
            _gameServerClient.Leaved += GameServerClient_Leaved;
            _gameServerClient.Receieve += OnMessage;

            _gameServerClient.Reconnecting += GameServerClient_Reconnecting;
            _gameServerClient.Reconnected += GameServerClient_Reconnected;
            _gameServerClient.Closed += GameServerClient_Closed;

            _gameServerClient.UploadProgress += GameServerClient_UploadProgress;

            ServerAddress = _gameServerClient.ServerAddress;

            AddEmoji = new CustomCommand(AddEmoji_Executed);
        }

        private Task GameServerClient_Reconnecting(Exception exc)
        {
            OnMessage(Resources.App_Name, $"{Resources.ReconnectingMessage} {exc?.Message}");
            return Task.CompletedTask;
        }

        private Task GameServerClient_Reconnected(string message)
        {
            OnMessage(Resources.App_Name, Resources.ReconnectedMessage);

            UI.Execute(async () =>
            {
                await ReloadGamesAsync();
                await ReloadUsersAsync();
            }, exc =>
            {
                Error = exc.Message;
            });

            return Task.CompletedTask;
        }

        private void AddEmoji_Executed(object arg)
        {
            ChatText += arg.ToString();
        }

        private void GameServerClient_UploadProgress(int progress)
        {
            UploadProgress = progress;
        }

        private Task GameServerClient_Closed(Exception exception)
        {
            PlatformSpecific.PlatformManager.Instance.ShowMessage($"{Resources.LostConnection}: {exception?.Message}",
                PlatformSpecific.MessageType.Warning);
            Cancel.Execute(null);

            return Task.CompletedTask;
        }

        public event Action<string, string> Message;

        private void OnMessage(string userName, string message)
        {
            Message?.Invoke(userName, message);
        }

        private void GameServerClient_Leaved(string userName)
        {
            lock (_usersLock)
            {
                Users.Remove(userName);
            }
        }

        private void GameServerClient_Joined(string userName)
        {
            lock (_usersLock)
            {
                var inserted = false;

                var length = Users.Count;
                for (int i = 0; i < length; i++)
                {
                    var comparison = Users[i].CompareTo(userName);
                    if (comparison == 0)
                    {
                        inserted = true;
                        break;
                    }

                    if (comparison > 0)
                    {
                        Users.Insert(i, userName);
                        inserted = true;
                        break;
                    }                        
                }

                if (!inserted)
                    Users.Add(userName);
            }
        }

        private void GameServerClient_GameChanged(GameInfo gameInfo)
        {
            lock (_serverGamesLock)
            {
                for (int i = 0; i < ServerGamesCache.Count; i++)
                {
                    if (ServerGamesCache[i].GameID == gameInfo.GameID)
                    {
                        ServerGamesCache[i] = gameInfo;
                        break;
                    }
                }

                RecountGames();
            }
        }

        private void GameServerClient_GameDeleted(int id)
        {
            lock (_serverGamesLock)
            {
                for (int i = 0; i < ServerGamesCache.Count; i++)
                {
                    if (ServerGamesCache[i].GameID == id)
                    {
                        ServerGamesCache.RemoveAt(i);
                        break;
                    }
                }

                RecountGames();
            }
        }

        private bool FilteredOk(GameInfo game) =>
            string.IsNullOrWhiteSpace(SearchFilter) ||
                !SearchFilter.StartsWith(CommonSettings.OnlineGameUrl) &&
                    CultureInfo.CurrentUICulture.CompareInfo.IndexOf(game.GameName, SearchFilter.Trim(), CompareOptions.IgnoreCase) >= 0 ||
                SearchFilter.StartsWith(CommonSettings.OnlineGameUrl) &&
                    int.TryParse(SearchFilter.Substring(CommonSettings.OnlineGameUrl.Length), out int gameId) &&
                    game.GameID == gameId;

        private bool FilterGame(GameInfo gameInfo)
        {
            if ((GamesFilter & GamesFilter.New) > 0 && gameInfo.RealStartTime != DateTime.MinValue)
            {
                return false;
            }

            if ((GamesFilter & GamesFilter.Sport) == 0 && (GamesFilter & GamesFilter.Tv) > 0 && gameInfo.Mode == SIEngine.GameModes.Sport)
            {
                return false;
            }

            if ((GamesFilter & GamesFilter.Sport) > 0 && (GamesFilter & GamesFilter.Tv) == 0 && gameInfo.Mode == SIEngine.GameModes.Tv)
            {
                return false;
            }

            if ((GamesFilter & GamesFilter.NoPassword) > 0 && gameInfo.PasswordRequired)
            {
                return false;
            }

            if (!FilteredOk(gameInfo))
            {
                return false;
            }

            return true;
        }

        private void GameServerClient_GameCreated(GameInfo gameInfo)
        {
            lock (_serverGamesLock)
            {
                ServerGamesCache.Add(gameInfo);
                RecountGames();
            }
        }

        private void InsertGame(GameInfo gameInfo)
        {
            var gameName = gameInfo.GameName;
            var length = ServerGames.Count;

            var inserted = false;

            for (int i = 0; i < length; i++)
            {
                var comparison = ServerGames[i].GameName.CompareTo(gameName);
                if (comparison == 0)
                {
                    inserted = true;
                    break;
                }

                if (comparison > 0)
                {
                    ServerGames.Insert(i, gameInfo);
                    inserted = true;
                    break;
                }
            }

            if (!inserted)
                ServerGames.Add(gameInfo);
        }

        public async Task Init()
        {
            try
            {
                IsProgress = true;
                _gamesHostInfo = await _gameServerClient.GetGamesHostInfoAsync(_cancellationTokenSource.Token);
                await ReloadGamesAsync();
                await ReloadUsersAsync();
                _avatar = (await UploadAvatar(Human, CancellationToken.None))?.Item1;
            }
            catch (Exception exc)
            {
                PlatformSpecific.PlatformManager.Instance.ShowMessage(exc.ToString(), PlatformSpecific.MessageType.Warning, true);
                Cancel.Execute(null);
            }
        }

        public async void Load()
        {
            try
            {
                var news = await _gameServerClient.GetNews();

                if (!string.IsNullOrEmpty(news))
                {
                    OnMessage(Resources.News, news);
                }
            }
            catch (Exception exc)
            {
                Error = exc.Message;
                FullError = exc.ToString();
            }
        }

        private async Task ReloadUsersAsync()
        {
            try
            {
                var users = await _gameServerClient.GetUsers();
                Array.Sort(users);
                lock (_usersLock)
                {
                    Users.Clear();
                    foreach (var user in users)
                    {
                        Users.Add(user);
                    }
                }
            }
            catch (Exception exc)
            {
                Error = exc.Message;
                FullError = exc.ToString();
            }
        }

        protected override async Task ClearConnection()
        {
            await _gameServerClient.DisposeAsync();

            await base.ClearConnection();
        }

        private async Task ReloadGamesAsync(CancellationToken cancellationToken = default)
        {
            IsProgress = true;
            Error = "";
            try
            {
                lock (_serverGamesLock)
                {
                    ServerGamesCache.Clear();
                    RecountGames();
                }

                SI.GameServer.Contract.Slice<GameInfo> gamesSlice = null;
                var whileGuard = 100;
                do
                {
                    var fromId = gamesSlice != null && gamesSlice.Data.Length > 0 ? gamesSlice.Data.Last().GameID + 1 : 0;
                    gamesSlice = await _gameServerClient.GetGamesAsync(fromId, cancellationToken);

                    lock (_serverGamesLock)
                    {
                        ServerGamesCache.AddRange(gamesSlice.Data);
                        RecountGames();
                    }

                    whileGuard--;
                } while (!gamesSlice.IsLastSlice && whileGuard > 0);
            }
            catch (Exception exc)
            {
                Error = exc.Message;
                FullError = exc.ToString();
            }
            finally
            {
                IsProgress = false;
            }
        }

        private void RecountGames()
        {
            var serverGames = ServerGames.ToArray();
            for (var i = 0; i < serverGames.Length; i++)
            {
                var item = serverGames[i];
                var game = ServerGamesCache.FirstOrDefault(sg => sg.GameID == item.GameID);
                if (game == null || !FilterGame(game))
                    ServerGames.Remove(item);
            }

            serverGames = ServerGames.ToArray();
            for (var i = 0; i < serverGames.Length; i++)
            {
                var item = serverGames[i];
                var game = ServerGamesCache.FirstOrDefault(sg => sg.GameID == item.GameID);
                if (game != null && game != item)
                    ServerGames[i] = game;
            }

            for (int i = 0; i < ServerGamesCache.Count; i++)
            {
                var item = ServerGamesCache[i];

                var game = ServerGames.FirstOrDefault(sg => sg.GameID == item.GameID);
                if (game == null && FilterGame(item))
                    InsertGame(item);
            }

            if (CurrentGame != null && !ServerGames.Contains(CurrentGame))
                CurrentGame = null;

            if (CurrentGame == null && ServerGames.Any())
                CurrentGame = ServerGames[0];
        }

        protected override void Prepare(GameSettingsViewModel gameSettings)
        {
            base.Prepare(gameSettings);

            gameSettings.NetworkGameType = NetworkGameType.GameServer;
            gameSettings.CreateGame += CreateGame;
        }

        private async Task<Tuple<SlaveServer, IViewerClient>> CreateGame(GameSettings settings, PackageSources.PackageSource packageSource)
        {
            var gameSettingsViewModel = gameSettings;
            gameSettingsViewModel.Message = Resources.PackageCheck;

            var cancellationTokenSource = gameSettingsViewModel.CancellationTokenSource = new CancellationTokenSource();

            var hash = await packageSource.GetPackageHashAsync();
            var packageKey = new PackageKey { Name = packageSource.GetPackageName(), Hash = hash, ID = packageSource.GetPackageId() };

            var hasPackage = await _gameServerClient.HasPackage(packageKey);

            if (!hasPackage)
            {
                var data = await packageSource.GetPackageDataAsync();
                if (data == null)
                {
                    throw new Exception(Resources.BadPackage);
                }

                using (data)
                {
                    if (cancellationTokenSource.IsCancellationRequested)
                        return null;

                    gameSettingsViewModel.Message = Resources.SendingPackageToServer;
                    ShowProgress = true;
                    try
                    {
                        await _gameServerClient.UploadPackage(packageKey, data, cancellationTokenSource.Token);
                    }
                    finally
                    {
                        ShowProgress = false;
                    }
                }
            }

            gameSettingsViewModel.Message = Resources.Preparing;

            var computerAccounts = await ProcessCustomPersonsAsync(settings, cancellationTokenSource.Token);

            gameSettingsViewModel.Message = Resources.Creating;

            var gameCreatingResult = await _gameServerClient.CreateGame((GameSettingsCore<AppSettingsCore>)settings, packageKey, computerAccounts.ToArray(), null);

            if (gameCreatingResult.Code != SI.GameServer.Contract.GameCreationResultCode.Ok)
            {
                throw new Exception(GetMessage(gameCreatingResult.Code));
            }

            gameSettingsViewModel.Message = Resources.GameEntering;

            await ConnectToServerAsHostAsync(gameCreatingResult.GameId, settings);
            if (_host == null)
            {
                return null;
            }

            return Tuple.Create(_server, _host);
        }

        private static string GetMessage(SI.GameServer.Contract.GameCreationResultCode gameCreationResultCode)
        {
            switch (gameCreationResultCode)
            {
                case SI.GameServer.Contract.GameCreationResultCode.NoPackage:
                    return Resources.GameCreationError_NoPackage;

                case SI.GameServer.Contract.GameCreationResultCode.TooMuchGames:
                    return Resources.GameCreationError_TooManyGames;

                case SI.GameServer.Contract.GameCreationResultCode.ServerUnderMaintainance:
                    return Resources.GameCreationError_ServerMaintainance;

                case SI.GameServer.Contract.GameCreationResultCode.BadPackage:
                    return Resources.GameCreationError_BadPackage;

                case SI.GameServer.Contract.GameCreationResultCode.GameNameCollision:
                    return Resources.GameCreationError_DuplicateName;

                case SI.GameServer.Contract.GameCreationResultCode.InternalServerError:
                    return Resources.GameCreationError_ServerError;

                case SI.GameServer.Contract.GameCreationResultCode.ServerNotReady:
                    return Resources.GameCreationError_ServerNotReady;

                case SI.GameServer.Contract.GameCreationResultCode.YourClientIsObsolete:
                    return Resources.GameCreationError_ObsoleteVersion;

                case SI.GameServer.Contract.GameCreationResultCode.UnknownError:
                    return Resources.GameCreationError_UnknownReason;

                case SI.GameServer.Contract.GameCreationResultCode.JoinError:
                    return Resources.GameCreationError_JoinError;

                case SI.GameServer.Contract.GameCreationResultCode.WrongGameSettings:
                    return Resources.GameCreationError_WrongSettings;

                default:
                    return Resources.GameCreationError_UnknownReason;
            }
        }

        private async Task<List<ComputerAccountInfo>> ProcessCustomPersonsAsync(GameSettings settings, CancellationToken cancellationToken)
        {
            var computerAccounts = new List<ComputerAccountInfo>();
            foreach (var player in settings.Players)
            {
                await ProcessPerson(computerAccounts, player, cancellationToken);
            }

            await ProcessPerson(computerAccounts, settings.Showman, cancellationToken);

            return computerAccounts;
        }

        private async Task ProcessPerson(List<ComputerAccountInfo> computerAccounts, Account account, CancellationToken cancellationToken)
        {
            if (!account.IsHuman && account.CanBeDeleted) // Нестандартный игрок, нужно передать его параметры на сервер
            {
                var avatar = (await UploadAvatar(account, cancellationToken))?.Item1;

                var computerAccount = new ComputerAccount((ComputerAccount)account) { Picture = avatar };

                computerAccounts.Add(new ComputerAccountInfo { Account = computerAccount });
            }
        }

        private async Task<Tuple<string, FileKey>> UploadAvatar(Account account, CancellationToken cancellationToken)
        {
            var picture = account.Picture;
            if (!Uri.TryCreate(picture, UriKind.Absolute, out Uri pictureUri))
            {
                return null;
            }

            if (pictureUri.Scheme != "file" || !File.Exists(picture)) // Это локальный файл, и его нужно отправить на сервер
            {
                return null;
            }

            byte[] fileHash = null;
            using (var stream = File.OpenRead(picture))
            {
                using (var sha1 = new System.Security.Cryptography.SHA1Managed())
                {
                    fileHash = sha1.ComputeHash(stream);
                }
            }

            var fileKey = new FileKey { Name = Path.GetFileName(picture), Hash = fileHash };

            // Если файла на сервере нет, загрузим его
            var picturePath = await _gameServerClient.HasPicture(fileKey);
            if (picturePath == null)
            {
                using (var stream = File.OpenRead(picture))
                {
                    picturePath = await _gameServerClient.UploadPicture(fileKey, stream, cancellationToken);
                }
            }

            if (!Uri.IsWellFormedUriString(picturePath, UriKind.Absolute))
            {
                var rootAddress = _gamesHostInfo.ContentPublicBaseUrls.FirstOrDefault() ?? _gameServerClient.ServerAddress;
                picturePath = rootAddress + picturePath;
            }

            return Tuple.Create(picturePath, fileKey);
        }

        protected override string GetExtraCredentials() =>
            !string.IsNullOrEmpty(_password) ? $"\n{_password}" : "";

        public override async Task JoinGameAsync(GameInfo gameInfo, GameRole role, bool isHost = false)
        {
            gameInfo = gameInfo ?? _currentGame;

            if (!isHost)
            {
                lock (_serverGamesLock)
                {
                    var passwordRequired = gameInfo != null && gameInfo.PasswordRequired;
                    if (passwordRequired && string.IsNullOrEmpty(_password))
                    {
                        IsProgress = false;
                        return;
                    }
                }
            }

            try
            {
                InitServerAndClient(_gamesHostInfo.Host ?? new Uri(ServerAddress).Host, _gamesHostInfo.Port);
                await ConnectCore(true);
                var result = await _connector.SetGameID(gameInfo.GameID);
                if (!result)
                {
                    Error = Resources.CreatedGameNotFound;
                    return;
                }

                await base.JoinGameAsync(gameInfo, role, isHost);
                _host.Connector.SetGameID(gameInfo.GameID);
            }
            catch (TaskCanceledException exc)
            {
                Error = Resources.GameConnectionTimeout;
                FullError = exc.ToString();
            }
            catch (Exception exc)
            {
                Error = exc.Message;
                FullError = exc.ToString();
            }
        }

        internal async Task ConnectToServerAsHostAsync(int gameID, GameSettings gameSettings)
        {
            var name = Human.Name;

            _password = gameSettings.NetworkGamePassword;
            var game = new GameInfo { GameID = gameID, Owner = name };

            await JoinGame(game, gameSettings.Role, true);
        }

        public void Say(string message, bool system = false)
        {
            if (!system)
            {
                _gameServerClient.Say(message);
            }
        }

        protected override void CloseContent_Executed(object arg)
        {
            if (gameSettings != null)
            {
                gameSettings.CancellationTokenSource?.Cancel();
            }

            base.CloseContent_Executed(arg);
        }

        public override ValueTask DisposeAsync()
        {
            if (gameSettings != null)
            {
                gameSettings.CancellationTokenSource?.Cancel();
            }

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();

            return base.DisposeAsync();
        }
    }
}
