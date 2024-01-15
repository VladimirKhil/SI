using SIData;
using SIGame.ViewModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace SIGame;

/// <summary>
/// Extends <see cref="AppSettingsCore" /> with app settings which do not affect game server but could be reset to defaults.
/// </summary>
public sealed class AppSettings : AppSettingsCore
{
    internal const int DefaultGameButtonKey2 = 119; // RightCtrl
    internal const int DefaultMoveNextKey = 25; // Right

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _logsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), CommonSettings.ManufacturerEn, CommonSettings.AppNameEn, CommonSettings.LogsFolderName);

    /// <summary>
    /// Папка логов
    /// </summary>
    [XmlAttribute]
    public string LogsFolder
    {
        get => _logsFolder;
        set { _logsFolder = value; OnPropertyChanged(); }
    }

    private bool _makeLogs = true;

    /// <summary>
    /// Вести ли логи
    /// </summary>
    [XmlAttribute]
    [DefaultValue(true)]
    public bool MakeLogs
    {
        get => _makeLogs;
        set { if (_makeLogs != value) { _makeLogs = value; OnPropertyChanged(); } }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private ThemeSettings _themeSettings = new();

    /// <summary>
    /// Application theme settings.
    /// </summary>
    public ThemeSettings ThemeSettings
    {
        get => _themeSettings;
        set { _themeSettings = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private int _gameButtonKey = DefaultGameButtonKey2;

    /// <summary>
    /// Game button key.
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultGameButtonKey2)]
    public int GameButtonKey2
    {
        get => _gameButtonKey;
        set { _gameButtonKey = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private int _moveNextKey = DefaultMoveNextKey;

    /// <summary>
    /// Move next key.
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultMoveNextKey)]
    public int MoveNextKey
    {
        get => _moveNextKey;
        set { _moveNextKey = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _translateGameToChat = false;

    /// <summary>
    /// Транслировать ли игру в чат
    /// </summary>
    [XmlAttribute]
    [DefaultValue(false)]
    public bool TranslateGameToChat
    {
        get { return _translateGameToChat; }
        set { _translateGameToChat = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _bindNextButton = true;

    /// <summary>
    /// Should the next button be bound to keyboard.
    /// </summary>
    [XmlAttribute]
    [DefaultValue(true)]
    public bool BindNextButton
    {
        get => _bindNextButton;
        set { _bindNextButton = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _attachContentToTable = true;

    /// <summary>
    /// Could the game attach content to game table.
    /// </summary>
    [XmlAttribute]
    [DefaultValue(true)]
    public bool AttachContentToTable
    {
        get => _attachContentToTable;
        set { _attachContentToTable = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _areAnswersShown = true;

    /// <summary>
    /// Показывать ли ответы в окне принятия решений ведущим
    /// </summary>
    [XmlAttribute]
    [DefaultValue(true)]
    public bool AreAnswersShown
    {
        get { return _areAnswersShown; }
        set { _areAnswersShown = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _isChatShown = true;

    /// <summary>
    /// Показывать ли игровой чат в лобби
    /// </summary>
    [XmlAttribute]
    [DefaultValue(true)]
    public bool IsChatShown
    {
        get { return _isChatShown; }
        set { _isChatShown = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _showBorderOnFalseStart = true;

    /// <summary>
    /// Показывать рамку при игре с фальстартами
    /// </summary>
    [XmlAttribute]
    [DefaultValue(true)]
    public bool ShowBorderOnFalseStart
    {
        get { return _showBorderOnFalseStart; }
        set { if (_showBorderOnFalseStart != value) { _showBorderOnFalseStart = value; OnPropertyChanged(); } }
    }

    public AppSettings()
    {
        MakeLogs = true;
    }

    public AppSettings(AppSettings origin) : base(origin)
    {
        _logsFolder = origin._logsFolder;
        _gameButtonKey = origin._gameButtonKey;
        _themeSettings.Initialize(origin._themeSettings);
        MakeLogs = origin.MakeLogs;
        TranslateGameToChat = origin._translateGameToChat;
        ShowBorderOnFalseStart = origin.ShowBorderOnFalseStart;
        BindNextButton = origin.BindNextButton;
        AttachContentToTable = origin.AttachContentToTable;
    }

    internal void Set(AppSettings settings)
    {
        base.Set(settings);
        GameButtonKey2 = settings._gameButtonKey;
        MakeLogs = settings.MakeLogs;
        ThemeSettings = settings.ThemeSettings;
        TranslateGameToChat = settings._translateGameToChat;
        ShowBorderOnFalseStart = settings.ShowBorderOnFalseStart;
        BindNextButton = settings.BindNextButton;
        AttachContentToTable = settings.AttachContentToTable;
        // logsFolder is not changed
    }

    public AppSettingsCore ToAppSettingsCore()
    {
        var core = new AppSettingsCore();
        core.Set(this);
        return core;
    }
}
