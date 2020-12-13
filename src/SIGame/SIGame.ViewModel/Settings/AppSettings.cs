using SIData;
using SIGame.ViewModel;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace SIGame
{
    /// <summary>
    /// Настройки, которые пользователь может редактировать с помощью диалога настроек и возвращать в первоначальное состояние
    /// </summary>
    public sealed class AppSettings : AppSettingsCore
    {
        internal const int DefaultGameButtonKey2 = 119; // RightCtrl

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _logsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), CommonSettings.ManufacturerEn, CommonSettings.AppNameEn, CommonSettings.LogsFolderName);

        /// <summary>
        /// Папка логов
        /// </summary>
        [XmlAttribute]
        public string LogsFolder
        {
            get { return _logsFolder; }
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
            get { return _makeLogs; }
            set { if (_makeLogs != value) { _makeLogs = value; OnPropertyChanged(); } }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ThemeSettings _themeSettings = new ThemeSettings();

        /// <summary>
        /// Настройки темы
        /// </summary>
        public ThemeSettings ThemeSettings
        {
            get { return _themeSettings; }
            set { _themeSettings = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _gameButtonKey = DefaultGameButtonKey2;

        /// <summary>
        /// Клавиша игровой кнопки
        /// </summary>
        [XmlAttribute]
        [DefaultValue(DefaultGameButtonKey2)]
        public int GameButtonKey2
        {
            get { return _gameButtonKey; }
            set { _gameButtonKey = value; OnPropertyChanged(); }
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
        }

        internal void Set(AppSettings settings)
        {
            base.Set(settings);
            GameButtonKey2 = settings._gameButtonKey;
            MakeLogs = settings.MakeLogs;
            ThemeSettings = settings.ThemeSettings;
            TranslateGameToChat = settings._translateGameToChat;
            ShowBorderOnFalseStart = settings.ShowBorderOnFalseStart;
            // logsFolder не меняется
        }

        public AppSettingsCore ToAppSettingsCore()
        {
            var core = new AppSettingsCore();
            core.Set(this);
            return core;
        }
    }
}
