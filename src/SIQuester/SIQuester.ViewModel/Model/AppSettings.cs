using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace SIQuester.Model;

/// <summary>
/// Defines application settings.
/// </summary>
public sealed class AppSettings : INotifyPropertyChanged
{
    /// <summary>
    /// Application name.
    /// </summary>
    public const string ProductName = "SIQuester";

    /// <summary>
    /// Name of the folder used to store simplified auto saves (packages without media).
    /// </summary>
    public const string AutoSaveSimpleFolderName = "AutoSaveNew";

    /// <summary>
    /// Name of the folder used to store unpacked media.
    /// </summary>
    public const string MediaFolderName = "Media";

    /// <summary>
    /// Name of the folder used to store temporary media files.
    /// </summary>
    public const string TempMediaFolderName = "TempMedia";

    /// <summary>
    /// Templates folder name.
    /// </summary>
    public const string TemplatesFolderName = "Templates";

    /// <summary>
    /// SIQ file extension.
    /// </summary>
    public const string SiqExtension = "siq";

    private const string DefaultFontFamily = "Calibri";

    private const int DefaultSelectOptionCount = 4;

    /// <summary>
    /// Auto-save interval.
    /// </summary>
    public static readonly TimeSpan AutoSaveInterval = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Is current Windows version a Windows Vista or a later version.
    /// </summary>
    public static readonly bool IsVistaOrLater = Environment.OSVersion.Version.Major >= 6;

    public static AppSettings Default { get; set; }

    private bool _searchForUpdates = true;

    /// <summary>
    /// Search and install updates automatically.
    /// </summary>
    [DefaultValue(true)]
    public bool SearchForUpdates
    {
        get => _searchForUpdates;
        set
        {
            if (_searchForUpdates != value)
            {
                _searchForUpdates = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _automaticTextImport = false;

    /// <summary>
    /// Detect text template on import automatically.
    /// </summary>
    [DefaultValue(false)]
    public bool AutomaticTextImport
    {
        get => _automaticTextImport;
        set
        {
            if (_automaticTextImport != value)
            {
                _automaticTextImport = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _changePriceOnMove = true;

    /// <summary>
    /// Recalculate question price after moving.
    /// </summary>
    [DefaultValue(true)]
    public bool ChangePriceOnMove
    {
        get => _changePriceOnMove;
        set
        {
            if (_changePriceOnMove != value)
            {
                _changePriceOnMove = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _createQuestionsWithTheme = false;

    [DefaultValue(false)]
    public bool CreateQuestionsWithTheme
    {
        get => _createQuestionsWithTheme;
        set
        {
            if (_createQuestionsWithTheme != value)
            {
                _createQuestionsWithTheme = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _showToolTips = true;

    [DefaultValue(true)]
    public bool ShowToolTips
    {
        get => _showToolTips;
        set
        {
            if (_showToolTips != value)
            {
                _showToolTips = value;
                OnPropertyChanged();
            }
        }
    }

    private int _questionBase = 100;

    /// <summary>
    /// Basic question price.
    /// </summary>
    [DefaultValue(100)]
    public int QuestionBase
    {
        get => _questionBase;
        set
        {
            if (_questionBase != value)
            {
                _questionBase = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _autoSave = true;

    [DefaultValue(true)]
    public bool AutoSave
    {
        get => _autoSave;
        set
        {
            if (_autoSave != value)
            {
                _autoSave = value;
                OnPropertyChanged();
            }
        }
    }

    private StringCollection _delayedErrors = new();

    public StringCollection DelayedErrors
    {
        get => _delayedErrors;
        set
        {
            if (_delayedErrors != value)
            {
                _delayedErrors = value;
                OnPropertyChanged();
            }
        }
    }

    private FileHistory _history = new();

    /// <summary>
    /// Defines a list of files that have been opened before.
    /// </summary>
    public FileHistory History
    {
        get => _history;
        set
        {
            if (_history != value)
            {
                _history = value;
                OnPropertyChanged();
            }
        }
    }

    private string _searchPath = "";

    [DefaultValue("")]
    public string SearchPath
    {
        get => _searchPath;
        set
        {
            if (_searchPath != value)
            {
                _searchPath = value;
                OnPropertyChanged();
            }
        }
    }

    private CostSetterList _costSetters = new();

    public CostSetterList CostSetters
    {
        get => _costSetters;
        set
        {
            if (_costSetters != value)
            {
                _costSetters = value;
                OnPropertyChanged();
            }
        }
    }

    private ViewMode _view = ViewMode.TreeFull;

    /// <summary>
    /// Document view mode.
    /// </summary>
    [DefaultValue(ViewMode.TreeFull)]
    public ViewMode View
    {
        get => _view;
        set
        {
            if (_view != value)
            {
                _view = value;
                OnPropertyChanged();
            }
        }
    }

    private FlatLayoutMode _flatLayoutMode = FlatLayoutMode.Table;

    /// <summary>
    /// Layout mode in flat view.
    /// </summary>
    [DefaultValue(FlatLayoutMode.Table)]
    public FlatLayoutMode FlatLayoutMode
    {
        get => _flatLayoutMode;
        set
        {
            if (_flatLayoutMode != value)
            {
                _flatLayoutMode = value;
                OnPropertyChanged();
            }
        }
    }

    private FlatScale _flatScale = FlatScale.Theme;

    [DefaultValue(FlatScale.Theme)]
    public FlatScale FlatScale
    {
        get => _flatScale;
        set
        {
            if (_flatScale != value)
            {
                _flatScale = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _showEditPanel = false;

    [DefaultValue(false)]
    public bool ShowEditPanel
    {
        get => _showEditPanel;
        set
        {
            if (_showEditPanel != value)
            {
                _showEditPanel = value;
                OnPropertyChanged();
            }
        }
    }

    private EditMode _edit = EditMode.None;

    [DefaultValue(EditMode.None)]
    public EditMode Edit
    {
        get => _edit;
        set
        {
            if (_edit != value)
            {
                _edit = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _alightAnswersRight = true;

    [DefaultValue(true)]
    public bool AlightAnswersRight
    {
        get => _alightAnswersRight;
        set
        {
            if (_alightAnswersRight != value)
            {
                _alightAnswersRight = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _spellChecking = true;

    [DefaultValue(true)]
    public bool SpellChecking
    {
        get => _spellChecking;
        set
        {
            if (_spellChecking != value)
            {
                _spellChecking = value;
                OnPropertyChanged();
            }
        }
    }

    private int _fontSize = 15;

    [DefaultValue(15)]
    public int FontSize
    {
        get => _fontSize;
        set
        {
            if (_fontSize != value)
            {
                _fontSize = value;
                OnPropertyChanged();
            }
        }
    }

    private string _fontFamily = DefaultFontFamily;

    [DefaultValue(DefaultFontFamily)]
    public string FontFamily
    {
        get => _fontFamily;
        set
        {
            if (_fontFamily != value && value != null) // null could be provided by the WPF binding
            {
                _fontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _removeLinks = true;

    /// <summary>
    /// Remove media file after the removal of last link to it.
    /// </summary>
    [DefaultValue(true)]
    public bool RemoveLinks
    {
        get => _removeLinks;
        set
        {
            if (_removeLinks != value)
            {
                _removeLinks = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _checkFileSize = true;

    /// <summary>
    /// Enables check that file size is greater than the default game server limit (100 MB).
    /// </summary>
    [DefaultValue(true)]
    public bool CheckFileSize
    {
        get => _checkFileSize;
        set
        {
            if (_checkFileSize != value)
            {
                _checkFileSize = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _language = null;

    /// <summary>
    /// Application language.
    /// </summary>
    public string? Language
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

    private int _selectOptionCount = DefaultSelectOptionCount;

    /// <summary>
    /// Default option count created with select answer.
    /// </summary>
    [DefaultValue(DefaultSelectOptionCount)]
    public int SelectOptionCount
    {
        get => _selectOptionCount;
        set
        {
            if (_selectOptionCount != value && value > 1)
            {
                _selectOptionCount = value;
                OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Maximum recommended image size.
    /// </summary>
    public int MaxImageSizeKb { get; } = 1 * 1024;

    /// <summary>
    /// Maximum recommended audio size.
    /// </summary>
    public int MaxAudioSizeKb { get; } = 5 * 1024;

    /// <summary>
    /// Maximum recommended video size.
    /// </summary>
    public int MaxVideoSizeKb { get; } = 10 * 1024;

    /// <summary>
    /// Maximum recommended html size.
    /// </summary>
    public int MaxHtmlSizeKb { get; } = 1 * 1024;

    /// <summary>
    /// Loads settings from stream.
    /// </summary>
    public static AppSettings Load(Stream stream)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(AppSettings));
            return (AppSettings?)serializer.Deserialize(stream) ?? Create();
        }
        catch { }

        return Create();
    }

    public static AppSettings Create()
    {
        var newSettings = new AppSettings();
        newSettings.Initialize();
        return newSettings;
    }

    internal void Initialize()
    {
        _costSetters.Add(new CostSetter(10));
        _costSetters.Add(new CostSetter(20));
        _costSetters.Add(new CostSetter(100));
        _costSetters.Add(new CostSetter(200));
        _costSetters.Add(new CostSetter(300));
    }

    public void Save(Stream stream)
    {
        var serializer = new XmlSerializer(typeof(AppSettings));
        serializer.Serialize(stream, this);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void Reset()
    {
        var defaultSettings = new AppSettings();
        defaultSettings.Initialize();

        AlightAnswersRight = defaultSettings.AlightAnswersRight;
        AutomaticTextImport = defaultSettings.AutomaticTextImport;
        AutoSave = defaultSettings.AutoSave;
        ChangePriceOnMove = defaultSettings.ChangePriceOnMove;

        _costSetters.Clear();

        foreach (var item in defaultSettings.CostSetters)
        {
            _costSetters.Add(item);
        }

        CreateQuestionsWithTheme = defaultSettings.CreateQuestionsWithTheme;
        FontFamily = defaultSettings.FontFamily;
        FontSize = defaultSettings.FontSize;
        QuestionBase = defaultSettings.QuestionBase;
        SearchForUpdates = defaultSettings.SearchForUpdates;
        RemoveLinks = defaultSettings.RemoveLinks;
        CheckFileSize = defaultSettings.CheckFileSize;
        _flatScale = defaultSettings._flatScale;
        FlatLayoutMode = defaultSettings.FlatLayoutMode;
        SelectOptionCount = DefaultSelectOptionCount;
    }
}
