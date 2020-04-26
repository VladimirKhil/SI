using System.ComponentModel;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Runtime.Serialization;
using SIEngine;
using System.Runtime.CompilerServices;

namespace SIData
{
    [DataContract]
    public class AppSettingsCore: IAppSettingsCore, INotifyPropertyChanged
    {
        public const int DefaultMultimediaPort = 6999;
        public const int DefaultReadingSpeed = 20;
        public const bool DefaultFalseStart = true;
        public const bool DefaultHintShowman = false;
        public const bool DefaultPartialText = false;
        public const bool DefaultOral = false;
        public const bool DefaultManaged = false;
        public const bool DefaultIgnoreWrong = false;
        public const GameModes DefaultGameMode = GameModes.Tv;
        public const int DefaultRandomRoundsCount = 3;
        public const int DefaultRandomThemesCount = 6;
        public const int DefaultRandomQuestionsBasePrice = 100;

        /// <summary>
        /// Настройки времени
        /// </summary>
        [DataMember]
        public TimeSettings TimeSettings { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _multimediaPort = DefaultMultimediaPort;

        /// <summary>
        /// Номер порта для мультимедиа-вопросов
        /// </summary>
        [XmlAttribute]
        [DefaultValue(DefaultMultimediaPort)]
        [DataMember]
        public int MultimediaPort
        {
            get { return _multimediaPort; }
            set { _multimediaPort = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _readingSpeed = DefaultReadingSpeed;

        /// <summary>
        /// Скорость чтения вопроса (символов в секунду)
        /// </summary>
        [XmlAttribute]
        [DefaultValue(DefaultReadingSpeed)]
        [DataMember]
        public int ReadingSpeed
        {
            get { return _readingSpeed; }
            set { _readingSpeed = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _falseStart = DefaultFalseStart;

        /// <summary>
        /// Игра с фальстартами
        /// </summary>
        [XmlAttribute]
        [DefaultValue(DefaultFalseStart)]
        [DataMember]
        public bool FalseStart
        {
            get { return _falseStart; }
            set { _falseStart = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _hintShowman = DefaultHintShowman;

        /// <summary>
        /// Игра с фальстартами
        /// </summary>
        [XmlAttribute]
        [DefaultValue(DefaultHintShowman)]
        [DataMember]
        public bool HintShowman
        {
            get { return _hintShowman; }
            set { _hintShowman = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _partialText = DefaultPartialText;

        /// <summary>
        /// Частичный текст
        /// </summary>
        [XmlAttribute]
        [DefaultValue(DefaultPartialText)]
        [DataMember]
        public bool PartialText
        {
            get { return _partialText; }
            set { _partialText = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _oral = DefaultOral;

        /// <summary>
        /// Устная игра
        /// </summary>
        [XmlAttribute]
        [DefaultValue(DefaultOral)]
        [DataMember]
        public bool Oral
        {
            get { return _oral; }
            set { _oral = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _managed = DefaultManaged;

        /// <summary>
        /// Управляемая игра
        /// </summary>
        [XmlAttribute]
        [DefaultValue(DefaultManaged)]
        [DataMember]
        public bool Managed
        {
            get { return _managed; }
            set { _managed = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _ignoreWrong = DefaultIgnoreWrong;

        /// <summary>
        /// Неправильный ответ не снимает очки
        /// </summary>
        [XmlAttribute]
        [DefaultValue(DefaultIgnoreWrong)]
        [DataMember]
        public bool IgnoreWrong
        {
            get { return _ignoreWrong; }
            set { _ignoreWrong = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private GameModes _gameMode = DefaultGameMode;

        /// <summary>
        /// Режим игры
        /// </summary>
        [XmlAttribute]
        [DefaultValue(DefaultGameMode)]
        [DataMember]
        public GameModes GameMode
        {
            get { return _gameMode; }
            set { _gameMode = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _customBackgroundUri = null;

        /// <summary>
        /// Настроенное фоновое изображение
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public string CustomBackgroundUri
        {
            get { return _customBackgroundUri; }
            set { _customBackgroundUri = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _randomRoundsCount = DefaultRandomRoundsCount;

        /// <summary>
        /// Число случайных раундов
        /// </summary>
        [XmlAttribute]
        [DefaultValue(DefaultRandomRoundsCount)]
        [DataMember]
        public int RandomRoundsCount
        {
            get { return _randomRoundsCount; }
            set { _randomRoundsCount = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _randomThemesCount = DefaultRandomThemesCount;

        /// <summary>
        /// Число случайных тем
        /// </summary>
        [XmlAttribute]
        [DefaultValue(DefaultRandomThemesCount)]
        [DataMember]
        public int RandomThemesCount
        {
            get { return _randomThemesCount; }
            set { _randomThemesCount = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _randomQuestionsBasePrice = DefaultRandomQuestionsBasePrice;

        /// <summary>
        /// Число случайных тем
        /// </summary>
        [XmlAttribute]
        [DefaultValue(DefaultRandomQuestionsBasePrice)]
        [DataMember]
        public int RandomQuestionsBasePrice
        {
            get { return _randomQuestionsBasePrice; }
            set { _randomQuestionsBasePrice = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Язык игры
        /// </summary>
        [XmlAttribute]
        [DataMember]
        [DefaultValue("ru-RU")]
        public string Culture { get; set; }

        public AppSettingsCore()
        {
            TimeSettings = new TimeSettings();
        }

        public AppSettingsCore(AppSettingsCore origin)
        {
            TimeSettings = origin.TimeSettings.Clone();

            _readingSpeed = origin._readingSpeed;
            _multimediaPort = origin._multimediaPort;
            _falseStart = origin._falseStart;
            _hintShowman = origin._hintShowman;
            _oral = origin._oral;
            _ignoreWrong = origin._ignoreWrong;
            _gameMode = origin._gameMode;
            _customBackgroundUri = origin._customBackgroundUri;

            _randomRoundsCount = origin._randomRoundsCount;
            _randomThemesCount = origin._randomThemesCount;
            _randomQuestionsBasePrice = origin._randomQuestionsBasePrice;

            Culture = origin.Culture;
        }

        public void Set(AppSettingsCore settings)
        {
            MultimediaPort = settings._multimediaPort;
            ReadingSpeed = settings._readingSpeed;
            TimeSettings = settings.TimeSettings;
            FalseStart = settings._falseStart;
            PartialText = settings.PartialText;
            Managed = settings.Managed;
            HintShowman = settings._hintShowman;
            Oral = settings._oral;
            _ignoreWrong = settings._ignoreWrong;
            _gameMode = settings._gameMode;
            _customBackgroundUri = settings._customBackgroundUri;

            _randomRoundsCount = settings._randomRoundsCount;
            _randomThemesCount = settings._randomThemesCount;
            _randomQuestionsBasePrice = settings._randomQuestionsBasePrice;
            Culture = settings.Culture;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
