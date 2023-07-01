using SICore.Clients.Viewer;
using SICore.Models;
using SIData;
using System.Collections.ObjectModel;
using System.Text;

namespace SICore;

/// <summary>
/// Defines viewer data.
/// </summary>
public sealed class ViewerData : Data
{
    public string ServerAddress { get; set; }

    public string ServerPublicUrl { get; set; }

    public string[] ContentPublicUrls { get; set; }

    private DialogModes _dialogMode = DialogModes.None;

    public DialogModes DialogMode
    {
        get => _dialogMode;
        set { _dialogMode = value; OnPropertyChanged(); }
    }

    private CustomCommand _atomViewed;

    public CustomCommand AtomViewed
    {
        get => _atomViewed;
        set
        {
            if (_atomViewed != value)
            {
                _atomViewed = value;
                OnPropertyChanged();
            }
        }
    }

    public string ProtocolPath { get; set; }

    private CustomCommand _kick;

    public CustomCommand Kick
    {
        get => _kick;
        set
        {
            if (_kick != value)
            {
                _kick = value;
                OnPropertyChanged();
            }
        }
    }

    private CustomCommand _ban;

    public CustomCommand Ban
    {
        get => _ban;
        set
        {
            if (_ban != value)
            {
                _ban = value;
                OnPropertyChanged();
            }
        }
    }

    private CustomCommand _setHost;

    public CustomCommand SetHost
    {
        get => _setHost;
        set
        {
            if (_setHost != value)
            {
                _setHost = value;
                OnPropertyChanged();
            }
        }
    }


    private CustomCommand _unban;

    public CustomCommand Unban
    {
        get => _unban;
        set
        {
            if (_unban != value)
            {
                _unban = value;
                OnPropertyChanged();
            }
        }
    }

    private CustomCommand _forceStart;

    public CustomCommand ForceStart
    {
        get => _forceStart;
        set
        {
            if (_forceStart != value)
            {
                _forceStart = value;
                OnPropertyChanged();
            }
        }
    }

    private CustomCommand _addTable;

    public CustomCommand AddTable
    {
        get => _addTable;
        set
        {
            if (_addTable != value)
            {
                _addTable = value;
                OnPropertyChanged();
            }
        }
    }

    private CustomCommand _deleteTable;

    public CustomCommand DeleteTable
    {
        get => _deleteTable;
        set
        {
            if (_deleteTable != value)
            {
                _deleteTable = value;
                OnPropertyChanged();
            }
        }
    }

    private string _studia;

    public string Studia
    {
        get => _studia;
        set
        {
            _studia = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Файл протокола (логов)
    /// </summary>
    public StreamWriter ProtocolWriter;

    public PersonAccount Speaker { get; set; }

    private string _printedText = "";

    public string PrintedText
    {
        get => _printedText;
        set
        {
            if (_printedText != value)
            {
                _printedText = value;
                OnPropertyChanged();

                SendMessageCommand.CanBeExecuted = value.Length > 0;
            }
        }
    }

    private string _hint = "";

    public string Hint
    {
        get => _hint;
        set { _hint = value; OnPropertyChanged(); }
    }

    private string _stageName = "";

    /// <summary>
    /// Human-readable game stage name.
    /// </summary>
    public string StageName
    {
        get => _stageName;
        set { if (_stageName != value) { _stageName = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Тип вопроса
    /// </summary>
    public string QuestionType { get; set; }

    public bool IsPartial { get; set; }

    public string AtomType { get; set; } = "";

    /// <summary>
    /// Номер текущего атома сценария вопроса
    /// </summary>
    public int AtomIndex { get; set; }

    internal int Winner { get; set; }

    internal int LastStakerIndex { get; set; } = -1;

    public string Name { get; internal set; }

    public ViewerAccount Me
    {
        get
        {
            AllPersons.TryGetValue(Name, out var me);
            return me;
        }
    }

    public bool IsInfoInitialized { get; set; }

    public bool IsChatOpened { get; set; } = true;

    /// <summary>
    /// Адрес изображения участника
    /// </summary>
    internal string Picture { get; set; }

    private bool _isPlayer;

    public bool IsPlayer
    {
        get => _isPlayer;
        set
        {
            if (_isPlayer != value)
            {
                _isPlayer = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _iReady = false;

    /// <summary>
    /// Готов ли участник к игре
    /// </summary>
    public bool IReady
    {
        get => _iReady;
        set { _iReady = value; OnPropertyChanged(); }
    }

    internal string Sound { set => BackLink.PlaySound(value); }

    internal bool FalseStart { get; set; } = true;

    public CustomCommand SendMessageCommand { get; set; }

    public PersonData PersonDataExtensions { get; private set; } = new();

    public PlayerData PlayerDataExtensions { get; private set; } = new();

    public ShowmanData ShowmanDataExtensions { get; private set; } = new();

    /// <summary>
    /// Делегат, организующий отправку сообщения
    /// </summary>
    public Action<string> MessageSending { get; set; }

    public event Action<string?, string, LogMode> StringAdding;

    private List<PlayerAccount> _players = new();

    /// <summary>
    /// Game players.
    /// </summary>
    public List<PlayerAccount> Players
    {
        get => _players;
        internal set
        {
            if (_players != value)
            {
                _players = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ApellationWrongEnabled));
            }
        }
    }

    /// <summary>
    /// Observable version of <see cref="Players" />.
    /// </summary>
    public ObservableCollection<PlayerAccount> PlayersObservable { get; } = new();

    private PersonAccount? _showMan = null;

    /// <summary>
    /// Ведущий
    /// </summary>
    public PersonAccount? ShowMan
    {
        get => _showMan;
        set
        {
            _showMan = value;
            OnPropertyChanged();

            if (_isUpdating)
            {
                return;
            }

            OnMainPersonsChanged();
            OnAllPersonsChanged();
        }
    }

    private bool _showMainTimer;

    public bool ShowMainTimer
    {
        get => _showMainTimer;
        set
        {
            if (_showMainTimer != value)
            {
                _showMainTimer = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _enableMediaLoadButton;

    /// <summary>
    /// Shows button that enables external media load.
    /// </summary>
    public bool EnableMediaLoadButton
    {
        get => _enableMediaLoadButton;
        set { if (_enableMediaLoadButton != value) { _enableMediaLoadButton = value; OnPropertyChanged(); } }
    }

    public void OnAllPersonsChanged()
    {
        PersonsUpdateHistory.Append("Update: ").Append(PrintPersons()).AppendLine();

        var accounts = new List<ViewerAccount>();

        if (_showMan != null)
        {
            accounts.Add(_showMan);
        }

        try
        {
            AllPersons = accounts.Concat(_players).Concat(_viewers)
                .Where(account => account.IsConnected)
                .ToDictionary(account => account.Name);
        }
        catch (ArgumentException exc)
        {
            throw new Exception($"OnAllPersonsChanged error: {PersonsUpdateHistory}", exc);
        }

        if (IsInfoInitialized && !AllPersons.ContainsKey(Name))
        {
            throw new Exception($"!AllPersons.ContainsKey({Name})! {string.Join(",", AllPersons.Keys)} {PersonsUpdateHistory}");
        }
    }

    public void OnMainPersonsChanged()
    {
        var accounts = new List<PersonAccount>();

        if (_showMan != null)
        {
            accounts.Add(_showMan);
        }

        MainPersons = accounts.Concat(_players).ToArray();
    }

    private List<ViewerAccount> _viewers = new();

    /// <summary>
    /// Зрители
    /// </summary>
    public List<ViewerAccount> Viewers
    {
        get => _viewers;
        internal set
        {
            if (_viewers != value)
            {
                _viewers = value;
                OnPropertyChanged();
            }
        }
    }

    internal void UpdatePlayers() => OnPropertyChanged(nameof(Players));

    internal void UpdateViewers() => OnPropertyChanged(nameof(Viewers));

    private PersonAccount[] _mainPersons = Array.Empty<PersonAccount>();

    /// <summary>
    /// Main game persons (showman and players).
    /// </summary>
    internal PersonAccount[] MainPersons
    {
        get => _mainPersons;
        private set
        {
            _mainPersons = value;
            OnPropertyChanged();
        }
    }

    private Dictionary<string, ViewerAccount> _allPersons = new();

    /// <summary>
    /// All persons connected to game.
    /// </summary>
    public Dictionary<string, ViewerAccount> AllPersons
    {
        get => _allPersons;
        private set
        {
            _allPersons = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Banned persons (keys are persons IPs; values are persons names).
    /// </summary>
    public ObservableCollection<BannedInfo> Banned { get; } = new();

    private string _gameMetadata = "";

    /// <summary>
    /// Game metadata.
    /// </summary>
    public string GameMetadata
    {
        get => _gameMetadata;
        set { if (_gameMetadata != value) { _gameMetadata = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Default computer players known by server.
    /// </summary>
    public Account[] DefaultComputerPlayers { get; set; }

    private bool _isUpdating = false;

    internal void BeginUpdatePersons(string? reason = null)
    {
        _isUpdating = true;
        PersonsUpdateHistory.Append($"Before ({reason}): ").Append(PrintPersons());
    }

    internal void EndUpdatePersons()
    {
        _isUpdating = false;

        OnMainPersonsChanged();
        OnAllPersonsChanged();
    }

    private string PrintPersons() => new StringBuilder()
        .Append("Showman: ").Append(PrintAccount(ShowMan)).AppendLine()
        .Append("Players: ").Append(string.Join(", ", Players.Select(PrintAccount))).AppendLine()
        .Append("Viewers: ").Append(string.Join(", ", Viewers.Select(PrintAccount))).AppendLine()
        .ToString();

    private JoinMode _joinMode = JoinMode.AnyRole;

    /// <summary>
    /// Allowed join mode.
    /// </summary>
    public JoinMode JoinMode
    {
        get => _joinMode;
        set
        {
            if (_joinMode != value)
            {
                _joinMode = value;
                OnPropertyChanged();
                OnJoinModeChanged(value);
            }
        }
    }

    public event Action<JoinMode>? JoinModeChanged;

    private void OnJoinModeChanged(JoinMode joinMode) => JoinModeChanged?.Invoke(joinMode);

    public ViewerData(IGameManager gameManager) : base(gameManager)
    {
        Winner = -1;

        SendMessageCommand = new CustomCommand(
            item =>
            {
                MessageSending?.Invoke(PrintedText);
                PrintedText = "";
            })
        {
            CanBeExecuted = false
        };
    }

    private readonly List<string> _chatTable = new();

    /// <summary>
    /// Adds mesage to the game chat.
    /// </summary>
    /// <param name="message">Message to add.</param>
    internal void AddToChat(Message message)
    {
        var index = _chatTable.IndexOf(message.Sender);
        
        // if user is not present in user list, add him
        if (index == -1)
        {
            _chatTable.Add(message.Sender);
            index = _chatTable.Count - 1;
        }

        OnAddString(message.Sender, message.Text, LogMode.Chat + index);
    }

    public override void OnAddString(string? person, string text, LogMode mode) => StringAdding?.Invoke(person, text, mode);

    private bool _autoReady = false;

    public bool AutoReady
    {
        get => _autoReady;
        set
        {
            _autoReady = value;
            OnAutoReadyChanged();
        }
    }

    public string PackageId { get; internal set; }

    public int ButtonBlockingTime { get; internal set; } = 3;

    public string ThemeName { get; internal set; }

    private bool _apellationEnabled = true;

    /// <summary>
    /// Enables apellation buttons.
    /// </summary>
    public bool ApellationEnabled
    {
        get => _apellationEnabled;
        set
        {
            if (_apellationEnabled != value)
            {
                _apellationEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Enabled "Wrong" appellation. 4 or more players required to have enough voice for it to work.
    /// </summary>
    public bool ApellationWrongEnabled => Players.Count > 3;

    private string _hostName;

    /// <summary>
    /// Game host name.
    /// </summary>
    public string HostName
    {
        get => _hostName;
        set
        {
            if (_hostName != value)
            {
                _hostName = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// External media uri.
    /// </summary>
    public string ExternalUri { get; internal set; }

    private string[] _roundNames = Array.Empty<string>();

    /// <summary>
    /// Game rounds names.
    /// </summary>
    public string[] RoundNames
    {
        get => _roundNames;
        set
        {
            _roundNames = value;
            OnPropertyChanged();
        }
    }

    private string? _voiceChatUrl = null;

    /// <summary>
    /// Voice chat url.
    /// </summary>
    public string? VoiceChatUri
    {
        get => _voiceChatUrl;
        set
        {
            if (_voiceChatUrl != value)
            {
                _voiceChatUrl = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Network game flag.
    /// </summary>
    public bool IsNetworkGame { get; set; }

    internal event Action? AutoReadyChanged;

    private void OnAutoReadyChanged() => AutoReadyChanged?.Invoke();
}
