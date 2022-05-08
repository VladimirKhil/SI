using SICore;
using SICore.Network.Servers;
using SIGame.ViewModel.Properties;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SIGame.ViewModel
{
    public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        public const string MainMenuSound = "main_menu";

        public ICommand NewGame { get; private set; }
        public IAsyncCommand Open { get; private set; }
        public ICommand NetworkGame { get; private set; }
        public ICommand BestPlayers { get; private set; }
        public ICommand About { get; private set; }

        public ICommand SetProfile { get; private set; }

        public HumanPlayerViewModel Human { get; private set; }

        private object _activeView = new IntroViewModel();

        public object ActiveView
        {
            get => _activeView;
            set
            {
                if (_activeView != value)
                {
                    if (_activeView is IDisposable disposable)
                    {
                        try
                        {
                            disposable.Dispose();
                        }
                        catch (Exception exc)
                        {
                            PlatformSpecific.PlatformManager.Instance.ShowMessage(exc.Message, PlatformSpecific.MessageType.Warning, true);
                        }
                    }
                    else if (_activeView is IAsyncDisposable asyncDisposable)
                    {
                        DisposeAsync(asyncDisposable);
                    }

                    _activeView = value;
                    OnPropertyChanged();
                    MainMenu.IsVisible = true;
                }
            }
        }

        private async void DisposeAsync(IAsyncDisposable asyncDisposable)
        {
            try
            {
                await asyncDisposable.DisposeAsync();
            }
            catch (Exception exc)
            {
                PlatformSpecific.PlatformManager.Instance.ShowMessage(exc.Message, PlatformSpecific.MessageType.Warning, true);
            }
        }

        public MainMenuViewModel MainMenu { get; private set; }

        public AppSettingsViewModel Settings { get; private set; }

        private ICommand _update;

        public ICommand Update
        {
            get => _update;
            set
            {
                _update = value;
                OnPropertyChanged();
            }
        }

        public ICommand CancelUpdate { get; set; }
        public CustomCommand Cancel { get; private set; }

        private readonly StartMenuViewModel _startMenu = new StartMenuViewModel();

        private bool _isSlideMenuOpen;

        public bool IsSlideMenuOpen
        {
            get => _isSlideMenuOpen;
            set { if (_isSlideMenuOpen != value) { _isSlideMenuOpen = value; OnPropertyChanged(); } }
        }

        public ICommand ShowSlideMenu { get; private set; }

        private string _startMenuPage;

        public string StartMenuPage
        {
            get => _startMenuPage;
            set { if (_startMenuPage != value) { _startMenuPage = value; OnPropertyChanged(); } }
        }

        private readonly CommonSettings _commonSettings;
        private readonly UserSettings _userSettings;

        public MainViewModel(CommonSettings commonSettings, UserSettings userSettings)
        {
            _commonSettings = commonSettings;
            _userSettings = userSettings;

            Human = new HumanPlayerViewModel(userSettings.GameSettings, commonSettings);
            Settings = new AppSettingsViewModel(userSettings.GameSettings.AppSettings);

            MainMenu = new MainMenuViewModel(userSettings) { IsVisible = false };

            BackLink.Default = new BackLink(Settings, userSettings);

            Cancel = new CustomCommand(Cancel_Executed);

            ShowMenu();

            NewGame = new CustomCommand(New_Executed);
            Open = new AsyncCommand(OnlineGame_Executed);
            NetworkGame = new CustomCommand(NetworkGame_Executed);
            BestPlayers = new CustomCommand(Best_Executed);
            About = new CustomCommand(About_Executed);

            SetProfile = new CustomCommand(SetProfile_Executed);

            _startMenu.MainCommands.Add(new UICommand { Header = Resources.MainMenu_SingleGame.ToUpper(), Command = NewGame });
            _startMenu.MainCommands.Add(new UICommand { Header = Resources.MainMenu_OnlineGame.ToUpper(), Command = Open });
            _startMenu.MainCommands.Add(new UICommand { Header = Resources.MainMenu_NetworkGame.ToUpper(), Command = NetworkGame });
            _startMenu.MainCommands.Add(new UICommand { Header = Resources.MainMenu_BestPlayers.ToUpper(), Command = BestPlayers });
            _startMenu.MainCommands.Add(new UICommand { Header = Resources.MainMenu_About.ToUpper(), Command = About });
            _startMenu.MainCommands.Add(new UICommand { Header = Resources.MainMenu_Exit.ToUpper(), Command = PlatformSpecific.PlatformManager.Instance.Close });

            _startMenu.Human = Human;

            _startMenu.SetProfile = SetProfile;

            CancelUpdate = new CustomCommand(obj => Update = null);

            Human.NewAccountCreating += HumanPlayer_NewAccountCreating;
            Human.NewAccountCreated += HumanPlayer_NewAccountCreated;
            Human.AccountEditing += Human_AccountEditing;

            ShowSlideMenu = new CustomCommand(ShowSlideMenu_Executed);
        }

        private void ShowSlideMenu_Executed(object arg)
        {
            StartMenuPage = ((arg as string) ?? "MainMenuView") + ".xaml";
            IsSlideMenuOpen = true;
        }

        private void SetProfile_Executed(object arg)
        {
            Human.HumanPlayer = (HumanAccount)arg;
        }

        private async void Cancel_Executed(object arg)
        {
            await Task.Delay(300);
            ActiveView = _startMenu;
        }

        private void HumanPlayer_NewAccountCreated()
        {
            ActiveView = _startMenu;
        }

        private void HumanPlayer_NewAccountCreating()
        {
            ActiveView = new ContentBox
            {
                Data = Human,
                Title = Resources.NewAccount,
                Cancel = new CustomCommand(arg =>
                {
                    Human.NewAccount = null;
                    Human.HumanPlayer = _commonSettings.Humans2.Last();

                    Cancel_Executed(arg);
                })
                {
                    CanBeExecuted = _commonSettings.Humans2.Any()
                }
            };
        }

        private void Human_AccountEditing() =>
            ActiveView = new ContentBox
            {
                Data = Human,
                Title = Resources.ChangeAccount,
                Cancel = new CustomCommand(
                    arg =>
                    {
                        Human.NewAccount = null;
                        Cancel_Executed(arg);
                    })
            };

        private void StartGame(
            Server server,
            IViewerClient host,
            bool isNetworkGame,
            bool isOnline,
            string tempDocFolder,
            int networkGamePort = -1)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            var game = new GameViewModel(server, host, _userSettings)
            {
                NetworkGame = isNetworkGame && server.IsMain,
                NetworkGamePort = networkGamePort,
                IsOnline = isOnline,
                TempDocFolder = tempDocFolder
            };

            game.GameEnded += EndGame_Executed;

            Settings.IsEditable = false;
            ActiveView = game;

            PlatformSpecific.PlatformManager.Instance.PlaySound();

            game.Init();

            host.MyLogic.PrintGreeting();
        }

        private void JoinGame(Server server, IViewerClient host, bool isOnline) => StartGame(server, host, false, isOnline, null);

        private async void EndGame_Executed()
        {
            if (_activeView is GameViewModel game && game.IsOnline)
            {
                await OnlineGame_Executed(null);
            }
            else
            {
                ActiveView = _startMenu;
            }

            Settings.IsEditable = true;
            PlayMainMenuSound();
        }

        private async void New_Executed(object arg)
        {
            await Task.Delay(500);

            var gameSettings = new GameSettingsViewModel(_userSettings.GameSettings, _commonSettings, _userSettings) { Human = Human.HumanPlayer, ChangeSettings = ShowSlideMenu };

            gameSettings.StartGame += StartGame;

            gameSettings.PrepareForGame();

            var contentBox = new ContentBox { Data = gameSettings, Title = Resources.NewGame };
            var navigator = new NavigatorViewModel
            {
                Content = contentBox,
                Cancel = Cancel
            };

            ActiveView = navigator;
        }

        private async Task OnlineGame_Executed(object arg)
        {
            await Task.Delay(300);

            var login = new LoginViewModel(_userSettings) { Login = Human.HumanPlayer.Name };
            login.Entered += async (login, client) =>
            {
                var humanAccount = new HumanAccount(Human.HumanPlayer)
                {
                    Name = login.Trim()
                };

                var onlineConnection = new SIOnlineViewModel(_userSettings.ConnectionData, client, _commonSettings, _userSettings)
                {
                    Human = humanAccount,
                    Cancel = Cancel,
                    ChangeSettings = ShowSlideMenu
                };

                onlineConnection.Ready += JoinGame;
                onlineConnection.StartGame += StartGame;

                ActiveView = onlineConnection;
                await onlineConnection.InitAsync();
            };

            ActiveView = new ContentBox { Data = login, Cancel = Cancel };

            await login.Enter.ExecuteAsync(null);
        }

        private async void NetworkGame_Executed(object arg)
        {
            var networkConnection = new SINetworkViewModel(_userSettings.ConnectionData, _commonSettings, _userSettings) { Human = Human.HumanPlayer, ChangeSettings = ShowSlideMenu, Cancel = Cancel };
            networkConnection.Ready += JoinGame;
            networkConnection.StartGame += StartGame;

            await Task.Delay(500);
            ActiveView = networkConnection;
        }

        private async void Best_Executed(object arg)
        {
            await Task.Delay(500);
            ActiveView = new ContentBox { Data = new BestPlayersViewModel(), Title = Resources.MainMenu_BestPlayers, Cancel = Cancel };
        }

        private async void About_Executed(object arg)
        {
            await Task.Delay(500);
            ActiveView = new ContentBox { Data = new AboutViewModel(), Title = Resources.MainMenu_About, Cancel = Cancel };
        }

        /// <summary>
        /// Изменилось значение свойства
        /// </summary>
        /// <param name="name">Имя свойства</param>
        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Version UpdateVersion { get; set; }

        public string UpdateVersionMessage => string.Format(Resources.UpdateVersionMessage, UpdateVersion);

        public void ShowMenu()
        {
            if (Human.NewAccount != null)
            {
                HumanPlayer_NewAccountCreating();
            }
            else
            {
                ActiveView = _startMenu;
                PlayMainMenuSound();
            }
        }

        private void PlayMainMenuSound()
        {
            if (_userSettings.MainMenuSound)
            {
                PlatformSpecific.PlatformManager.Instance.PlaySound(MainMenuSound, loop: true);
            }
        }

        public void Dispose()
        {
            if (_activeView is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
