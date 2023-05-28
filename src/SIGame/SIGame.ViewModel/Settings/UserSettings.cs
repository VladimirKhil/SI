using SICore;
using SIGame.ViewModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace SIGame;

/// <summary>
/// Provides user-level app settings.
/// </summary>
public sealed class UserSettings : INotifyPropertyChanged
{
    public static UserSettings Default { get; set; }

    #region Settings

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private GameSettings _gameSettings = new();

    public GameSettings GameSettings
    {
        get => _gameSettings;
        set 
        {
            _gameSettings = value; 
            OnPropertyChanged();
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private StringCollection _packages = new StringCollection();

    public StringCollection Packages
    {
        get => _packages;
        set { _packages = value; OnPropertyChanged(); }
    }

    public List<string> PackageHistory { get; set; } = new List<string>();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _sound = true;

    [XmlAttribute]
    [DefaultValue(true)]
    public bool Sound
    {
        get => _sound;
        set { _sound = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _mainMenuSound = false;

    [XmlAttribute]
    [DefaultValue(false)]
    public bool MainMenuSound
    {
        get => _mainMenuSound;
        set { _mainMenuSound = value; OnPropertyChanged(); }
    }

    private double _volume = 25;

    /// <summary>
    /// Громкость звука
    /// </summary>
    public double Volume
    {
        get => _volume;
        set
        {
            if (_volume != value && value > 0 && value <= 100)
            {
                var oldValue = _volume;
                _volume = value;
                VolumeChanged?.Invoke(_volume / oldValue);
            }
        }
    }

    public event Action<double>? VolumeChanged;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _fullScreen = 
#if DEBUG
    false;
#else
    true;
#endif

    /// <summary>
    /// Полноэкранный режим
    /// </summary>
    [XmlAttribute]
    [DefaultValue(true)]
    public bool FullScreen
    {
        get => _fullScreen;
        set { if (_fullScreen != value) { _fullScreen = value; OnPropertyChanged(); } }
    }
    
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _sendReport = true;

    /// <summary>
    /// Отправлять отчёт об игре
    /// </summary>
    [XmlAttribute]
    [DefaultValue(true)]
    public bool SendReport
    {
        get => _sendReport;
        set { if (_sendReport != value) { _sendReport = value; OnPropertyChanged(); } }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _loadExternalMedia = false;

    /// <summary>
    /// Загружать медиа по внешним ссылкам
    /// </summary>
    [XmlAttribute]
    [DefaultValue(false)]
    public bool LoadExternalMedia
    {
        get => _loadExternalMedia;
        set { if (_loadExternalMedia != value) { _loadExternalMedia = value; OnPropertyChanged(); } }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _searchForUpdates = true;

    [XmlAttribute]
    [DefaultValue(true)]
    public bool SearchForUpdates
    {
        get => _searchForUpdates;
        set { if (_searchForUpdates != value) { _searchForUpdates = value; OnPropertyChanged(); } }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private ConnectionData _connectionData = new();

    public ConnectionData ConnectionData
    {
        get => _connectionData;
        set { _connectionData = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _restriction = "12+";

    /// <summary>
    /// Ограничение на пакеты
    /// </summary>
    [XmlAttribute]
    [DefaultValue("12+")]
    public string Restriction
    {
        get => _restriction;
        set { _restriction = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _publisher = null;

    /// <summary>
    /// Издатель
    /// </summary>
    [XmlAttribute]
    public string Publisher
    {
        get => _publisher;
        set { _publisher = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _tag = null;

    /// <summary>
    /// Тематика
    /// </summary>
    [XmlAttribute]
    public string Tag
    {
        get => _tag;
        set { _tag = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _showRunning = false;

    /// <summary>
    /// Показывать только запущенные игры
    /// </summary>
    [XmlAttribute]
    [DefaultValue(false)]
    public bool ShowRunning
    {
        get => _showRunning;
        set { if (_showRunning != value) { _showRunning = value; OnPropertyChanged(); } }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private GamesFilter _gamesFilter = GamesFilter.NoFilter;

    /// <summary>
    /// Показывать только запущенные игры
    /// </summary>
    [XmlAttribute]
    [DefaultValue(GamesFilter.NoFilter)]
    public GamesFilter GamesFilter
    {
        get => _gamesFilter;
        set { if (_gamesFilter != value) { _gamesFilter = value; OnPropertyChanged(); } }
    }

    private string _language = null;

    /// <summary>
    /// Язык программы
    /// </summary>
    public string Language
    {
        get => _language;
        set
        {
            if (_language != value)
            {
                _language = value;
                OnPropertyChanged();
            }
        }
    }

    [XmlIgnore]
    public bool UseSignalRConnection { get; set; }

    #endregion

    public void Save(Stream stream, XmlSerializer? serializer = null)
    {
        serializer ??= new XmlSerializer(typeof(UserSettings));
        serializer.Serialize(stream, this);
    }

    /// <summary>
    /// Загрузить пользовательские настройки
    /// </summary>
    /// <returns></returns>
    public static UserSettings? LoadOld(string configFileName)
    {
        using (var file = IsolatedStorageFile.GetUserStoreForAssembly())
        {
            if (file.FileExists(configFileName) && Monitor.TryEnter(configFileName, 2000))
            {
                try
                {
                    using var stream = file.OpenFile(configFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    return Load(stream);
                }
                catch { }
                finally
                {
                    Monitor.Exit(configFileName);
                }
            }
        }

        return null;
    }

    public static UserSettings? Load(Stream stream, XmlSerializer? serializer = null)
    {
        serializer ??= new XmlSerializer(typeof(UserSettings));

        var settings = (UserSettings?)serializer.Deserialize(stream);

        return settings;
    }

    internal UserSettings LoadFrom(Stream stream)
    {
        var settings = Load(stream);

        ConnectionData = settings.ConnectionData;
        FullScreen = settings.FullScreen;
        SearchForUpdates = settings.SearchForUpdates;
        SendReport = settings.SendReport;
        Sound = settings.Sound;
        Volume = settings.Volume;
        LoadExternalMedia = settings.LoadExternalMedia;

        GameSettings.AppSettings.Set(settings.GameSettings.AppSettings);
        GameSettings.NetworkPort = settings.GameSettings.NetworkPort;
        GameSettings.RandomSpecials = settings.GameSettings.RandomSpecials;
        GameSettings.IsPrivate = settings.GameSettings.IsPrivate;
        GameSettings.IsAutomatic = settings.GameSettings.IsAutomatic;

        return settings;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;
}
