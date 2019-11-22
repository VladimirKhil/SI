using SIEngine;
using SImulator.Model;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel.Core;
using SImulator.ViewModel.PlatformSpecific;
using SImulator.ViewModel.ViewModel;
using SIPackages;
using SIUI.ViewModel;
using SIUI.ViewModel.Core;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SImulator.ViewModel
{
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        #region Constants
        /// <summary>
        /// Максимальное число игровых кнопок, которое можно зарегистрировать в программе
        /// </summary>
        private const int MaxNumberOfButtons = 12;
        /// <summary>
        /// Название продукта
        /// </summary>
        public const string ProductName = "СИмулятор";

        #endregion

        private bool _lockPlayerButtonSync = false;

        /// <summary>
        /// Менеджер игровых кнопок
        /// </summary>
        private IButtonManager _buttonManager;

        private readonly SimpleUICommand _start;

        private readonly SimpleCommand _selectPackage;
        private readonly SimpleCommand _selectVideo;
        private readonly SimpleCommand _selectLogsFolder;

        private readonly SimpleUICommand _listen;
        private readonly SimpleUICommand _stopListen;

        private readonly SimpleUICommand _addPlayerButton;
        private readonly SimpleUICommand _setPlayerButton;
        private readonly SimpleCommand _removePlayerButton;

        private readonly SimpleCommand _refresh;

        public ICommand Start => _start;

        public ICommand SelectPackage => _selectPackage;
        public ICommand SelectVideoFile => _selectVideo;
        public ICommand SelectLogoFile { get; private set; }
        public ICommand SelectLogsFolder => _selectLogsFolder;

        public ICommand DeletePlayerKey => _removePlayerButton;

        public ICommand Refresh => _refresh;

        public ICommand NavigateToSite { get; private set; }

        public ICommand SelectColor { get; private set; }

        public ICommand AddPlayer { get; private set; }
        public ICommand RemovePlayer { get; private set; }

        private ICommand _activeListenCommand;

        public ICommand ActiveListenCommand
        {
            get { return _activeListenCommand; }
            set { _activeListenCommand = value; OnPropertyChanged(); }
        }

        private ICommand _activePlayerButtonCommand;

        public ICommand ActivePlayerButtonCommand
        {
            get { return _activePlayerButtonCommand; }
            set { if (_activePlayerButtonCommand != value) { _activePlayerButtonCommand = value; OnPropertyChanged(); } }
        }

        private IPackageSource _packageSource;

        /// <summary>
        /// Путь к отыгрываемому документу
        /// </summary>
        public IPackageSource PackageSource
        {
            get { return _packageSource; }
            set
            {
                if (_packageSource != value)
                {
                    _packageSource = value;
                    OnPropertyChanged();
                    UpdateStartCommand();
                }
            }
        }

        public AppSettings Settings { get; }

        public AppSettingsViewModel SettingsViewModel { get; }

        private string[] _comPorts;

        public string[] ComPorts
        {
            get
            {
                if (_comPorts == null)
                {
                    _comPorts = PlatformManager.Instance.GetComPorts();

                    if (Settings.ComPort == null || _comPorts != null && _comPorts.Length > 0)
                        Settings.ComPort = _comPorts[0];
                }

                return _comPorts;
            }
        }

        private GameEngine _game;

        public GameEngine Game
        {
            get { return _game; }
            private set
            {
                if (_game != value)
                {
                    _game = value;
                    OnPropertyChanged();
                }
            }
        }

        private GameMode _mode = GameMode.Start;

        public GameMode Mode
        {
            get { return _mode; }
            set
            {
                if (_mode != value)
                {
                    _mode = value;
                    OnPropertyChanged();
                    OnModeChanged();
                }
            }
        }

        private ModeTransition _transition = ModeTransition.ModeratorToStart;

        public ModeTransition Transition
        {
            get { return _transition; }
            set { _transition = value; OnPropertyChanged(); }
        }

        private bool _isRemoteControlling = false;

        public bool IsRemoteControlling
        {
            get { return _isRemoteControlling; }
            set
            {
                if (_isRemoteControlling != value)
                {
                    _isRemoteControlling = value;
                    OnPropertyChanged();

                    if (value && _computers == null)
                        GetComputers();

                    UpdateStartCommand();
                }
            }
        }

        private string[] _computers = null;

        public string[] Computers
        {
            get
            {
                if (_computers == null)
                {
                    GetComputers();
                }

                return _computers;
            }
        }

        public bool CanSelectScreens
        {
            get { return (_mode == GameMode.Start) && Screens.Length > 1; }
        }

        public IScreen[] Screens { get; private set; }

        public string Host
        {
            get
            {
                return "[Ваш IP-адрес]";
            }
        }

        /// <summary>
        /// Список игроков, отображаемых на табло в особом режиме игры
        /// </summary>
        public ObservableCollection<SimplePlayerInfo> Players { get; set; }

        public MainViewModel(AppSettings settings)
        {
            UI.Initialize();

            Settings = settings;
            SettingsViewModel = new AppSettingsViewModel(Settings);

            _start = new SimpleUICommand(Start_Executed) { Name = "Начать игру" };

            _selectPackage = new SimpleCommand(SelectPackage_Executed);
            SelectLogoFile = new SimpleCommand(SelectLogoFile_Executed);
            _selectVideo = new SimpleCommand(SelectVideo_Executed);
            _selectLogsFolder = new SimpleCommand(SelectLogsFolder_Executed);

            _refresh = new SimpleCommand(Refresh_Executed);

            _listen = new SimpleUICommand(Listen_Executed) { Name = "Ожидать подключения" };
            _stopListen = new SimpleUICommand(StopListen_Executed) { Name = "Прекратить ожидание" };

            _addPlayerButton = new SimpleUICommand(AddPlayerButton_Executed) { Name = "Добавить" };
            _setPlayerButton = new SimpleUICommand(SetPlayerButton_Executed) { Name = "Нажмите на кнопку" };
            _removePlayerButton = new SimpleCommand(RemovePlayerButton_Executed);

            NavigateToSite = new SimpleCommand(NavigateToSite_Executed);
            SelectColor = new SimpleCommand(SelectColor_Executed);

            AddPlayer = new SimpleCommand(AddPlayer_Executed);
            RemovePlayer = new SimpleCommand(RemovePlayer_Executed);

            ActiveListenCommand = _listen;
            ActivePlayerButtonCommand = _addPlayerButton;

            UpdateStartCommand();
            UpdateCanAddPlayerButton();

            Screens = PlatformManager.Instance.GetScreens();
            Players = new ObservableCollection<SimplePlayerInfo>();

            var screensLength = Screens.Length;
            if (Settings.ScreenNumber == -1 || Settings.ScreenNumber >= screensLength)
                Settings.ScreenNumber = screensLength - 1;

            Settings.PropertyChanged += MyDefault_PropertyChanged;

#if DEBUG
            Settings.ScreenNumber = Math.Max(0, this.Screens.Length - 2);
#endif

            SetIsRemoteControlling();
        }

        private void AddPlayer_Executed(object arg)
        {
            var info = new PlayerInfo();
            Players.Add(info);
        }

        private void RemovePlayer_Executed(object arg)
        {
            if (!(arg is SimplePlayerInfo player))
                return;

            Players.Remove(player);
        }

        private void NavigateToSite_Executed(object arg)
        {
            try
            {
                PlatformManager.Instance.NavigateToSite();
            }
            catch (Exception exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Возникла ошибка при переходе на сайт программы (http://vladimirkhil.com). Убедитесь, что у вас настроен браузер по умолчанию.\r\n{0}", exc.Message));
            }
        }

        private void SelectColor_Executed(object arg)
        {
            var color = PlatformManager.Instance.AskSelectColor();
            if (color == null)
                return;

            var settings = Settings.SIUISettings;
            if (Convert.ToInt32(arg) == 0)
                settings.TableColorString = color;
            else
                settings.TableBackColorString = color;
        }

        private void MyDefault_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AppSettings.IsRemoteControlAllowed):
                    if (_computers == null)
                        GetComputers();

                    UpdateStartCommand();
                    break;

                case nameof(AppSettings.ScreenNumber):
                    SetIsRemoteControlling();
                    break;

                case nameof(AppSettings.RemotePCName):
                    UpdateStartCommand();
                    break;

                case nameof(AppSettings.UsePlayersKeys):
                    Settings.PlayerKeys2.Clear();
                    UpdateCanAddPlayerButton();
                    break;

                case nameof(AppSettings.PlayersView):
                    UpdatePlayersView();
                    break;
            }
        }

        private void SetIsRemoteControlling()
        {
            IsRemoteControlling = Screens[Settings.ScreenNumber].IsRemote;
        }

        /// <summary>
        /// Начало игры
        /// </summary>
        /// <param name="arg"></param>
        private async void Start_Executed(object arg)
        {
            try
            {
                var packageStream = await _packageSource.GetPackageAsync();

                var engineSettingsProvider = new EngineSettingsProvider(SettingsViewModel.Model);
                EngineBase engine;

                try
                {
                    var document = SIDocument.Load(packageStream);
                    engine = SettingsViewModel.Model.GameMode == GameModes.Tv ? (EngineBase)new TvEngine(document, engineSettingsProvider) : new SportEngine(document, engineSettingsProvider);
                }
                catch (Exception exc)
                {
                    throw new Exception(string.Format("Ошибка при загрузке игрового пакета: {0}", exc.Message));
                }

                var gameHost = PlatformManager.Instance.CreateGameHost(engine);

                IRemoteGameUI ui;
                if (_isRemoteControlling)
                {
                    if (!Connect(gameHost, out ui))
                        return;
                }
                else
                {
                    ui = new RemoteGameUI { GameHost = gameHost, ScreenIndex = SettingsViewModel.Model.ScreenNumber };
                    ui.UpdateSettings(SettingsViewModel.SIUISettings.Model);
                }

                var game = new GameEngine(SettingsViewModel, engine, gameHost, ui, Players, _isRemoteControlling);

                game.Start();

                game.Error += ShowError;
                game.RequestStop += Game_RequestStop;

                var recent = Settings.Recent;
                if (!string.IsNullOrEmpty(_packageSource.Token) && !recent.Contains(_packageSource.Token))
                {
                    recent.Insert(0, _packageSource.Token);
                    if (recent.Count > 10)
                        recent.RemoveAt(10);
                }

                Mode = GameMode.Moderator;
                Game = game;
            }
            catch (Exception exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка старта игры: {0}", exc.ToString()), false);
                return;
            }
        }

        public static Binding GetBinding()
        {
            var binding = new NetTcpBinding();
            binding.Security.Mode = SecurityMode.None;
            return binding;
        }

        private DuplexChannelFactory<IRemoteGameUI> factory = null;

        internal bool Connect(IGameHost gameHost, out IRemoteGameUI ui)
        {
            try
            {
                factory = new DuplexChannelFactory<IRemoteGameUI>(new InstanceContext(gameHost), GetBinding(), new EndpointAddress(string.Format("net.tcp://{1}:{0}/simulator", SettingsViewModel.Model.HttpPort, SettingsViewModel.Model.RemotePCName)));
                factory.Open();
                ui = factory.CreateChannel();

                ui.UpdateSettings(SettingsViewModel.SIUISettings.Model); // Проверим соединение заодно

                ((IChannel)ui).Closed += GameEngine_Closed;
                ((IChannel)ui).Faulted += GameEngine_Closed;

                return true;
            }
            catch (Exception exc)
            {
                ui = null;
                ShowError(exc.Message);
                return false;
            }
        }

        private void GameEngine_Closed(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                EndGame();

                PlatformManager.Instance.ShowMessage("Соединение с демонстрационным компьютером было разорвано. Игра прекращена.", false);
            }, System.Threading.CancellationToken.None, TaskCreationOptions.None, UI.Scheduler);
        }

        private void Disconnect()
        {
            if (factory != null)
            {
                try
                {
                    factory.Close(TimeSpan.FromSeconds(2.0));
                }
                catch (TimeoutException)
                {
                    factory.Abort();
                }
            }

            factory = null;
        }

        async void Game_RequestStop()
        {
            await RaiseStop();
        }

        public async Task<bool> RaiseStop()
        {
            if (_game == null)
                return true;

            var result = await PlatformManager.Instance.AskStopGame();

            if (!result)
                return false;

            if (_game != null)
                _game.CloseMainView();

            Disconnect();
            EndGame();
            return true;
        }

        /// <summary>
        /// Завершить игру
        /// </summary>
        private void EndGame()
        {
            if (_game != null)
            {
                _game.Error -= ShowError;
                _game.RequestStop -= Game_RequestStop;

                _game.Dispose();

                Game = null;
            }

            Mode = GameMode.Start;
            Transition = ModeTransition.ModeratorToStart;

            if (Settings.UsePlayersKeys == PlayerKeysModes.Web)
                ActivePlayerButtonCommand = _addPlayerButton;
        }

        private async void SelectPackage_Executed(object arg)
        {
            var packageSource = await PlatformManager.Instance.AskSelectPackage(arg);
            if (packageSource != null)
                PackageSource = packageSource;
        }

        private async void SelectLogoFile_Executed(object arg)
        {
            var logoUri = await PlatformManager.Instance.AskSelectLogo();
            if (logoUri != null)
                Settings.SIUISettings.LogoUri = logoUri;
        }

        private async void SelectVideo_Executed(object arg)
        {
            var videoUrl = await PlatformManager.Instance.AskSelectVideo();
            if (videoUrl != null)
                Settings.VideoUrl = videoUrl;
        }

        private void SelectLogsFolder_Executed(object arg)
        {
            var folder = PlatformManager.Instance.AskSelectLogsFolder();
            if (folder != null)
                Settings.LogsFolder = folder;
        }

        private void Refresh_Executed(object arg)
        {
            _computers = new string[0];
            OnPropertyChanged(nameof(Computers));

            GetComputers();
        }

        /// <summary>
        /// Разрешить удалённое управление
        /// </summary>
        private void Listen_Executed(object arg)
        {
            try
            {
                PlatformManager.Instance.CreateServer(typeof(IRemoteGameUI), Settings.HttpPort, Settings.DemoScreenIndex);

                Mode = GameMode.View;
                ActiveListenCommand = _stopListen;
            }
            catch (Exception exc)
            {
                ShowError(exc);

                Mode = GameMode.Start;
                ActiveListenCommand = _listen;
            }
        }

        private void StopListen_Executed(object arg)
        {
            PlatformManager.Instance.CloseServer();

            Mode = GameMode.Start;
            ActiveListenCommand = _listen;
        }

        private void AddPlayerButton_Executed(object arg)
        {
            ActivePlayerButtonCommand = _setPlayerButton;

            _lockPlayerButtonSync = true;

            try
            {
                if (Settings.UsePlayersKeys == PlayerKeysModes.Joystick || Settings.UsePlayersKeys == PlayerKeysModes.Com)
                {
                    _buttonManager = PlatformManager.Instance.ButtonManagerFactory.Create(Settings);
                    _buttonManager.KeyPressed += OnPlayerKeyPressed;
                    if (!_buttonManager.Run())
                    {
                        ActivePlayerButtonCommand = _addPlayerButton;
                        _buttonManager.Dispose();
                        _buttonManager = null;
                    }
                }
            }
            finally
            {
                _lockPlayerButtonSync = false;
            }
        }

        internal bool OnPlayerKeyPressed(GameKey key)
        {
            // Задание кнопки для игрока (в настройках)
            // Может быть не только при this.engine.stage == GameStage.Before, но и в процессе игры
            if (_activePlayerButtonCommand == _setPlayerButton)
            {
                if (Settings.UsePlayersKeys == PlayerKeysModes.Joystick)
                {
                    ProcessNewPlayerButton(key);

                    _buttonManager.Stop();
                    _buttonManager.Dispose();
                    _buttonManager = null;
                }
            }

            return false;
        }

        public bool OnKeyPressed(GameKey key)
        {
            // Задание кнопки для игрока (в настройках)
            if (_activePlayerButtonCommand == _setPlayerButton && Settings.UsePlayersKeys == PlayerKeysModes.Keyboard)
            {
                return ProcessNewPlayerButton(key);
            }

            return false;
        }

        private bool ProcessNewPlayerButton(GameKey key)
        {
            if (!PlatformManager.Instance.IsEscapeKey(key) && !Settings.PlayerKeys2.Contains(key))
            {
                Settings.PlayerKeys2.Add(key);
                UpdateCanAddPlayerButton();
                ActivePlayerButtonCommand = _addPlayerButton;
                return true;
            }

            ActivePlayerButtonCommand = _addPlayerButton;
            return false;
        }

        public void OnButtonsLeft()
        {
            if (!_lockPlayerButtonSync)
            {
                if (_activePlayerButtonCommand == _setPlayerButton)
                {
                    ActivePlayerButtonCommand = _addPlayerButton;

                    if (_mode == GameMode.Start && (Settings.UsePlayersKeys == PlayerKeysModes.Joystick || Settings.UsePlayersKeys == PlayerKeysModes.Com) && _buttonManager != null)
                    {
                        _buttonManager.Stop();
                        _buttonManager.Dispose();
                        _buttonManager = null;
                    }
                }
            }
        }

        private void RemovePlayerButton_Executed(object arg)
        {
            var key = (GameKey)arg;
            if (Settings.PlayerKeys2.Contains(key))
            {
                Settings.PlayerKeys2.Remove(key);
                UpdateCanAddPlayerButton();
            }
        }

        private void UpdateCanAddPlayerButton()
        {
            _addPlayerButton.CanBeExecuted = Settings.PlayerKeys2.Count < MaxNumberOfButtons;
        }

        private void SetPlayerButton_Executed(object arg)
        {
            // Ничего не делаем; команда активируется нажатием клавиши
        }

        private async void GetComputers()
        {
            if (!_isRemoteControlling)
                return;

            _refresh.CanBeExecuted = false;

            try
            {
                _computers = await Task.Factory.StartNew(PlatformManager.Instance.GetLocalComputers);
                OnPropertyChanged(nameof(Computers));
            }
            catch (Exception exc)
            {
                ShowError(exc);
            }
            finally
            {
                _refresh.CanBeExecuted = true;
            }
        }

        private void ShowError(string error)
        {
            PlatformManager.Instance.ShowMessage(error);
        }

        /// <summary>
        /// Вывести сообщение об ошибке
        /// </summary>
        /// <param name="exc"></param>
        private void ShowError(Exception exc)
        {
            ShowError(string.Format("Ошибка: {0}", exc.Message));
        }

        private void UpdateStartCommand()
        {
            _start.CanBeExecuted = (_mode == GameMode.Start) && _packageSource != null &&
                (!_isRemoteControlling || !string.IsNullOrWhiteSpace(Settings.RemotePCName));
        }

        private void OnModeChanged()
        {
            OnPropertyChanged(nameof(CanSelectScreens));
            _selectPackage.CanBeExecuted = _selectVideo.CanBeExecuted = _selectLogsFolder.CanBeExecuted = Mode == GameMode.Start;

            UpdateStartCommand();

            UpdatePlayersView();
        }

        private void UpdatePlayersView()
        {
            if (Settings.PlayersView == PlayersViewMode.Separate && _mode == GameMode.Moderator)
            {
                PlatformManager.Instance.CreatePlayersView(_game);
            }
            else
            {
                PlatformManager.Instance.ClosePlayersView();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
