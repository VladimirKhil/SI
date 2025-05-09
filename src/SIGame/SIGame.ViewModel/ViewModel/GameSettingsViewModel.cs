﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SI.GameServer.Contract;
using SICore;
using SICore.Clients;
using SICore.Contracts;
using SICore.Network.Clients;
using SICore.Network.Configuration;
using SICore.Network.Servers;
using SICore.Services;
using SICore.Special;
using SIData;
using SIGame.ViewModel.Contracts;
using SIGame.ViewModel.Data;
using SIGame.ViewModel.Models;
using SIGame.ViewModel.PackageSources;
using SIGame.ViewModel.PlatformSpecific;
using SIGame.ViewModel.Properties;
using SIGame.ViewModel.Services;
using SIGame.ViewModel.Web;
using SIPackages;
using SIStorage.Service.Client;
using SIStorage.Service.Contract;
using SIStorageService.ViewModel;
using SIUI.ViewModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;
using Utils;
using Utils.Commands;

namespace SIGame.ViewModel;

public sealed class GameSettingsViewModel : ViewModelWithNewAccount<GameSettings>, INavigatable, IPackageSelector, IDisposable
{
    private const string LocalAddress = "http://localhost";
    
    private readonly Random _random = new();

    private string _duplicatingName = null;

    internal CancellationTokenSource CancellationTokenSource { get; set; }

    /// <summary>
    /// Номер за столом в роли игрока
    /// </summary>
    public int PlayerNumber => 0;

    /// <summary>
    /// Роль организатора в игре
    /// </summary>
    public GameRole Role
    {
        get => _model.Role;
        set
        {
            if (_model.Role != value)
            {
                _model.Role = value;
                OnPropertyChanged();
                UpdateRoleTrigger();
                NetworkGameOrRoleChanged(true);
            }
        }
    }

    /// <summary>
    /// Будет ли игра сетевой
    /// </summary>
    public bool NetworkGame
    {
        get => _model.NetworkGame;
        set
        {
            _model.NetworkGame = value;
            OnPropertyChanged();
            UpdateRoleTrigger();
            SetErrorMessage();
            NetworkGameOrRoleChanged(false);
        }
    }

    /// <summary>
    /// Тип сетевой игры
    /// </summary>
    public NetworkGameType NetworkGameType
    {
        get => _model.NetworkGameType;
        set
        {
            _model.NetworkGameType = value;
            OnPropertyChanged();
            SetErrorMessage();
        }
    }

    /// <summary>
    /// Название сетевой игры
    /// </summary>
    public string NetworkGameName
    {
        get => _model.NetworkGameName;
        set
        {
            _model.NetworkGameName = value;
            OnPropertyChanged();
            SetErrorMessage();
        }
    }

    /// <summary>
    /// Пароль сетевой игры
    /// </summary>
    public string NetworkGamePassword
    {
        get => _model.NetworkGamePassword;
        set
        {
            _model.NetworkGamePassword = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Network voice chat link.
    /// </summary>
    public string NetworkVoiceChat
    {
        get => _model.NetworkVoiceChat;
        set
        {
            _model.NetworkVoiceChat = value;
            OnPropertyChanged();
        }
    }

    public int PlayersCount
    {
        get => _model.PlayersCount;
        set
        {
            var oldValue = _model.PlayersCount;

            if (oldValue != value)
            {
                _model.PlayersCount = value;
                OnPropertyChanged();

                if (oldValue < value)
                {
                    do
                    {
                        Players.Add(new GameAccount(this) { AccountType = AccountTypes.Computer });
                        oldValue++;
                    } while (oldValue < value);
                }
                else
                {
                    do
                    {
                        Players.RemoveAt(oldValue - 1);
                        oldValue--;
                    } while (oldValue > value);
                }
            }
        }
    }

    /// <summary>
    /// Порт сетевой игры
    /// </summary>
    public int NetworkPort
    {
        get => _model.NetworkPort;
        set { _model.NetworkPort = value; OnPropertyChanged(); SetErrorMessage(); }
    }

    private GameAccount _showman;

    public GameAccount Showman
    {
        get => _showman;
        set
        {
            if (_showman != value)
            {
                if (_showman != null)
                {
                    _showman.PropertyChanged -= Showman_PropertyChanged;
                }

                _showman = value;

                if (_showman != null)
                {
                    _showman.PropertyChanged += Showman_PropertyChanged;
                }

                OnPropertyChanged();
                UpdateShowman();
            }
        }
    }

    private readonly ComputerAccount[] _defaultComputerPlayers = StoredPersonsRegistry.GetDefaultPlayers(
        new Localizer(Thread.CurrentThread.CurrentUICulture.Name),
        Global.PhotoUri);

    private readonly ComputerAccount[] _defaultComputerShowmans = StoredPersonsRegistry.GetDefaultShowmans(
        new Localizer(Thread.CurrentThread.CurrentUICulture.Name),
        Global.PhotoUri);

    private readonly ComputerAccount _newPlayerAccount = new(Resources.New + "…", true);
    private readonly ComputerAccount _newShowmanAccount = new(Resources.New + "…", true);

    private ComputerAccount[] _computerPlayers;
    private ComputerAccount[] _computerShowmans;

    public void UpdateComputerPlayers()
    {
        var result = new List<ComputerAccount>(_defaultComputerPlayers);
        result.AddRange(_commonSettings.CompPlayers2);
        result.Add(_newPlayerAccount);
        _computerPlayers = result.ToArray();

        foreach (var player in Players)
        {
            if (player.AccountType == AccountTypes.Computer)
            {
                player.SelectionList = _computerPlayers;
            }
        }
    }

    public void UpdateComputerShowmans()
    {
        var result = new List<ComputerAccount>(_defaultComputerShowmans);
        result.AddRange(_commonSettings.CompShowmans2);
        result.Add(_newShowmanAccount);
        _computerShowmans = result.ToArray();

        if (_showman != null && _showman.AccountType == AccountTypes.Computer)
        {
            _showman.SelectionList = _computerShowmans;
        }
    }

    /// <summary>
    /// Игроки
    /// </summary>
    public ObservableCollection<GameAccount> Players { get; } = new ObservableCollection<GameAccount>();

    /// <summary>
    /// Зрители
    /// </summary>
    public ObservableCollection<SimpleAccount<HumanAccount>> Viewers { get; } = new ObservableCollection<SimpleAccount<HumanAccount>>();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _errorMessage = "";

    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    private PackageSource _package;

    /// <summary>
    /// Игровой пакет
    /// </summary>
    public PackageSource Package
    {
        get => _package;
        set
        {
            _package = value;
            _model.PackageKey = value.Key;
            OnPropertyChanged();
            SetErrorMessage();
        }
    }

    public IAsyncCommand BeginGame { get; private set; }

    public ICommand RemoveComputerAccount { get; private set; }

    public ICommand EditComputerAccount { get; private set; }

    public ICommand RemoveShowmanAccount { get; private set; }

    private void NewComputerAccount_Add(ComputerAccount origin, ComputerAccount account)
    {
        _commonSettings.CompPlayers2.Add(account);
        UpdateComputerPlayers();

        var gamePlayers = Players;

        foreach (var player in gamePlayers)
        {
            if (player.SelectedAccount == _computerPlayers.Last())
            {
                player.SelectedAccount = account;
            }
        }
    }

    private void NewComputerAccount_Edit(ComputerAccount origin, ComputerAccount account)
    {
        var players = _commonSettings.CompPlayers2;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == origin)
            {
                var oldAccount = players[i];
                GameAccount? playerAcc = null;

                var gamePlayers = Players;

                foreach (var player in gamePlayers)
                {
                    if (player.SelectedAccount == oldAccount)
                    {
                        playerAcc = player;
                        break;
                    }
                }

                players[i] = account;
                UpdateComputerPlayers();

                if (playerAcc != null)
                {
                    playerAcc.SelectedAccount = account;
                }
            }
        }
    }

    private void NewShowmanAccount_Add(ComputerAccount account)
    {
        _commonSettings.CompShowmans2.Add(account);
        UpdateComputerShowmans();

        Showman.SelectedAccount = account;

        Content = null;
    }

    private readonly AsyncContextCommand _selectPackage;

    public AsyncContextCommand SelectPackage => _selectPackage;

    private ICommand _closeNewShowman;

    private ICommand _closeNewPlayer;

    private readonly CommonSettings _commonSettings;

    internal event Func<GameSettings, PackageSource, Task<(SecondaryNode, IViewerClient, GameViewModel)?>> CreateGame;

    public event Action<ContentBox> Navigate;

    private bool _isProgress;

    public bool IsProgress
    {
        get => _isProgress;
        set { _isProgress = value; OnPropertyChanged(); }
    }

    private string _message;

    public string Message
    {
        get => _message;
        set { _message = value; OnPropertyChanged(); }
    }

    private readonly long? _maxPackageSize;
    private readonly SettingsViewModel _settingsViewModel;

    private readonly SIStorageInfo[] _libraries;

    private readonly UserSettings _userSettings;
    private readonly ISIStorageClientFactory _siStorageClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    public GameSettingsViewModel(
        GameSettings gameSettings,
        CommonSettings commonSettings,
        UserSettings userSettings,
        SettingsViewModel settingsViewModel,
        ISIStorageClientFactory siStorageClientFactory,
        SIStorageInfo[] libraries,
        ILoggerFactory loggerFactory,
        bool isNetworkGame = false,
        long? maxPackageSize = null)
        : base(gameSettings)
    {
        NetworkGame = isNetworkGame;
        _commonSettings = commonSettings;
        _userSettings = userSettings;
        _settingsViewModel = settingsViewModel;
        _siStorageClientFactory = siStorageClientFactory;
        _libraries = libraries;
        _loggerFactory = loggerFactory;

        UpdateComputerPlayers();
        UpdateComputerShowmans();

        gameSettings.Updated += GameSettings_Updated;

        _maxPackageSize = maxPackageSize;

        _selectPackage = new AsyncContextCommand(SelectPackage_Executed);

        _selectPackage.ExecutionContext.Add(PackageSourceTypes.Local);
        _selectPackage.ExecutionContext.Add(PackageSourceTypes.SIStorage);
        _selectPackage.ExecutionContext.Add(PackageSourceTypes.RandomServer);
        _selectPackage.ExecutionContext.Add(new SIStorageParameters { StorageIndex = 0, IsRandom = true });
        _selectPackage.ExecutionContext.Add(new SIStorageParameters { StorageIndex = 0, IsRandom = false });
        _selectPackage.ExecutionContext.Add(PackageSourceTypes.VK);

        var packageDirExists = Directory.Exists(Global.PackagesUri);

        if (packageDirExists)
        {
            _selectPackage.ExecutionContext.Add(PackageSourceTypes.Random);
        }

        if (packageDirExists)
        {
            _selectPackage.ExecutionContext.Add(PackageSourceTypes.Next);
        }

        if (_package == null)
        {
            if (_model.PackageKey != null && _selectPackage.ExecutionContext.Contains(_model.PackageKey.Type))
            {
                switch (_model.PackageKey.Type)
                {
                    case PackageSourceTypes.Next:
                        Package = new NextPackageSource();
                        break;

                    case PackageSourceTypes.Random:
                        Package = new RandomPackageSource();
                        break;

                    case PackageSourceTypes.RandomServer:
                        Package = new RandomStoragePackageSource(PlatformManager.Instance.ServiceProvider!.GetRequiredService<ISIStorageServiceClient>(), _userSettings);
                        break;

                    case PackageSourceTypes.Local:
                        Package = new CustomPackageSource(_model.PackageKey.Data);
                        break;

                    case PackageSourceTypes.SIStorage:
                        var key = _model.PackageKey;

                        if (Uri.TryCreate(key.Data, UriKind.Absolute, out var uri))
                        {
                            Package = new SIStoragePackageSource(uri, key.ID, key.Name, key.PackageID);
                        }
                        break;
                }
            }
            else if (packageDirExists)
            {
                Package = new RandomPackageSource();
            }
        }
    }

    private void GameSettings_Updated() => OnPropertyChanged(nameof(NetworkPort));

    protected override void Initialize()
    {
        base.Initialize();

        Players.CollectionChanged += Players_CollectionChanged;
        Viewers.CollectionChanged += Viewers_CollectionChanged;

        BeginGame = new AsyncCommand(BeginGame_ExecutedAsync);

        RemoveComputerAccount = new SimpleCommand(RemoveComputerAccount_Executed);
        RemoveShowmanAccount = new SimpleCommand(RemoveShowmanAccount_Executed);
        EditComputerAccount = new SimpleCommand(EditComputerAccount_Executed);

        _closeNewShowman = new SimpleCommand(CloseNewShowman_Executed);
        _closeNewPlayer = new SimpleCommand(CloseNewPlayer_Executed);

        SetErrorMessage();
    }

    private void CloseNewShowman_Executed(object? arg)
    {
        _showman.SelectedAccount = _computerShowmans[0];
        _closeContent.Execute(arg);
    }

    private void CloseNewPlayer_Executed(object? arg)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].SelectedAccount == _computerPlayers.Last())
            {
                UpdatePlayer(Players[i], true);
            }
        }

        _closeContent.Execute(arg);
    }

    private async Task SelectPackage_Executed(object? arg)
    {
        if (arg == null)
        {
            return;
        }

        if (arg is SIStorageParameters storageParameters)
        {
            var siStorageClient = _siStorageClientFactory.CreateClient(_libraries[storageParameters.StorageIndex].ServiceUri);

            if (storageParameters.IsRandom)
            {
                Package = new RandomStoragePackageSource(siStorageClient, _userSettings);
            }
            else
            {
                var storageViewModel = new StorageViewModel(siStorageClient)
                {
                    DefaultLanguage = Thread.CurrentThread.CurrentUICulture.Name
                };

                var storageInfo = new SIStorageViewModel(storageViewModel, _userSettings, this);
                var contentBox = new ContentBox { Data = storageInfo, Title = Resources.SIStorage };
                await storageInfo.InitAsync();
                Navigate?.Invoke(contentBox);
            }

            return;
        }

        var code = (PackageSourceTypes)arg;

        switch (code)
        {
            case PackageSourceTypes.Next:
                Package = new NextPackageSource();
                break;

            case PackageSourceTypes.Random:
                Package = new RandomPackageSource();
                break;

            case PackageSourceTypes.Local:
                var packagePath = PlatformManager.Instance.SelectLocalPackage(_maxPackageSize);

                if (packagePath != null)
                {
                    Package = new CustomPackageSource(packagePath);
                }
                break;

            case PackageSourceTypes.VK:
                try
                {
                    Browser.Open(Resources.ThemesLink);
                }
                catch (Exception exc)
                {
                    PlatformManager.Instance.ShowMessage(
                        string.Format(Resources.VKThemesError + "\r\n{1}", Resources.ThemesLink, exc.Message),
                        MessageType.Error);
                }
                break;
        }
    }

    public void OnNavigatedFrom(object data)
    {
        if (data is ShowmanViewModel)
        {
            _showman.SelectedAccount = _computerShowmans[0];
        }
        else if (data is ComputerAccountViewModel)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                if (Players[i].SelectedAccount == _computerPlayers.Last())
                {
                    UpdatePlayer(Players[i], true);
                }
            }
        }
    }

    private void RemoveShowmanAccount_Executed(object? account)
    {
        var computerAccount = account as ComputerAccount;
        if (!PlatformManager.Instance.Ask(string.Format(Resources.ShowmanDeleteConfirm, computerAccount.Name)))
            return;

        _commonSettings.CompShowmans2.Remove(computerAccount);
        UpdateComputerShowmans();

        Showman.SelectedAccount = _computerShowmans.FirstOrDefault();
    }

    private void RemoveComputerAccount_Executed(object? account)
    {
        var computerAccount = account as ComputerAccount;
        if (!PlatformManager.Instance.Ask(string.Format(Resources.PlayerDeleteConfirm, computerAccount.Name)))
            return;

        _commonSettings.CompPlayers2.Remove(computerAccount);
        UpdateComputerPlayers();

        foreach (var player in Players)
        {
            if (player.SelectedAccount == null)
            {
                UpdatePlayer(player, true);
                break;
            }
        }
    }

    private void EditComputerAccount_Executed(object? arg)
    {
        var computerAccount = (ComputerAccount)arg;
        var newComputerAccount = new ComputerAccountViewModel(computerAccount.Clone(), computerAccount);
        newComputerAccount.Add += NewComputerAccount_Edit;
        var contentBox = new ContentBox { Data = newComputerAccount, Title = Resources.ComputerPlayer };
        Navigate?.Invoke(contentBox);
    }

    private async Task BeginGame_ExecutedAsync(object? arg)
    {
        IsProgress = true;
        BeginGame.CanBeExecuted = false;

        ErrorMessage = null;
        FullError = null;

        try
        {
            if (CreateGame != null)
            {
                var info = await CreateGame(_model, _package);

                if (!info.HasValue)
                {
                    return;
                }

                MoveToGame(info.Value.Item3, (ViewerHumanLogic)info.Value.Item2.MyLogic);
            }
            else
            {
                var (document, path) = await BeginNewGameAsync();
                BeginNewGameCompleted(document, path);
            }
        }
        catch (TargetInvocationException exc) when (exc.InnerException != null)
        {
            ErrorMessage = exc.InnerException.Message;
            FullError = exc.ToString();
        }
        catch (Exception exc)
        {
            ErrorMessage = exc.Message;
            FullError = exc.ToString();
        }
        finally
        {
            IsProgress = false;
            BeginGame.CanBeExecuted = true;
        }
    }

    private async Task<(SIDocument, string)> BeginNewGameAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var (packageFile, isTemp) = await Package.GetPackageFileAsync(cancellationToken);

            var tempDir = Path.Combine(Path.GetTempPath(), CommonSettings.AppNameEn, Guid.NewGuid().ToString());

            var packageFolderPath = Path.Combine(tempDir, "package");

            var document = await SIDocument.ExtractToFolderAndLoadAsync(
                packageFile,
                packageFolderPath,
                cancellationToken: cancellationToken);

            Directory.CreateDirectory(Path.Combine(tempDir, "avatars"));

            if (isTemp)
            {
                File.Delete(packageFile);
            }

            return (document, tempDir);
        }
        finally
        {
            IsProgress = false;
            BeginGame.CanBeExecuted = true;
        }
    }

    private void BeginNewGameCompleted(SIDocument document, string documentPath)
    {
        var fileShare = new WebManager(
            _model.AppSettings.MultimediaPort,
            new Dictionary<ResourceKind, string>
            {
                [ResourceKind.Package] = Path.Combine(documentPath, "package"),
                [ResourceKind.Avatar] = Path.Combine(documentPath, "avatars"),
                [ResourceKind.DefaultAvatar] = Global.PhotoUri,
            });

        Node node;

        if (NetworkGame)
        {
            node = new TcpMasterServer(NetworkPort, NodeConfiguration.Default);
            ((TcpMasterServer)node).StartListen();
        }
        else
        {
            node = new LocalNode(NodeConfiguration.Default);
        }

        node.Error += Server_Error;

        _model.NetworkGamePassword = "";
        _model.AppSettings.Culture = Thread.CurrentThread.CurrentUICulture.Name;
        _model.HumanPlayerName = Human.Name;

        var avatarHelper = new AvatarHelper(Path.Combine(documentPath, "avatars"));

        var game = GameRunner.CreateGame(
            node,
            _model,
            document,
            BackLink.Default,
            fileShare,
            _computerPlayers.ToArray(),
            _computerShowmans.ToArray(),
            avatarHelper,
            null);

        var data = new ViewerData(BackLink.Default);

        var gameViewModel = new GameViewModel(data, node, _userSettings, _settingsViewModel, fileShare, _loggerFactory.CreateLogger<GameViewModel>())
        {
            NetworkGame = NetworkGame,
            NetworkGamePort = NetworkPort,
            IsOnline = false,
            TempDocFolder = documentPath
        };

        var localizer = new Localizer(_model.AppSettings.Culture);

        var client = new Client(_model.HumanPlayerName);
        var actions = new ViewerActions(client);
        var logic = new ViewerHumanLogic(gameViewModel, data, actions, LocalAddress);

        IViewerClient? host = null;

        // TODO: consider using Join() method
        game.ClientData.BeginUpdatePersons("Add human host");

        try
        {
            if (_model.HumanPlayerName == _model.Showman.Name)
            {
                host = new Showman(client, _model.Showman, true, logic, actions, localizer, data);
                game.ClientData.ShowMan.IsConnected = true;
            }
            else
            {
                for (var i = 0; i < _model.Players.Length; i++)
                {
                    if (_model.Players[i].Name == _model.HumanPlayerName)
                    {
                        host = new Player(client, _model.Players[i], true, logic, actions, localizer, data);
                        game.ClientData.Players[i].IsConnected = true;
                        break;
                    }
                }

                if (host == null)
                {
                    host = new Viewer(client, _model.Viewers[0], true, logic, actions, localizer, data);
                    game.ClientData.Viewers.Add(new ViewerAccount(_model.Viewers[0]) { IsConnected = true });
                }
            }
        }
        finally
        {
            game.ClientData.EndUpdatePersons();
        }

        client.ConnectTo(node);

        gameViewModel.Host = host;
        host.MyData.IsNetworkGame = NetworkGame;

        if (!NetworkGame)
        {
            host.MyData.IsChatOpened = false;
        }

        game.Run();

        MoveToGame(gameViewModel, (ViewerHumanLogic)host.MyLogic);
    }

    private void MoveToGame(GameViewModel gameViewModel, ViewerHumanLogic logic)
    {
        _model.ShowmanType = Showman.AccountType;
        _model.PlayersTypes = Players.Select(p => p.AccountType).ToArray();

        OnStartGame(gameViewModel, logic);
    }

    private void Server_Error(Exception exc, bool isWarning) =>
        PlatformManager.Instance.ShowMessage(
            $"{Resources.GameEngineError}: {exc.Message} {exc.InnerException}",
            isWarning ? MessageType.Warning : MessageType.Error,
            true);

    public void SelectPackageSource(PackageSource packageSource) => Package = packageSource;

    private void NetworkGameOrRoleChanged(bool roleChanged)
    {
        if (_showman == null)
        {
            return;
        }

        if (Role == GameRole.Showman)
        {
            _showman.AccountType = AccountTypes.Human;

            if (roleChanged)
            {
                UpdateShowman();
            }
        }
        else if (!NetworkGame)
        {
            _showman.AccountType = AccountTypes.Computer;
        }
        else
        {
            UpdateShowman();
        }
    }

    private void Showman_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GameAccount.AccountType))
        {
            UpdateShowman();
            return;
        }
        
        if (e.PropertyName == nameof(GameAccount.SelectedAccount))
        {
            if (_showman.SelectedAccount == _computerShowmans.Last()) // New showman
            {
                var account = new ComputerAccount { CanBeDeleted = true };
                var newShowmanAccount = new ShowmanViewModel(account);
                newShowmanAccount.Add += NewShowmanAccount_Add;
                var contentBox = new ContentBox { Data = newShowmanAccount, Title = Resources.NewShowman, Cancel = _closeNewShowman };
                Navigate?.Invoke(contentBox);

                return;
            }

            _model.Showman = _showman.SelectedAccount;
            CheckUniqueAccounts();
        }
    }

    void Players_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (GameAccount item in e.NewItems)
                {
                    UpdatePlayer(item);
                    item.PropertyChanged += Item_PropertyChanged;                        
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                foreach (GameAccount item in e.OldItems)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
                break;
        }

        _model.Players = Players.Select(acc => acc.SelectedAccount).ToArray();
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var acc = (GameAccount)sender;
        if (e.PropertyName == nameof(GameAccount.AccountType))
        {
            UpdatePlayer(acc);
        }
        else if (e.PropertyName == nameof(GameAccount.SelectedAccount))
        {                
            var index = Players.IndexOf(acc);

            if (index > -1 && index < _model.Players.Length)
                _model.Players[index] = acc.SelectedAccount;

            if (acc.SelectedAccount == _computerPlayers.Last())
            {
                var account = new ComputerAccount() { CanBeDeleted = true };

                // Зададим ему рандомные характеристики
                account.Randomize();

                var newComputerAccount = new ComputerAccountViewModel(account, null);
                newComputerAccount.Add += NewComputerAccount_Add;
                var contentBox = new ContentBox { Data = newComputerAccount, Title = Resources.NewPlayer, Cancel = _closeNewPlayer };
                Navigate?.Invoke(contentBox);
            }

            CheckUniqueAccounts();
        }
    }

    private void Viewers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (INotifyPropertyChanged item in e.NewItems)
                {
                    item.PropertyChanged += Viewer_PropertyChanged;
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                foreach (INotifyPropertyChanged item in e.OldItems)
                {
                    item.PropertyChanged -= Viewer_PropertyChanged;
                }
                break;
        }

        _model.Viewers = Viewers.Select(acc => acc.SelectedAccount).ToArray();
    }

    private void Viewer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SimpleAccount<HumanAccount>.SelectedAccount))
        {
            var acc = (SimpleAccount<HumanAccount>)sender;
            var index = Viewers.IndexOf(acc);
            _model.Viewers[index] = acc.SelectedAccount;
            CheckUniqueAccounts();
        }
    }

    private void CheckUniqueAccounts()
    {
        var accounts = new List<Account>();
        accounts.AddRange(Players.Select(acc => acc.SelectedAccount));
        accounts.AddRange(Viewers.Select(acc => acc.SelectedAccount));

        if (Showman != null)
        {
            accounts.Add(Showman.SelectedAccount);
        }

        for (int i = 0; i < accounts.Count; i++)
        {
            if (accounts[i] == null || accounts[i].Name == Constants.FreePlace)
            {
                continue;
            }

            for (int j = i + 1; j < accounts.Count; j++)
            {
                if (accounts[j] != null && accounts[i].Name == accounts[j].Name)
                {
                    _duplicatingName = accounts[i].Name;
                    SetErrorMessage();
                    return;
                }
            }
        }

        _duplicatingName = null;
        SetErrorMessage();
    }

    protected override void OnHumanChanged()
    {
        base.OnHumanChanged();

        UpdateRoleTrigger();
    }

    private void UpdateRoleTrigger()
    {
        if (Human == null)
        {
            return;
        }

        int player = -1;

        if (Role == GameRole.Player)
        {
            player = PlayerNumber;

            if (PlayerNumber > -1 && PlayerNumber < Players.Count)
            {
                Players[PlayerNumber].AccountType = AccountTypes.Human;
            }
        }

        for (int i = 0; i < Players.Count; i++)
        {
            if (i != player && !_model.NetworkGame)
            {
                Players[i].AccountType = AccountTypes.Computer;
            }
            else
            {
                UpdatePlayer(Players[i]);
            }
        }

        if (Role == GameRole.Viewer)
        {
            if (Viewers.Count == 0)
            {
                Viewers.Add(new SimpleAccount<HumanAccount>() { SelectedAccount = Human });
            }
            else
            {
                Viewers[0].SelectedAccount = Human;
            }
        }
        else if (Viewers.Count > 0)
        {
            Viewers.Clear();
            CheckUniqueAccounts();
        }
    }

    private void UpdateShowman()
    {
        if (_showman == null)
        {
            return;
        }

        if (_showman.AccountType == AccountTypes.Human)
        {
            if (!_model.NetworkGame || _model.Role == GameRole.Showman)
            {
                _showman.SelectionList = new List<Account>(new Account[] { Human });
                _showman.SelectedAccount = Human;
            }
            else
            {
                var oneAccount = new HumanAccount { Name = Constants.FreePlace, IsHuman = true };
                _showman.SelectionList = new Account[] { oneAccount };
                _showman.SelectedAccount = oneAccount;
            }
        }
        else if (_showman.SelectionList != _computerShowmans)
        {
            _showman.SelectionList = _computerShowmans;
            _showman.SelectedAccount = _computerShowmans[0];
        }
    }

    private void UpdatePlayer(GameAccount player, bool force = false)
    {
        if (player == null)
        {
            return;
        }

        if (player.AccountType == AccountTypes.Human)
        {
            if (!_model.NetworkGame || _model.Role == GameRole.Player && _model.PlayerNumber > -1
                && _model.PlayerNumber < Players.Count && Players[_model.PlayerNumber] == player)
            {
                player.SelectionList = new List<Account>(new Account[] { Human });
                player.SelectedAccount = Human;
            }
            else
            {
                var oneAccount = new HumanAccount { Name = Constants.FreePlace, IsHuman = true };
                player.SelectionList = new Account[] { oneAccount };
                player.SelectedAccount = oneAccount;
            }
        }
        else if (force || player.SelectionList != _computerPlayers)
        {
            var visited = new List<int>();

            for (int i = 0; i < Players.Count; i++)
            {
                if (Players[i] != player && Players[i].AccountType == AccountTypes.Computer)
                {
                    visited.Add(Array.IndexOf(_computerPlayers, (ComputerAccount)Players[i].SelectedAccount));
                }
            }

            var index = _random.Next(_computerPlayers.Length - visited.Count - 1);

            while (visited.Contains(index))
            {
                index++;
            }

            if (!force)
            {
                player.SelectionList = _computerPlayers;
            }

            player.SelectedAccount = _computerPlayers[index];
        }
    }

    private void SetErrorMessage()
    {
        if (_package == null)
        {
            ErrorMessage = " ";
            BeginGame.CanBeExecuted = false;
        }
        else if (_duplicatingName != null)
        {
            ErrorMessage = string.Format(Resources.IsDouble, _duplicatingName);
            BeginGame.CanBeExecuted = false;
        }
        else if (NetworkGame && NetworkGameType == NetworkGameType.GameServer && string.IsNullOrEmpty(NetworkGameName))
        {
            ErrorMessage = Resources.GameNameRequired;
            BeginGame.CanBeExecuted = false;
        }
        else if (NetworkGame && NetworkGameType == NetworkGameType.DirectConnection && NetworkPort == 0)
        {
            ErrorMessage = Resources.PortNumberRequired;
            BeginGame.CanBeExecuted = false;
        }
        else
        {
            ErrorMessage = "";
            FullError = null;
            BeginGame.CanBeExecuted = true;
        }
    }

    internal void PrepareForGame()
    {
        Viewers.Clear();

        if (Role == GameRole.Viewer)
        {
            Viewers.Add(new SimpleAccount<HumanAccount> { SelectedAccount = Human });
        }

        Players.Clear();

        var playersCount = PlayersCount;

        var anyAccount = new HumanAccount { Name = Constants.FreePlace, CanBeDeleted = false };

        var playerTypes = Model.PlayersTypes;

        for (int i = 0; i < playersCount; i++) // Random players
        {
            if (Role == GameRole.Player && i == PlayerNumber)
            {
                Players.Add(new GameAccount(this) { AccountType = AccountTypes.Human, SelectedAccount = Human });
            }
            else if (!NetworkGame ||
                playerTypes == null ||
                i >= playerTypes.Length ||
                playerTypes[i] == AccountTypes.Computer)
            {
                Players.Add(new GameAccount(this) { AccountType = AccountTypes.Computer });
            }
            else
            {
                Players.Add(new GameAccount(this) { AccountType = AccountTypes.Human, SelectedAccount = anyAccount });
            }
        }

        if (Showman == null)
        {
            if (Role == GameRole.Showman)
            {
                Showman = new GameAccount(this) { AccountType = AccountTypes.Human, SelectedAccount = Human };
            }
            else if (!NetworkGame || _model.ShowmanType == AccountTypes.Computer)
            {
                Showman = new GameAccount(this) { AccountType = AccountTypes.Computer, SelectedAccount = _computerShowmans[0] };
            }
            else
            {
                Showman = new GameAccount(this) { AccountType = AccountTypes.Human, SelectedAccount = anyAccount };
            }
        }
    }

    public void Dispose()
    {
        Model.Updated -= GameSettings_Updated;
    }
}
