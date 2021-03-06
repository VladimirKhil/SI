using SIEngine;
using SImulator.ViewModel.Model;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel.Core;
using SImulator.ViewModel.PlatformSpecific;
using SImulator.ViewModel.Properties;
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
using System.IO;
using System.Diagnostics;

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
        private readonly SimpleCommand _selectBackgroundImageFile;
        private readonly SimpleCommand _selectBackgroundVideoFile;
        private readonly SimpleCommand _selectLogsFolder;
        private readonly SimpleCommand _selectAudioFile;

        private readonly SimpleUICommand _listen;
        private readonly SimpleUICommand _stopListen;

        private readonly SimpleUICommand _addPlayerButton;
        private readonly SimpleUICommand _setPlayerButton;
        private readonly SimpleCommand _removePlayerButton;

        private readonly SimpleCommand _refresh;

        public ICommand Start => _start;

        public ICommand SelectPackage => _selectPackage;
        public ICommand SelectVideoFile => _selectVideo;
        public ICommand SelectBackgroundImageFile => _selectBackgroundImageFile;
        public ICommand SelectBackgroundVideoFile => _selectBackgroundVideoFile;
        public ICommand SelectLogoFile { get; private set; }
        public ICommand SelectLogsFolder => _selectLogsFolder;
        public ICommand SelectAudioFile => _selectAudioFile;

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

        public ICommand AddRight { get; private set; }

        public ICommand AddWrong { get; private set; }

        public ICommand OpenLicensesFolder { get; private set; }

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
            get => _game;
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
            Settings = settings;
            SettingsViewModel = new AppSettingsViewModel(Settings);

            _start = new SimpleUICommand(Start_Executed) { Name = Resources.StartGame };

            _selectPackage = new SimpleCommand(SelectPackage_Executed);
            SelectLogoFile = new SimpleCommand(SelectLogoFile_Executed);
            _selectVideo = new SimpleCommand(SelectVideo_Executed);
            _selectBackgroundImageFile = new SimpleCommand(SelectBackgroundImageFile_Executed);
            _selectBackgroundVideoFile = new SimpleCommand(SelectBackgroundVideoFile_Executed);
            _selectLogsFolder = new SimpleCommand(SelectLogsFolder_Executed);
            _selectAudioFile = new SimpleCommand(SelectAudioFile_Executed);

            _refresh = new SimpleCommand(Refresh_Executed);

            _listen = new SimpleUICommand(Listen_Executed) { Name = Resources.ListenForConnections };
            _stopListen = new SimpleUICommand(StopListen_Executed) { Name = Resources.StopListen };

            _addPlayerButton = new SimpleUICommand(AddPlayerButton_Executed) { Name = Resources.Add };
            _setPlayerButton = new SimpleUICommand(SetPlayerButton_Executed) { Name = Resources.PressTheButton };
            _removePlayerButton = new SimpleCommand(RemovePlayerButton_Executed);

            NavigateToSite = new SimpleCommand(NavigateToSite_Executed);
            SelectColor = new SimpleCommand(SelectColor_Executed);

            AddPlayer = new SimpleCommand(AddPlayer_Executed);
            RemovePlayer = new SimpleCommand(RemovePlayer_Executed);

            AddRight = new SimpleCommand(AddRight_Executed);
            AddWrong = new SimpleCommand(AddWrong_Executed);

            OpenLicensesFolder = new SimpleCommand(OpenLicensesFolder_Executed);

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
            Settings.ScreenNumber = Math.Max(0, Screens.Length - 2);
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
            {
                return;
            }

            Players.Remove(player);
        }

        private void AddRight_Executed(object arg)
        {
            _game?.AddRight.Execute(null);
        }

        private void AddWrong_Executed(object arg)
        {
            _game?.AddWrong.Execute(null);
        }

        private void OpenLicensesFolder_Executed(object arg)
        {
            var licensesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "licenses");
            if (!Directory.Exists(licensesFolder))
            {
                PlatformManager.Instance.ShowMessage(Resources.NoLicensesFolder);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(licensesFolder));
            }
            catch (Exception exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format(Resources.OpenLicensesError, exc.Message), true);
            }
        }

        private void NavigateToSite_Executed(object arg) => PlatformManager.Instance.NavigateToSite();

        private void SelectColor_Executed(object arg)
        {
            if (!int.TryParse(arg?.ToString(), out var colorMode) || colorMode < 0 || colorMode > 3)
            {
                return;
            }

            var color = PlatformManager.Instance.AskSelectColor();
            if (color == null)
            {
                return;
            }

            var settings = Settings.SIUISettings;
            switch (colorMode)
            {
                case 0:
                    settings.TableColorString = color;
                    break;
                case 1:
                    settings.TableBackColorString = color;
                    break;
                case 2:
                    settings.TableGridColorString = color;
                    break;
                case 3:
                    settings.AnswererColorString = color;
                    break;
            }
        }

        private void MyDefault_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AppSettings.IsRemoteControlAllowed):
                    if (_computers == null)
                    {
                        GetComputers();
                    }

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
                    throw new Exception(string.Format(Resources.GamePackageLoadError, exc.Message));
                }

                var gameHost = PlatformManager.Instance.CreateGameHost(engine);

                IRemoteGameUI ui;
                if (_isRemoteControlling)
                {
                    if (!Connect(gameHost, out ui))
                    {
                        return;
                    }
                }
                else
                {
                    var remoteGameUI = new RemoteGameUI
                    {
                        GameHost = gameHost,
                        ScreenIndex = SettingsViewModel.Model.ScreenNumber
                    };

                    ui = remoteGameUI;
                    ui.UpdateSettings(SettingsViewModel.SIUISettings.Model);
                    remoteGameUI.OnError += ShowError;
                }

                var game = new GameEngine(SettingsViewModel, engine, gameHost, ui, Players, _isRemoteControlling);
                Game = game;

                game.Start();

                game.Error += ShowError;
                game.RequestStop += Game_RequestStop;

                var recent = Settings.Recent;
                if (!string.IsNullOrEmpty(_packageSource.Token) && !recent.Contains(_packageSource.Token))
                {
                    recent.Insert(0, _packageSource.Token);
                    if (recent.Count > 10)
                    {
                        recent.RemoveAt(10);
                    }
                }

                Mode = GameMode.Moderator;
            }
            catch (Exception exc)
            {
                var reason = exc.InnerException ?? exc;

                PlatformManager.Instance.ShowMessage(string.Format(Resources.GameStartError, reason.Message), false);
                if (_game != null)
                {
                    _game.CloseMainView();
                }

                EndGame();
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

        private void GameEngine_Closed(object sender, EventArgs e) =>
            Task.Factory.StartNew(
                () =>
                {
                    EndGame();
                    PlatformManager.Instance.ShowMessage(Resources.GameEndsBecauseOfConnectionLoss, false);
                },
                System.Threading.CancellationToken.None,
                TaskCreationOptions.None,
                UI.Scheduler);

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
                catch (CommunicationObjectFaultedException)
                {
                    factory.Abort();
                }
            }

            factory = null;
        }

        private async void Game_RequestStop() => await RaiseStop();

        public async Task<bool> RaiseStop()
        {
            if (_game == null)
            {
                return true;
            }

            var result = await PlatformManager.Instance.AskStopGameAsync();

            if (!result)
            {
                return false;
            }

            if (_game != null)
            {
                _game.CloseMainView();
            }

            Disconnect();
            EndGame();

            return true;
        }

        /// <summary>
        /// Ends the game.
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
            {
                ActivePlayerButtonCommand = _addPlayerButton;
            }
        }

        private async void SelectPackage_Executed(object arg)
        {
            var packageSource = await PlatformManager.Instance.AskSelectPackageAsync(arg);
            if (packageSource != null)
            {
                PackageSource = packageSource;
            }
        }

        private async void SelectLogoFile_Executed(object arg)
        {
            var logoUri = await PlatformManager.Instance.AskSelectFileAsync(Resources.SelectLogoImage);
            if (logoUri != null)
            {
                Settings.SIUISettings.LogoUri = logoUri;
            }
        }

        private async void SelectVideo_Executed(object arg)
        {
            var videoUrl = await PlatformManager.Instance.AskSelectFileAsync(Resources.SelectIntroVideo);
            if (videoUrl != null)
            {
                Settings.VideoUrl = videoUrl;
            }
        }

        private async void SelectBackgroundImageFile_Executed(object arg)
        {
            var imageUrl = await PlatformManager.Instance.AskSelectFileAsync(Resources.SelectBackgroundImage);
            if (imageUrl != null)
            {
                Settings.SIUISettings.BackgroundImageUri = imageUrl;
            }
        }

        private async void SelectBackgroundVideoFile_Executed(object arg)
        {
            var videoUrl = await PlatformManager.Instance.AskSelectFileAsync(Resources.SelectBackgroundVideo);
            if (videoUrl != null)
            {
                Settings.SIUISettings.BackgroundVideoUri = videoUrl;
            }
        }

        private void SelectLogsFolder_Executed(object arg)
        {
            var folder = PlatformManager.Instance.AskSelectLogsFolder();
            if (folder != null)
            {
                Settings.LogsFolder = folder;
            }
        }

        private async void SelectAudioFile_Executed(object arg)
        {
            if (!int.TryParse(arg?.ToString(), out var fileId))
            {
                return;
            }

            var fileUri = await PlatformManager.Instance.AskSelectFileAsync(Resources.SelectAudioFile);
            if (fileUri == null)
            {
                return;
            }

            switch (fileId)
            {
                case 0:
                    Settings.Sounds.BeginGame = fileUri;
                    break;

                case 1:
                    Settings.Sounds.GameThemes = fileUri;
                    break;

                case 2:
                    Settings.Sounds.QuestionSelected = fileUri;
                    break;

                case 3:
                    Settings.Sounds.PlayerPressed = fileUri;
                    break;

                case 4:
                    Settings.Sounds.SecretQuestion = fileUri;
                    break;

                case 5:
                    Settings.Sounds.StakeQuestion = fileUri;
                    break;

                case 6:
                    Settings.Sounds.NoRiskQuestion = fileUri;
                    break;

                case 7:
                    Settings.Sounds.AnswerRight = fileUri;
                    break;

                case 8:
                    Settings.Sounds.AnswerWrong = fileUri;
                    break;

                case 9:
                    Settings.Sounds.NoAnswer = fileUri;
                    break;

                case 10:
                    Settings.Sounds.RoundBegin = fileUri;
                    break;

                case 11:
                    Settings.Sounds.RoundThemes = fileUri;
                    break;

                case 12:
                    Settings.Sounds.RoundTimeout = fileUri;
                    break;

                case 13:
                    Settings.Sounds.FinalDelete = fileUri;
                    break;

                case 14:
                    Settings.Sounds.FinalThink = fileUri;
                    break;
            }
        }

        private void Refresh_Executed(object arg)
        {
            _computers = Array.Empty<string>();
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
            // Do nothing; the command is activated by key press
        }

        private async void GetComputers()
        {
            if (!_isRemoteControlling)
                return;

            _refresh.CanBeExecuted = false;

            try
            {
                _computers = await Task.Run(PlatformManager.Instance.GetLocalComputers);
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
            ShowError($"{Resources.Error}: {exc.Message}");
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
