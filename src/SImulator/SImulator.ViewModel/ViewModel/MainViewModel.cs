using SIEngine;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel.Controllers;
using SImulator.ViewModel.Core;
using SImulator.ViewModel.Listeners;
using SImulator.ViewModel.Model;
using SImulator.ViewModel.PlatformSpecific;
using SImulator.ViewModel.Properties;
using SIPackages;
using SIUI.ViewModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Utils.Commands;

namespace SImulator.ViewModel;

public sealed class MainViewModel : INotifyPropertyChanged, IButtonManagerListener, IDisposable
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

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>
    /// Менеджер игровых кнопок
    /// </summary>
    private IButtonManager? _buttonManager;

    private readonly AsyncCommand _start;

    private readonly SimpleCommand _selectPackage;
    private readonly SimpleCommand _selectVideo;
    private readonly SimpleCommand _selectBackgroundImageFile;
    private readonly SimpleCommand _selectBackgroundVideoFile;
    private readonly SimpleCommand _selectLogsFolder;
    private readonly SimpleCommand _selectAudioFile;

    private readonly SimpleUICommand _addPlayerButton;
    private readonly SimpleUICommand _setPlayerButton;
    private readonly SimpleCommand _removePlayerButton;

    public IAsyncCommand Start => _start;

    public ICommand SelectPackage => _selectPackage;
    public ICommand SelectVideoFile => _selectVideo;
    public ICommand SelectBackgroundImageFile => _selectBackgroundImageFile;
    public ICommand SelectBackgroundVideoFile => _selectBackgroundVideoFile;
    public ICommand SelectLogoFile { get; private set; }
    public ICommand SelectLogsFolder => _selectLogsFolder;
    public ICommand SelectAudioFile => _selectAudioFile;

    public ICommand DeletePlayerKey => _removePlayerButton;

    public ICommand NavigateToSite { get; private set; }

    public ICommand SelectColor { get; private set; }

    public ICommand AddPlayer { get; private set; }

    public ICommand RemovePlayer { get; private set; }

    private ICommand _activePlayerButtonCommand;

    public ICommand ActivePlayerButtonCommand
    {
        get => _activePlayerButtonCommand;
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
        get => _packageSource;
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

    private bool _isStarting;

    public bool IsStarting
    {
        get => _isStarting;
        set { if (_isStarting != value) { _isStarting = value; OnPropertyChanged(); } }
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
                {
                    Settings.ComPort = _comPorts[0];
                }
            }

            return _comPorts;
        }
    }

    private GameViewModel? _game;

    public GameViewModel? Game
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
        get => _mode;
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
        get => _transition;
        set { _transition = value; OnPropertyChanged(); }
    }

    public bool CanSelectScreens => (_mode == GameMode.Start) && Screens.Length > 1;

    public IScreen[] Screens { get; private set; }

    public string Host => "[Ваш IP-адрес]";

    /// <summary>
    /// Список игроков, отображаемых на табло в особом режиме игры
    /// </summary>
    public ObservableCollection<SimplePlayerInfo> Players { get; set; }

    public int ButtonBlockTime => (int)(Settings.BlockingTime * 1000);

    public MainViewModel(AppSettings settings)
    {
        Settings = settings;
        SettingsViewModel = new AppSettingsViewModel(Settings);

        _start = new AsyncCommand(Start_Executed);

        _selectPackage = new SimpleCommand(SelectPackage_Executed);
        SelectLogoFile = new SimpleCommand(SelectLogoFile_Executed);
        _selectVideo = new SimpleCommand(SelectVideo_Executed);
        _selectBackgroundImageFile = new SimpleCommand(SelectBackgroundImageFile_Executed);
        _selectBackgroundVideoFile = new SimpleCommand(SelectBackgroundVideoFile_Executed);
        _selectLogsFolder = new SimpleCommand(SelectLogsFolder_Executed);
        _selectAudioFile = new SimpleCommand(SelectAudioFile_Executed);

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

        ActivePlayerButtonCommand = _addPlayerButton;

        UpdateStartCommand();
        UpdateCanAddPlayerButton();

        Players = new ObservableCollection<SimplePlayerInfo>();

        Screens = PlatformManager.Instance.GetScreens();
        
        var screensLength = Screens.Length;

#if DEBUG
        Settings.ScreenNumber = Math.Max(0, screensLength - 1);
#else
        if (Settings.ScreenNumber == -1 || Settings.ScreenNumber >= screensLength)
        {
            Settings.ScreenNumber = screensLength - 1;
        }
#endif

        Settings.PropertyChanged += MyDefault_PropertyChanged;
    }

    private void AddPlayer_Executed(object? arg)
    {
        var info = new PlayerInfo();
        Players.Add(info);
    }

    private void RemovePlayer_Executed(object? arg)
    {
        if (arg is not SimplePlayerInfo player)
        {
            return;
        }

        Players.Remove(player);
    }

    private void AddRight_Executed(object? arg)
    {
        _game?.AddRight.Execute(null);
    }

    private void AddWrong_Executed(object? arg)
    {
        _game?.AddWrong.Execute(null);
    }

    private void OpenLicensesFolder_Executed(object? arg)
    {
        var licensesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "licenses");

        if (!Directory.Exists(licensesFolder))
        {
            PlatformManager.Instance.ShowMessage(Resources.NoLicensesFolder);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {licensesFolder}") { CreateNoWindow = true });
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowMessage(string.Format(Resources.OpenLicensesError, exc.Message), true);
        }
    }

    private void NavigateToSite_Executed(object? arg) => PlatformManager.Instance.NavigateToSite();

    private void SelectColor_Executed(object? arg)
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

    private void MyDefault_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AppSettings.UsePlayersKeys):
                Settings.PlayerKeys2.Clear();
                UpdateCanAddPlayerButton();
                break;

            case nameof(AppSettings.PlayersView):
                UpdatePlayersView();
                break;
        }
    }

    private async Task<SIDocument> PreparePackageAsync(CancellationToken cancellationToken = default)
    {
        var (filePath, isTemporary) = await _packageSource.GetPackageFileAsync(cancellationToken);

        var tempDir = Path.Combine(Path.GetTempPath(), AppSettings.AppName, Guid.NewGuid().ToString());

        var document = await SIDocument.ExtractToFolderAndLoadAsync(
            filePath,
            tempDir,
            cancellationToken: cancellationToken);

        if (isTemporary)
        {
            File.Delete(filePath);
        }

        return document;
    }

    private EngineOptions GetEngineOptions()
    {
        var options = SettingsViewModel.Model;

        return new()
        {
            IsPressMode = options.FalseStart && options.UsePlayersKeys != PlayerKeysModes.None,
            IsMultimediaPressMode = options.FalseStart && options.FalseStartMultimedia && options.UsePlayersKeys != PlayerKeysModes.None,
            ShowRight = options.ShowRight,
            ShowScore = false,
            AutomaticGame = options.AutomaticGame,
            PlaySpecials = options.PlaySpecials,
            ThinkingTime = options.ThinkingTime
        };
    }

    private ILogger CreateLogger()
    {
        if (Settings.SaveLogs)
        {
            var logsFolder = Settings.LogsFolder;

            if (string.IsNullOrWhiteSpace(logsFolder))
            {
                PlatformManager.Instance.ShowMessage(Resources.LogsFolderNotSetWarning);
                return PlatformManager.Instance.CreateLogger(null);
            }
            else
            {
                try
                {
                    return PlatformManager.Instance.CreateLogger(logsFolder);
                }
                catch (Exception exc)
                {
                    PlatformManager.Instance.ShowMessage(string.Format(Resources.LoggerCreationWarning, exc.Message), false);
                    return PlatformManager.Instance.CreateLogger(null);
                }
            }
        }
        else
        {
            return PlatformManager.Instance.CreateLogger(null);
        }
    }

    /// <summary>
    /// Starts the game.
    /// </summary>
    private async Task Start_Executed(object? _)
    {
        try
        {
            _start.CanBeExecuted = false;
            IsStarting = true;

            var document = await PreparePackageAsync(_cancellationTokenSource.Token);

            ISIEngine engine;

            var gameEngineController = new GameEngineController(document);

            try
            {
                document.Upgrade();

                engine = EngineFactory.CreateEngine(
                    SettingsViewModel.Model.GameMode == GameModes.Tv,
                    document,
                    GetEngineOptions,
                    gameEngineController,
                    gameEngineController);
            }
            catch (Exception exc)
            {
                throw new Exception(string.Format(Resources.GamePackageLoadError, exc.Message));
            }

            var presentationListener = new PresentationListener(engine);

            var presentationController = new PresentationController
            {
                Listener = presentationListener,
                ScreenIndex = SettingsViewModel.Model.ScreenNumber
            };

            presentationController.UpdateSettings(SettingsViewModel.SIUISettings.Model);
            presentationController.UpdateShowPlayers(SettingsViewModel.Model.ShowPlayers);
            presentationController.Error += ShowError;

            var logger = CreateLogger();

            var game = new GameViewModel(
                SettingsViewModel,
                engine,
                presentationListener,
                presentationController,
                Players,
                logger);

            Game = game;

            gameEngineController.GameViewModel = game;

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

            _game?.CloseMainView();

            await EndGameAsync();
            return;
        }
        finally
        {
            _start.CanBeExecuted = true;
            IsStarting = false;
        }
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

        await EndGameAsync();

        return true;
    }

    /// <summary>
    /// Ends the game.
    /// </summary>
    private async Task EndGameAsync()
    {
        if (_game != null)
        {
            _game.Error -= ShowError;
            _game.RequestStop -= Game_RequestStop;

            await _game.DisposeAsync();

            Game = null;
        }

        Mode = GameMode.Start;
        Transition = ModeTransition.ModeratorToStart;

        if (Settings.UsePlayersKeys == PlayerKeysModes.Web)
        {
            ActivePlayerButtonCommand = _addPlayerButton;
        }
    }

    private async void SelectPackage_Executed(object? arg)
    {
        if (arg == null)
        {
            throw new ArgumentNullException(nameof(arg));
        }

        var packageSource = await PlatformManager.Instance.AskSelectPackageAsync(arg);

        if (packageSource != null)
        {
            PackageSource = packageSource;
        }
    }

    private async void SelectLogoFile_Executed(object? arg)
    {
        var logoUri = await PlatformManager.Instance.AskSelectFileAsync(Resources.SelectLogoImage);

        if (logoUri != null)
        {
            Settings.SIUISettings.LogoUri = logoUri;
        }
    }

    private async void SelectVideo_Executed(object? arg)
    {
        var videoUrl = await PlatformManager.Instance.AskSelectFileAsync(Resources.SelectIntroVideo);

        if (videoUrl != null)
        {
            Settings.VideoUrl = videoUrl;
        }
    }

    private async void SelectBackgroundImageFile_Executed(object? arg)
    {
        var imageUrl = await PlatformManager.Instance.AskSelectFileAsync(Resources.SelectBackgroundImage);

        if (imageUrl != null)
        {
            Settings.SIUISettings.BackgroundImageUri = imageUrl;
        }
    }

    private async void SelectBackgroundVideoFile_Executed(object? arg)
    {
        var videoUrl = await PlatformManager.Instance.AskSelectFileAsync(Resources.SelectBackgroundVideo);

        if (videoUrl != null)
        {
            Settings.SIUISettings.BackgroundVideoUri = videoUrl;
        }
    }

    private void SelectLogsFolder_Executed(object? arg)
    {
        var folder = PlatformManager.Instance.AskSelectLogsFolder();

        if (folder != null)
        {
            Settings.LogsFolder = folder;
        }
    }

    private async void SelectAudioFile_Executed(object? arg)
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

    private async void AddPlayerButton_Executed(object? arg)
    {
        ActivePlayerButtonCommand = _setPlayerButton;

        _lockPlayerButtonSync = true;

        try
        {
            if (Settings.UsePlayersKeys == PlayerKeysModes.Joystick || Settings.UsePlayersKeys == PlayerKeysModes.Com)
            {
                _buttonManager = PlatformManager.Instance.ButtonManagerFactory.Create(Settings, this);

                if (_buttonManager == null)
                {
                    PlatformManager.Instance.ShowMessage($"Could not create button manager for mode {Settings.UsePlayersKeys}");
                    return;
                }

                if (!_buttonManager.Start())
                {
                    ActivePlayerButtonCommand = _addPlayerButton;
                    await _buttonManager.DisposeAsync();
                    _buttonManager = null;
                }
            }
        }
        finally
        {
            _lockPlayerButtonSync = false;
        }
    }

    public bool OnKeyboardPressed(GameKey key)
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

    public async Task OnButtonsLeftAsync()
    {
        if (!_lockPlayerButtonSync)
        {
            if (_activePlayerButtonCommand == _setPlayerButton)
            {
                ActivePlayerButtonCommand = _addPlayerButton;

                if (_mode == GameMode.Start && (Settings.UsePlayersKeys == PlayerKeysModes.Joystick || Settings.UsePlayersKeys == PlayerKeysModes.Com) && _buttonManager != null)
                {
                    _buttonManager.Stop();
                    await _buttonManager.DisposeAsync();
                    _buttonManager = null;
                }
            }
        }
    }

    private void RemovePlayerButton_Executed(object? arg)
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

    private void SetPlayerButton_Executed(object? arg)
    {
        // Do nothing; the command is activated by key press
    }

    private void ShowError(string error) => PlatformManager.Instance.ShowMessage(error);

    /// <summary>
    /// Shows error message.
    /// </summary>
    /// <param name="exc">Error to show.</param>
    private void ShowError(Exception exc) => ShowError($"{Resources.Error}: {exc.Message}");

    private void UpdateStartCommand()
    {
        _start.CanBeExecuted = _mode == GameMode.Start && _packageSource != null;
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public PlayerInfo? GetPlayerById(string playerId, bool strict) => throw new NotImplementedException();

    public bool OnKeyPressed(GameKey key)
    {
        // Задание кнопки для игрока (в настройках)
        // Может быть не только при this.engine.stage == GameStage.Before, но и в процессе игры
        if (_activePlayerButtonCommand == _setPlayerButton)
        {
            if (Settings.UsePlayersKeys == PlayerKeysModes.Joystick)
            {
                ProcessNewPlayerButton(key);

                _buttonManager.Stop();
                _buttonManager.DisposeAsync(); // no await
                _buttonManager = null;
            }
        }

        return false;
    }

    public bool OnPlayerPressed(PlayerInfo player) => throw new NotImplementedException();
}
