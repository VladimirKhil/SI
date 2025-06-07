using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SICore;
using SICore.Network;
using SICore.Network.Clients;
using SICore.Network.Servers;
using SIData;
using SIGame.ViewModel.Models;
using SIGame.ViewModel.PlatformSpecific;
using SIGame.ViewModel.Properties;
using SIStorage.Service.Client;
using SIUI.ViewModel;
using System.Data;
using System.Windows.Input;
using Utils.Commands;

namespace SIGame.ViewModel;

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
public abstract class ConnectionDataViewModel : ViewModelWithNewAccount<ConnectionData>, IAsyncDisposable
{
    #region Commands

    private ExtendedCommand _join;

    // Команда для этапа 3 - получения информации об игре - не нужна, т.к. всё происходит автоматически после пункта 2
    /// <summary>
    /// Войти в игру
    /// </summary>
    public ICommand Join => _join;

    public SimpleCommand NewGame { get; private set; }

    protected abstract bool IsOnline { get; }

    protected SecondaryNode _node;
    protected Client _client;
    protected IViewerClient _host;

    protected virtual string[] ContentPublicBaseUrls { get; } = Array.Empty<string>();

    protected void UpdateJoinCommand(ConnectionPersonData[] persons)
    {
        _join.ExecutionArea.Clear();

        // Можно под кем-то подключиться
        var showman = persons.FirstOrDefault(p => p.Role == GameRole.Showman);

        if (showman != null && !showman.IsOnline)
        {
            _join.ExecutionArea.Add(GameRole.Showman);
        }

        var players = persons.Where(p => p.Role == GameRole.Player);

        if (players.Any(p => !p.IsOnline))
        {
            _join.ExecutionArea.Add(GameRole.Player);
        }

        _join.ExecutionArea.Add(GameRole.Viewer);

        _join.OnCanBeExecutedChanged();
    }

    #endregion

    #region Data

    private bool _releaseServer = true;

    protected bool ReleaseConnection => _releaseServer;

    private bool _isProgress = false;

    public bool IsProgress
    {
        get => _isProgress;
        set { if (_isProgress != value) { _isProgress = value; OnPropertyChanged(); } }
    }

    private string _error = "";

    public string Error
    {
        get => _error;
        set
        {
            if (_error != value)
            {
                _error = value;
                OnPropertyChanged();

                if (string.IsNullOrEmpty(_error))
                {
                    FullError = null;
                }
            }
        }
    }

    public string ServerAddress { get; protected set; }

    protected virtual long? MaxPackageSize { get; } = null;

    #endregion

    #region Init

    private readonly CommonSettings _commonSettings;
    protected readonly UserSettings _userSettings;
    private readonly SettingsViewModel _settingsViewModel;

    protected GameSettingsViewModel GameSettings { get; private set; }

    protected ConnectionDataViewModel(CommonSettings commonSettings, UserSettings userSettings)
    {
        _commonSettings = commonSettings;
        _userSettings = userSettings;
    }

    protected ConnectionDataViewModel(ConnectionData connectionData, CommonSettings commonSettings, UserSettings userSettings, SettingsViewModel settingsViewModel)
        : base(connectionData)
    {
        _commonSettings = commonSettings;
        _userSettings = userSettings;
        _settingsViewModel = settingsViewModel;
    }

    protected override void Initialize()
    {
        base.Initialize();

        _join = new ExtendedCommand(Join_Executed);
        NewGame = new SimpleCommand(NewGame_Executed);
    }

    private void NewGame_Executed(object? arg)
    {
        _userSettings.GameSettings.HumanPlayerName = Human.Name;

        var siStorageClientFactory = PlatformManager.Instance.ServiceProvider!.GetRequiredService<ISIStorageClientFactory>();
        var siStorageClientOptions = PlatformManager.Instance.ServiceProvider!.GetRequiredService<IOptions<SIStorageClientOptions>>().Value;
        var loggerFactory = PlatformManager.Instance.ServiceProvider!.GetRequiredService<ILoggerFactory>();

        var libraries = new SI.GameServer.Contract.SIStorageInfo[]
        {
            new()
            {
                ServiceUri = siStorageClientOptions.ServiceUri,
                Name = Resources.QuestionLibrary,
                RandomPackagesSupported = true,
                IdentifiersSupported = true
            }
        };

        GameSettings = new GameSettingsViewModel(
            _userSettings.GameSettings,
            _commonSettings,
            _userSettings,
            _settingsViewModel,
            siStorageClientFactory,
            libraries,
            loggerFactory,
            true,
            MaxPackageSize)
        {
            Human = Human,
            ChangeSettings = ChangeSettings
        };

        GameSettings.StartGame += OnStartGame;
        GameSettings.PrepareForGame();

        Prepare(GameSettings);

        var contextBox = new ContentBox
        {
            Data = GameSettings,
            Title = Resources.NewGame
        };

        Content = new NavigatorViewModel
        {
            Content = contextBox,
            Cancel = _closeContent
        };
    }

    protected virtual void Prepare(GameSettingsViewModel gameSettings)
    {
       
    }

    #endregion

    #region Вход в игру

    private async void Join_Executed(object? arg) => await JoinGameAsync(null, (GameRole)arg);

    protected async Task<GameViewModel?> JoinGameAsync(GameInfo? gameInfo, GameRole role, bool host = false, CancellationToken cancellationToken = default)
    {
        IsProgress = true;

        try
        {
            return await JoinGameCoreAsync(gameInfo, role, host, cancellationToken);
        }
        catch (Exception exc)
        {
            try
            {
                Error = exc.Message;
                FullError = exc.ToString();
                _host?.Dispose();
            }
            catch { }
        }
        finally
        {
            IsProgress = false;
        }

        return null;
    }

    public abstract Task<GameViewModel?> JoinGameCoreAsync(
        GameInfo? gameInfo,
        GameRole role,
        bool isHost = false,
        CancellationToken cancellationToken = default);

    protected Task<(string? AvatarUrl, FileKey? FileKey)>? _avatarLoadingTask;

    protected async Task<GameViewModel> JoinGameCompletedAsync(GameRole role, bool isHost, CancellationToken cancellationToken = default)
    {
        await _node.ConnectionsLock.WithLockAsync(
            () =>
            {
                var externalServer = _node.HostServer;

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
                    _host?.Dispose();
                    return;
                }
            },
            cancellationToken);

        var humanPlayer = Human;
        var name = humanPlayer.Name;

        var data = new ViewerData()
        {
            IsNetworkGame = true
        };

        var loggerFactory = PlatformManager.Instance.ServiceProvider!.GetRequiredService<ILoggerFactory>();

        var gameViewModel = new GameViewModel(data, _node, _userSettings, _settingsViewModel, null, loggerFactory.CreateLogger<GameViewModel>())
        {
            IsOnline = IsOnline
        };

        var actions = new ViewerActions(_client);
        
        var logic = new ViewerHumanLogic(
            gameViewModel,
            data,
            actions,
            _userSettings,
            ServerAddress,
            loggerFactory.CreateLogger<ViewerHumanLogic>(),
            ContentPublicBaseUrls?.FirstOrDefault(),
            ContentPublicBaseUrls);

        try
        {
            _host = role switch
            {
                GameRole.Showman => new Showman(_client, humanPlayer, isHost, logic, actions, data),
                GameRole.Player => new Player(_client, humanPlayer, isHost, logic, actions, data),
                _ => new Viewer(_client, humanPlayer, isHost, logic, actions, data),
            };

            gameViewModel.Host = _host;

            _host.Avatar = _avatarLoadingTask != null ? (await _avatarLoadingTask).AvatarUrl : null;
            _client.ConnectTo(_node);

            _releaseServer = false;

            if (!isHost && Ready != null)
            {
                Ready(gameViewModel, logic); // Moving to game view is happening here
            }

            await ClearConnectionAsync();

            _host.GetInfo();

            Error = "";

            _node.Error += Server_Error;

            return gameViewModel;
        }
        catch (Exception exc)
        {
            await logic.DisposeAsync();
            throw;
        }
    }

    private void Server_Error(Exception exc, bool isWarning) =>
        PlatformManager.Instance.ShowMessage(
            $"{Resources.GameEngineError}: {exc.Message} {exc.InnerException}",
            isWarning ? MessageType.Warning : MessageType.Error,
            true);

    protected virtual Task ClearConnectionAsync() => Task.CompletedTask;

    public event Action<GameViewModel, ViewerHumanLogic> Ready;

    #endregion

    public virtual async ValueTask DisposeAsync()
    {
        await ClearConnectionAsync();

        if (_releaseServer && _node != null)
        {
            await _node.DisposeAsync();
            _node = null;
        }
    }
}
