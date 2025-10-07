using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SI.GameServer.Client;
using SI.GameServer.Contract;
using SIContentService.Client;
using SIGame.ViewModel.PlatformSpecific;
using SIGame.ViewModel.Properties;
using SIGame.ViewModel.Settings;
using SIStatisticsService.Contract;
using SIStorage.Service.Client;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Utils;
using Utils.Commands;

namespace SIGame.ViewModel;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    public const string MainMenuSound = "main_menu";

    public static bool IsSupportedOS => Environment.OSVersion.Version >= new Version(6, 2);

    public const string SIGameOnlineUrl = "https://sigame.vladimirkhil.com";

    public ICommand NewGame { get; private set; }

    public IAsyncCommand Open { get; private set; }

    public ICommand NetworkGame { get; private set; }

    public ICommand BestPlayers { get; private set; }

    public ICommand About { get; private set; }

    public ICommand SetProfile { get; private set; }

    public HumanPlayerViewModel Human { get; private set; }

    private object? _activeView = new IntroViewModel();

    public object? ActiveView
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
                        PlatformManager.Instance.ShowMessage(exc.Message, MessageType.Warning, true);
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

    private static async void DisposeAsync(IAsyncDisposable asyncDisposable)
    {
        try
        {
            await asyncDisposable.DisposeAsync();
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowMessage(exc.Message, MessageType.Warning, true);
        }
    }

    public MainMenuViewModel MainMenu { get; private set; }

    public AppSettingsViewModel Settings { get; private set; }

    public ICommand Cancel { get; private set; }

    private readonly StartMenuViewModel _startMenu;

    public StartMenuViewModel StartMenu => _startMenu;

    private bool _isSlideMenuOpen;

    public bool IsSlideMenuOpen
    {
        get => _isSlideMenuOpen;
        set { if (_isSlideMenuOpen != value) { _isSlideMenuOpen = value; OnPropertyChanged(); } }
    }

    public ICommand ShowSlideMenu { get; private set; }

    public ICommand CloseSlideMenu { get; private set; }    

    private string _startMenuPage = "MainMenuView.xaml";

    public string StartMenuPage
    {
        get => _startMenuPage;
        set { if (_startMenuPage != value) { _startMenuPage = value; OnPropertyChanged(); } }
    }

    private readonly CommonSettings _commonSettings;
    private readonly UserSettings _userSettings;
    private readonly IServiceProvider _serviceProvider;

    public MainViewModel(CommonSettings commonSettings, UserSettings userSettings, AppState appState, IServiceProvider serviceProvider)
    {
        _commonSettings = commonSettings;
        _userSettings = userSettings;
        _serviceProvider = serviceProvider;

        Human = new HumanPlayerViewModel(userSettings.GameSettings, commonSettings);
        Settings = new AppSettingsViewModel(userSettings.GameSettings.AppSettings);

        MainMenu = new MainMenuViewModel(userSettings) { IsVisible = false };

        BackLink.Default = new BackLink(appState, serviceProvider);

        Cancel = new SimpleCommand(Cancel_Executed);

        NewGame = new SimpleCommand(New_Executed);
        Open = new AsyncCommand(OnlineGame_Executed);
        NetworkGame = new SimpleCommand(NetworkGame_Executed);
        BestPlayers = new SimpleCommand(Best_Executed);
        About = new SimpleCommand(About_Executed);

        SetProfile = new SimpleCommand(SetProfile_Executed);

        _startMenu = new StartMenuViewModel(Human, SetProfile);

        _startMenu.MainCommands.Add(new UICommand { Header = Resources.MainMenu_SingleGame.ToUpper(), Command = NewGame });
        _startMenu.MainCommands.Add(new UICommand { Header = Resources.MainMenu_OnlineGame.ToUpper(), Command = Open });
        _startMenu.MainCommands.Add(new UICommand { Header = Resources.MainMenu_NetworkGame.ToUpper(), Command = NetworkGame });
        _startMenu.MainCommands.Add(new UICommand { Header = Resources.MainMenu_BestPlayers.ToUpper(), Command = BestPlayers });
        _startMenu.MainCommands.Add(new UICommand { Header = Resources.MainMenu_About.ToUpper(), Command = About });
        _startMenu.MainCommands.Add(new UICommand { Header = Resources.MainMenu_Exit.ToUpper(), Command = PlatformManager.Instance.Close });

        ShowMenu();

        Human.NewAccountCreating += HumanPlayer_NewAccountCreating;
        Human.NewAccountCreated += HumanPlayer_NewAccountCreated;
        Human.AccountEditing += Human_AccountEditing;

        ShowSlideMenu = new SimpleCommand(ShowSlideMenu_Executed);
        CloseSlideMenu = new SimpleCommand(CloseSlideMenu_Executed);
    }

    private void ShowSlideMenu_Executed(object? arg)
    {
        StartMenuPage = ((arg as string) ?? "MainMenuView") + ".xaml";
        IsSlideMenuOpen = true;
    }

    private void CloseSlideMenu_Executed(object? arg) => IsSlideMenuOpen = false;

    private void SetProfile_Executed(object? arg) => Human.HumanPlayer = (HumanAccount)arg;

    private async void Cancel_Executed(object? arg)
    {
        await Task.Delay(300);
        ActiveView = _startMenu;
    }

    private void HumanPlayer_NewAccountCreated() => ActiveView = _startMenu;

    private void HumanPlayer_NewAccountCreating() =>
        ActiveView = new ContentBox
        {
            Data = Human,
            Title = Resources.NewAccount,
            Cancel = new SimpleCommand(arg =>
            {
                Human.NewAccount = null;
                Human.HumanPlayer = _commonSettings.Humans2.Last();

                Cancel_Executed(arg);
            })
            {
                CanBeExecuted = _commonSettings.Humans2.Any()
            }
        };

    private void Human_AccountEditing() =>
        ActiveView = new ContentBox
        {
            Data = Human,
            Title = Resources.ChangeAccount,
            Cancel = new SimpleCommand(
                arg =>
                {
                    Human.NewAccount = null;
                    Cancel_Executed(arg);
                })
        };

    private void StartGame(GameViewModel gameViewModel)
    {
        gameViewModel.GameEnded += EndGame_Executed;

        Settings.IsEditable = false;
        ActiveView = gameViewModel;

        PlatformManager.Instance.PlaySound();

        gameViewModel.Init();
    }

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

    private async void New_Executed(object? arg)
    {
        await Task.Delay(500);

        var siStorageClientFactory = PlatformManager.Instance.ServiceProvider!.GetRequiredService<ISIStorageClientFactory>();
        var siStorageClientOptions = PlatformManager.Instance.ServiceProvider!.GetRequiredService<IOptions<SIStorageClientOptions>>().Value;
        var loggerFactory = PlatformManager.Instance.ServiceProvider!.GetRequiredService<ILoggerFactory>();

        var libraries = new SIStorageInfo[]
        {
            new()
            {
                ServiceUri = siStorageClientOptions.ServiceUri,
                Name = Resources.QuestionLibrary,
                RandomPackagesSupported = true,
                IdentifiersSupported = true
            }
        };

        var gameSettings = new GameSettingsViewModel(
            _userSettings.GameSettings,
            _commonSettings,
            _userSettings,
            Settings.ThemeSettings.SIUISettings,
            siStorageClientFactory,
            libraries,
            loggerFactory)
        {
            Human = Human.HumanPlayer,
            ChangeSettings = ShowSlideMenu
        };

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

    private async Task OnlineGame_Executed(object? arg)
    {
        await Task.Delay(300);

        if (!IsSupportedOS)
        {
            Browser.Open(SIGameOnlineUrl);
            return;
        }

        var login = new LoginViewModel(_serviceProvider.GetRequiredService<IGameServerClientFactory>()) { Login = Human.HumanPlayer.Name };

        login.Entered += async (login, client) =>
        {
            var humanAccount = new HumanAccount(Human.HumanPlayer)
            {
                Name = login.Trim()
            };

            var onlineViewModel = new SIOnlineViewModel(
                _userSettings.ConnectionData,
                client,
                _commonSettings,
                _userSettings,
                Settings.ThemeSettings.SIUISettings,
                _serviceProvider.GetRequiredService<ISIStatisticsServiceClient>(),
                _serviceProvider.GetRequiredService<IOptions<SIContentClientOptions>>().Value,
                _serviceProvider.GetRequiredService<ILogger<SIOnlineViewModel>>(),
                Cancel)
            {
                Human = humanAccount,
                ChangeSettings = ShowSlideMenu
            };

            onlineViewModel.Ready += StartGame;
            onlineViewModel.StartGame += StartGame;

            ActiveView = onlineViewModel;
            await onlineViewModel.InitAsync();
        };

        ActiveView = new ContentBox { Data = login, Cancel = Cancel };

        await login.Enter.ExecuteAsync(null);
    }

    private async void NetworkGame_Executed(object? arg)
    {
        var networkConnection = new SINetworkViewModel(
            _userSettings.ConnectionData,
            _commonSettings,
            _userSettings,
            Settings.ThemeSettings.SIUISettings)
        {
            Human = Human.HumanPlayer, 
            ChangeSettings = ShowSlideMenu,
            Cancel = Cancel
        };
        
        networkConnection.Ready += StartGame;
        networkConnection.StartGame += StartGame;

        await Task.Delay(500);
        ActiveView = networkConnection;
    }

    private async void Best_Executed(object? arg)
    {
        await Task.Delay(500);
        ActiveView = new ContentBox { Data = new BestPlayersViewModel(), Title = Resources.MainMenu_BestPlayers, Cancel = Cancel };
    }

    private async void About_Executed(object? arg)
    {
        await Task.Delay(500);
        ActiveView = new ContentBox { Data = new AboutViewModel(), Title = Resources.MainMenu_About, Cancel = Cancel };
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public event PropertyChangedEventHandler? PropertyChanged;

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
            PlatformManager.Instance.PlaySound(MainMenuSound, loop: true);
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
