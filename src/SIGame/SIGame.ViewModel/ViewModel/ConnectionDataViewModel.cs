using System;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using SICore;
using SIGame.ViewModel.PlatformSpecific;
using SIGame.ViewModel.Properties;
using System.Threading;
using SICore.BusinessLogic;
using SICore.Network.Clients;
using SICore.Network.Servers;
using SICore.Network;
using SICore.Network.Configuration;

namespace SIGame.ViewModel
{
    /// <summary>
    /// Подключение к существующей игре.
    /// Происходит по этапам:
    /// 1. Подключение к Игровому серверу СИ.
    /// 2. Подключение к конкретному серверу.
    /// 3. Получение информации об игре.
    /// 4. Вход в игру.
    /// При прямом подключении пункт 1 пропускается.
    /// При реконнекте всегда выполняются только пункты 2 и 4.
    /// Всё API сделано асинхронным, чтобы не блокировать пользовательский интерфейс.
    /// </summary>
    public abstract class ConnectionDataViewModel: ViewModelWithNewAccount<ConnectionData>, IAsyncDisposable
    {
        #region Commands

        private ExtendedCommand _join;

        // Команда для этапа 3 - получения информации об игре - не нужна, т.к. всё происходит автоматически после пункта 2
        /// <summary>
        /// Войти в игру
        /// </summary>
        public ICommand Join => _join;

        public ICommand NewGame { get; private set; }

        protected abstract bool IsOnline { get; }

        protected SlaveServer _server;
        private Client _client;
        protected IViewerClient _host;
        protected Connector _connector;

        protected virtual string PackagesPublicBaseUrl { get; } = null;

        protected virtual string[] ContentPublicBaseUrls { get; } = null;

        protected void UpdateJoinCommand(ConnectionPersonData[] persons)
        {
            _join.ExecutionArea.Clear();

            // Можно под кем-то подключиться
            var showman = persons.FirstOrDefault(p => p.Role == GameRole.Showman);
            if (showman != null && !showman.IsOnline)
                _join.ExecutionArea.Add(GameRole.Showman);

            var players = persons.Where(p => p.Role == GameRole.Player);
            if (players.Any(p => !p.IsOnline))
                _join.ExecutionArea.Add(GameRole.Player);

            _join.ExecutionArea.Add(GameRole.Viewer);

            _join.OnCanBeExecutedChanged();
        }

        #endregion

        #region Data

        private bool _releaseServer = true;

        private bool _isProgress = false;

        public bool IsProgress
        {
            get { return _isProgress; }
            set { _isProgress = value; OnPropertyChanged(); }
        }

        private string _error = "";

        public string Error
        {
            get { return _error; }
            set
            {
                if (_error != value)
                {
                    _error = value;
                    OnPropertyChanged();

                    if (string.IsNullOrEmpty(_error))
                        FullError = null;
                }
            }
        }
        
        public string ServerAddress { get; protected set; }

        #endregion

        #region Init

        private readonly CommonSettings _commonSettings;
        protected UserSettings _userSettings;

        protected ConnectionDataViewModel(CommonSettings commonSettings, UserSettings userSettings)
        {
            _commonSettings = commonSettings;
            _userSettings = userSettings;
        }

        protected ConnectionDataViewModel(ConnectionData connectionData, CommonSettings commonSettings, UserSettings userSettings)
            : base(connectionData)
        {
            _commonSettings = commonSettings;
            _userSettings = userSettings;
        }

        protected override void Initialize()
        {
            base.Initialize();

            _join = new ExtendedCommand(Join_Executed);
            NewGame = new CustomCommand(NewGame_Executed);
        }

        protected GameSettingsViewModel gameSettings;

        private void NewGame_Executed(object arg)
        {
            gameSettings = new GameSettingsViewModel(_userSettings.GameSettings, _commonSettings, _userSettings, true) { Human = Human, ChangeSettings = ChangeSettings };
            gameSettings.StartGame += OnStartGame;
            gameSettings.PrepareForGame();

            Prepare(gameSettings);

            var contextBox = new ContentBox
            {
                Data = gameSettings,
                Title = Resources.NewGame
            };

            Content = new NavigatorViewModel
            {
                Content = contextBox,
                Cancel = _closeContent
            };
        }

        protected override void OnStartGame(Server server, IViewerClient host, bool networkGame, bool isOnline, string tempDocFolder, int networkGamePort)
            => base.OnStartGame(server, host, networkGame, IsOnline, tempDocFolder, networkGamePort);

        protected virtual void Prepare(GameSettingsViewModel gameSettings)
        {
           
        }

        #endregion

        #region Начальное подключение к серверу

        protected void InitServerAndClient(string address, int port)
        {
            if (_server != null)
            {
                _server.Dispose();
                _server = null;
            }

            _server = new TcpSlaveServer(port, address, ServerConfiguration.Default, new NetworkLocalizer(Thread.CurrentThread.CurrentUICulture.Name));
            _client = new Client("");
            _client.ConnectTo(_server);
        }

        protected async Task ConnectCore(bool upgrade)
        {
            await _server.Connect(upgrade);

            if (_connector != null)
                _connector.Dispose();

            _connector = new Connector(_server, _client);
        }

        #endregion

        #region Вход в игру

        private async void Join_Executed(object arg)
        {
            await JoinGame(null, (GameRole)arg);
        }

        protected virtual async Task JoinGame(GameInfo gameInfo, GameRole role, bool host = false)
        {
            IsProgress = true;

            try
            {
                await JoinGameAsync(gameInfo, role, host);
            }
            catch (Exception exc)
            {
                try
                {
                    Error = exc.Message;
                    FullError = exc.ToString();
                    if (_host != null)
                        _host.Dispose();
                }
                catch { }
            }
            finally
            {
                IsProgress = false;
            }
        }

        public virtual async Task JoinGameAsync(GameInfo gameInfo, GameRole role, bool isHost = false)
        {
            var name = Human.Name;

            var sex = Human.IsMale ? 'm' : 'f';
            var command = $"{Messages.Connect}\n{role.ToString().ToLowerInvariant()}\n{name}\n{sex}\n{-1}{GetExtraCredentials()}";

            _ = await _connector.JoinGame(command);
            JoinGameCompleted(role, isHost);
        }

        protected virtual string GetExtraCredentials() => "";

        protected string _avatar;

        private void JoinGameCompleted(GameRole role, bool isHost)
        {
            lock (_server.ConnectionsSync)
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
                    if (_host != null)
                    {
                        _host.Dispose();
                    }

                    return;
                }
            }

            var humanPlayer = Human;
            var name = humanPlayer.Name;

            var data = new ViewerData
            {
                BackLink = BackLink.Default,
                ServerPublicUrl = PackagesPublicBaseUrl,
                ContentPublicUrls = ContentPublicBaseUrls,
                ServerAddress = ServerAddress
            };

            var localizer = new Localizer(Thread.CurrentThread.CurrentUICulture.Name);

            switch (role)
            {
                case GameRole.Showman:
                    _host = new Showman(_client, humanPlayer, isHost, localizer, data);
                    break;

                case GameRole.Player:
                    _host = new Player(_client, humanPlayer, isHost, localizer, data);
                    break;

                default:
                    _host = new SimpleViewer(_client, humanPlayer, isHost, localizer, data);
                    break;
            }

            _host.Avatar = _avatar;

            _host.Connector = new ReconnectManager(_server, _client, _host, humanPlayer, role, GetExtraCredentials(), IsOnline) { ServerAddress = ServerAddress };

            if (_host.Client.Name != name)
            {
                _host.Rename(name);
            }

            _host.Client.ConnectTo(_server);

            _releaseServer = false;
            if (!isHost && Ready != null)
            {
                Ready(_server, _host, IsOnline); // Здесь происходит переход к игре
            }

            if (_connector != null)
            {
                _connector.Dispose();
                _connector = null;
            }

            ClearConnection();

            _host.GetInfo();

            Error = null;

            _server.Error += Server_Error;
        }

        private void Server_Error(Exception exc, bool isWarning)
        {
            PlatformManager.Instance.ShowMessage($"{Resources.GameEngineError}: {exc.Message} {exc.InnerException}", isWarning ? MessageType.Warning : MessageType.Error, true);
        }

        protected virtual Task ClearConnection() => Task.CompletedTask;

        public event Action<Server, IViewerClient, bool> Ready;

        #endregion

        public virtual async ValueTask DisposeAsync()
        {
            await ClearConnection();

            if (_connector != null)
            {
                _connector.Dispose();
                _connector = null;
            }

            if (_releaseServer && _server != null)
            {
                _server.Dispose();
                _server = null;
            }
        }
    }
}
