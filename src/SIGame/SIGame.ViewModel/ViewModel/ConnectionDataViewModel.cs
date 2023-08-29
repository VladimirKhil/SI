using Microsoft.Extensions.DependencyInjection;
using SICore;
using SICore.BusinessLogic;
using SICore.Contracts;
using SICore.Network;
using SICore.Network.Clients;
using SICore.Network.Configuration;
using SICore.Network.Servers;
using SIData;
using SIGame.ViewModel.Models;
using SIGame.ViewModel.PlatformSpecific;
using SIGame.ViewModel.Properties;
using SIStorageService.ViewModel;
using System.Diagnostics;
using System.Windows.Input;

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

    public CustomCommand NewGame { get; private set; }

    protected abstract bool IsOnline { get; }

    protected SecondaryNode _node;
    protected Client _client;
    protected IViewerClient _host;
    protected Connector? _connector;

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

    protected GameSettingsViewModel GameSettings { get; private set; }

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

    private void NewGame_Executed(object? arg)
    {
        _userSettings.GameSettings.HumanPlayerName = Human.Name;

        var siStorage = PlatformManager.Instance.ServiceProvider.GetRequiredService<SIStorage>();

        GameSettings = new GameSettingsViewModel(_userSettings.GameSettings, _commonSettings, _userSettings, siStorage, true, MaxPackageSize)
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

    protected override void OnStartGame(
        Node node,
        IViewerClient host,
        bool networkGame,
        bool isOnline,
        string tempDocFolder,
        IFileShare? fileShare,
        int networkGamePort) =>
        base.OnStartGame(node, host, networkGame, IsOnline, tempDocFolder, fileShare, networkGamePort);

    protected virtual void Prepare(GameSettingsViewModel gameSettings)
    {
       
    }

    #endregion

    #region Начальное подключение к серверу

    protected async Task InitServerAndClientAsync(string address, int port)
    {
        if (_node != null)
        {
            await _node.DisposeAsync();
            _node = null;
        }

        _node = new TcpSlaveServer(
            port,
            address,
            NodeConfiguration.Default,
            new NetworkLocalizer(Thread.CurrentThread.CurrentUICulture.Name));

        _client = new Client(Human.Name);
        _client.ConnectTo(_node);
    }

    protected async Task ConnectCoreAsync(bool upgrade)
    {
        await _node.ConnectAsync(upgrade);

        if (_connector != null)
        {
            _connector.Dispose();
        }

        _connector = new Connector(_node, _client);
    }

    #endregion

    #region Вход в игру

    private async void Join_Executed(object? arg) => await JoinGameAsync(null, (GameRole)arg);

    protected virtual async Task JoinGameAsync(GameInfo gameInfo, GameRole role, bool host = false, CancellationToken cancellationToken = default)
    {
        IsProgress = true;

        try
        {
            await JoinGameCoreAsync(gameInfo, role, host, cancellationToken);
        }
        catch (Exception exc)
        {
            try
            {
                Error = exc.Message;
                FullError = exc.ToString();

                if (_host != null)
                {
                    await _host.DisposeAsync();
                }
            }
            catch { }
        }
        finally
        {
            IsProgress = false;
        }
    }

    public virtual async Task JoinGameCoreAsync(
        GameInfo gameInfo,
        GameRole role,
        bool isHost = false,
        CancellationToken cancellationToken = default)
    {
        var name = Human.Name;

        var sex = Human.IsMale ? 'm' : 'f';
        var command = $"{Messages.Connect}\n{role.ToString().ToLowerInvariant()}\n{name}\n{sex}\n{-1}{GetExtraCredentials()}";

        _ = await _connector.JoinGameAsync(command);
        await JoinGameCompletedAsync(role, isHost, cancellationToken);
    }

    protected virtual string GetExtraCredentials() => "";

    protected Task<(string? AvatarUrl, FileKey? FileKey)>? _avatarLoadingTask;

    protected async Task JoinGameCompletedAsync(GameRole role, bool isHost, CancellationToken cancellationToken = default)
    {
        await _node.ConnectionsLock.WithLockAsync(
            async () =>
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

                    if (_host != null)
                    {
                        await _host.DisposeAsync();
                    }

                    return;
                }
            },
            cancellationToken);

        var humanPlayer = Human;
        var name = humanPlayer.Name;

        var data = new ViewerData(BackLink.Default)
        {
            ServerPublicUrl = ContentPublicBaseUrls?.FirstOrDefault(),
            ContentPublicUrls = ContentPublicBaseUrls,
            ServerAddress = ServerAddress,
            IsNetworkGame = true
        };

        var localizer = new Localizer(Thread.CurrentThread.CurrentUICulture.Name);

        _host = role switch
        {
            GameRole.Showman => new Showman(_client, humanPlayer, isHost, localizer, data),
            GameRole.Player => new Player(_client, humanPlayer, isHost, localizer, data),
            _ => new SimpleViewer(_client, humanPlayer, isHost, localizer, data),
        };

        _host.Avatar = _avatarLoadingTask != null ? (await _avatarLoadingTask).AvatarUrl : null;

        _host.Connector = new ReconnectManager(_node, _client, _host, humanPlayer, role, GetExtraCredentials(), IsOnline)
        {
            ServerAddress = ServerAddress
        };

        _host.Client.ConnectTo(_node);

        _releaseServer = false;

        if (!isHost && Ready != null)
        {
            Ready(_node, _host, IsOnline); // Здесь происходит переход к игре
        }

        if (_connector != null)
        {
            _connector.Dispose();
            _connector = null;
        }

        await ClearConnectionAsync();

        _host.GetInfo();

        Trace.TraceInformation("INFO request sent");

        Error = null;

        _node.Error += Server_Error;
    }

    private void Server_Error(Exception exc, bool isWarning) =>
        PlatformManager.Instance.ShowMessage(
            $"{Resources.GameEngineError}: {exc.Message} {exc.InnerException}",
            isWarning ? MessageType.Warning : MessageType.Error,
            true);

    protected virtual Task ClearConnectionAsync() => Task.CompletedTask;

    public event Action<Node, IViewerClient, bool> Ready;

    #endregion

    public virtual async ValueTask DisposeAsync()
    {
        await ClearConnectionAsync();

        if (_connector != null)
        {
            _connector.Dispose();
            _connector = null;
        }

        if (_releaseServer && _node != null)
        {
            await _node.DisposeAsync();
            _node = null;
        }
    }
}
