using System;
using System.ComponentModel;
using System.Collections.Specialized;
using System.IO;
using System.Xml.Serialization;
using System.Threading;
using System.IO.IsolatedStorage;
using System.Diagnostics;
using SIGame.ViewModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using SICore;

namespace SIGame
{
    /// <summary>
    /// Пользовательские настройки игры
    /// </summary>
    public sealed class UserSettings: INotifyPropertyChanged
    {
        public static UserSettings Default { get; set; }

        #region Settings

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private GameSettings _gameSettings = new GameSettings();

        public GameSettings GameSettings
        {
            get { return _gameSettings; }
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
            get { return _packages; }
            set { _packages = value; OnPropertyChanged(); }
        }

        public List<string> PackageHistory { get; set; } = new List<string>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _sound = true;

        [XmlAttribute]
        [DefaultValue(true)]
        public bool Sound
        {
            get { return _sound; }
            set { _sound = value; OnPropertyChanged(); }
        }

        private double _volume = 25;

        /// <summary>
        /// Громкость звука
        /// </summary>
        public double Volume
        {
            get { return _volume; }
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

        public event Action<double> VolumeChanged;

        private bool _readQuestions = false;

        [XmlAttribute]
        [DefaultValue(false)]
        public bool ReadQuestions
        {
            get { return _readQuestions; }
            set
            {
                _readQuestions = value;
                OnPropertyChanged();
            }
        }

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
            get { return _fullScreen; }
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
            get { return _sendReport; }
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
            get { return _loadExternalMedia; }
            set { if (_loadExternalMedia != value) { _loadExternalMedia = value; OnPropertyChanged(); } }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _searchForUpdates =
#if DEBUG
        false;
#else
        true;
#endif

        [XmlAttribute]
        [DefaultValue(true)]
        public bool SearchForUpdates
        {
            get { return _searchForUpdates; }
            set { if (_searchForUpdates != value) { _searchForUpdates = value; OnPropertyChanged(); } }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ConnectionData _connectionData = new ConnectionData();

        public ConnectionData ConnectionData
        {
            get { return _connectionData; }
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
            get { return _restriction; }
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
            get { return _publisher; }
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
            get { return _tag; }
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
            get { return _showRunning; }
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
            get { return _gamesFilter; }
            set { if (_gamesFilter != value) { _gamesFilter = value; OnPropertyChanged(); } }
        }

        private string _language = null;

        /// <summary>
        /// Язык программы
        /// </summary>
        public string Language
        {
            get { return _language; }
            set
            {
                if (_language != value)
                {
                    _language = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        public void Save(Stream stream, XmlSerializer serializer = null)
        {
            if (serializer == null)
                serializer = new XmlSerializer(typeof(UserSettings));

            serializer.Serialize(stream, this);
        }

        /// <summary>
        /// Загрузить пользовательские настройки
        /// </summary>
        /// <returns></returns>
        public static UserSettings LoadOld(string configFileName)
        {
            using (var file = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                if (file.FileExists(configFileName) && Monitor.TryEnter(configFileName, 2000))
                {
                    try
                    {
                        using (var stream = file.OpenFile(configFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            return Load(stream);
                        }
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

        public static UserSettings Load(Stream stream, XmlSerializer serializer = null)
        {
            if (serializer == null)
                serializer = new XmlSerializer(typeof(UserSettings));

            var settings = (UserSettings)serializer.Deserialize(stream);

            if (settings.GameSettings.AppSettings.CustomBackgroundUri != null)
            {
                settings.GameSettings.AppSettings.ThemeSettings.CustomBackgroundUri = settings.GameSettings.AppSettings.CustomBackgroundUri;
                settings.GameSettings.AppSettings.CustomBackgroundUri = null;
            }

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
            ReadQuestions = settings.ReadQuestions;

            GameSettings.AppSettings.Set(settings.GameSettings.AppSettings);
            GameSettings.NetworkPort = settings.GameSettings.NetworkPort;
            GameSettings.RandomSpecials = settings.GameSettings.RandomSpecials;
            GameSettings.AllowViewers = settings.GameSettings.AllowViewers;

            return settings;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
