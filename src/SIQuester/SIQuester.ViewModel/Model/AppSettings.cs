using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace SIQuester.Model
{
    public sealed class AppSettings: INotifyPropertyChanged
    {
        public const string ProductName = "SIQuester";

        /// <summary>
        /// Используется ли версия Windows от Vista и выше
        /// </summary>
        public static readonly bool IsVistaOrLater = Environment.OSVersion.Version.Major >= 6;

        public static AppSettings Default { get; set; }

        private bool _searchForUpdates = true;

        [DefaultValue(true)]
        public bool SearchForUpdates
        {
            get { return _searchForUpdates; }
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

        [DefaultValue(false)]
        public bool AutomaticTextImport
        {
            get { return _automaticTextImport; }
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

        [DefaultValue(true)]
        public bool ChangePriceOnMove
        {
            get { return _changePriceOnMove; }
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
            get { return _createQuestionsWithTheme; }
            set
            {
                if (_createQuestionsWithTheme != value)
                {
                    _createQuestionsWithTheme = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isFirstRun = true;

        [DefaultValue(true)]
        public bool IsFirstRun
        {
            get { return _isFirstRun; }
            set
            {
                if (_isFirstRun != value)
                {
                    _isFirstRun = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _showToolTips = true;

        [DefaultValue(true)]
        public bool ShowToolTips
        {
            get { return _showToolTips; }
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

        [DefaultValue(100)]
        public int QuestionBase
        {
            get { return _questionBase; }
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
            get { return _autoSave; }
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
            get { return _delayedErrors; }
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

        public FileHistory History
        {
            get { return _history; }
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
            get { return _searchPath; }
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
            get { return _costSetters; }
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

        [DefaultValue(ViewMode.TreeFull)]
        public ViewMode View
        {
            get { return _view; }
            set
            {
                if (_view != value)
                {
                    _view = value;
                    OnPropertyChanged();
                }
            }
        }

        private FlatScale _flatScale = FlatScale.Theme;

        [DefaultValue(FlatScale.Theme)]
        public FlatScale FlatScale
        {
            get { return _flatScale; }
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
            get { return _showEditPanel; }
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
            get { return _edit; }
            set
            {
                if (_edit != value)
                {
                    _edit = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _alightAnswersRight = false;

        [DefaultValue(false)]
        public bool AlightAnswersRight
        {
            get { return _alightAnswersRight; }
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
            get { return _spellChecking; }
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
            get { return _fontSize; }
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _fontFamily = "Calibri";

        [DefaultValue("Calibri")]
        public string FontFamily
        {
            get => _fontFamily;
            set
            {
                if (_fontFamily != value && value != null)
                {
                    _fontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _removeLinks = true;

        /// <summary>
        /// Удалять медиа после удаления последней ссылки
        /// </summary>
        [DefaultValue(true)]
        public bool RemoveLinks
        {
            get { return _removeLinks; }
            set
            {
                if (_removeLinks != value)
                {
                    _removeLinks = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Загрузить пользовательские настройки
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static AppSettings Load(Stream stream)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(AppSettings));
                return (AppSettings)serializer.Deserialize(stream);
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

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

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
            _flatScale = defaultSettings._flatScale;
        }
    }
}
