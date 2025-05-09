﻿using SICore.Contracts;
using SICore.Models;
using SIData;
using System.Collections.ObjectModel;
using System.Text;
using Utils;

namespace SICore;

// TODO: move all UI-related stuff to GameViewModel class

/// <summary>
/// Defines viewer data.
/// </summary>
public sealed class ViewerData : Data
{
    internal const int LockTimeoutMs = 5000;

    /// <summary>
    /// Allows to separate message handling and logic execution.
    /// </summary>
    internal Lock TaskLock { get; } = new Lock(nameof(TaskLock));

    private string _stageName = "";

    /// <summary>
    /// Human-readable game stage name.
    /// </summary>
    public string StageName
    {
        get => _stageName;
        set { if (_stageName != value) { _stageName = value; OnPropertyChanged(); } }
    }

    // TODO: maybe client logic should not rely on this property
    /// <summary>
    /// Current question type.
    /// </summary>
    public string? QuestionType { get; set; }

    internal int LastStakerIndex { get; set; } = -1;

    public string Name { get; internal set; }

    public ViewerAccount? Me
    {
        get
        {
            AllPersons.TryGetValue(Name, out var me);
            return me;
        }
    }

    public bool IsInfoInitialized { get; set; }

    private bool _isChatOpened = true;

    public bool IsChatOpened
    {
        get => _isChatOpened;
        set
        {
            if (_isChatOpened != value)
            {
                _isChatOpened = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Адрес изображения участника
    /// </summary>
    internal string Picture { get; set; }

    public string Sound { set => Host.PlaySound(value); }

    internal bool FalseStart { get; set; } = true;

    public PersonData PersonDataExtensions { get; private set; } = new();

    /// <summary>
    /// Defines time stamp when game buttons have been activated.
    /// </summary>
    public DateTimeOffset? TryStartTime { get; set; }

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

    private readonly PersonAccount _showMan = new(Constants.FreePlace, true, false, false) { IsShowman = true };

    /// <summary>
    /// Game showman.
    /// </summary>
    public PersonAccount ShowMan => _showMan;

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

        OnPropertyChanged(nameof(Me));
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
    public PersonAccount[] MainPersons
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

    /// <summary>
    /// Contains information about system errors in the game, which would be good to send to the author, but do not lead to a system crash.
    /// </summary>
    public StringBuilder SystemLog { get; } = new();

    public ViewerData(IGameHost gameHost) : base(gameHost)
    {

    }

    public string PackageId { get; internal set; }

    public int ButtonBlockingTime { get; internal set; } = 3;

    public string ThemeName { get; internal set; }

    public string ThemeComments { get; internal set; }

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
    /// External content that can be loaded only after user approval.
    /// </summary>
    public List<(string ContentType, string Uri)> ExternalContent { get; } = new();

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

    private int _roundIndex = -1;

    /// <summary>
    /// Current round index.
    /// </summary>
    public int RoundIndex 
    {
        get => _roundIndex;
        set
        {
            if (_roundIndex != value)
            {
                _roundIndex = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsAnswer { get; set; }
}
