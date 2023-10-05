using SImulator.ViewModel.Core;
using SIUI.ViewModel.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace SImulator.ViewModel.Model;

/// <summary>
/// Defines application settings.
/// </summary>
public sealed class AppSettings : INotifyPropertyChanged
{
    public const string AppName = "SImulator";

    #region Settings

    private const int RoundTimeDefaultValue = 600;

    private int _roundTime = RoundTimeDefaultValue;

    /// <summary>
    /// Maximum round time.
    /// </summary>
    [DefaultValue(RoundTimeDefaultValue)]
    public int RoundTime
    {
        get => _roundTime;
        set
        {
            if (_roundTime != value && value > 0)
            {
                _roundTime = value;
                OnPropertyChanged();
            }
        }
    }

    private const int ThinkingTimeDefaultValue = 5;

    private int _thinkingTime = ThinkingTimeDefaultValue;

    /// <summary>
    /// Time for pressing button.
    /// </summary>
    [DefaultValue(ThinkingTimeDefaultValue)]
    public int ThinkingTime
    {
        get => _thinkingTime;
        set
        {
            if (_thinkingTime != value && value > 0)
            {
                _thinkingTime = value;
                OnPropertyChanged();
            }
        }
    }

    private const int ThinkingTime2DefaultValue = 15;

    private int _thinkingTime2 = ThinkingTime2DefaultValue;

    /// <summary>
    /// Time for thinking on question.
    /// </summary>
    [DefaultValue(ThinkingTime2DefaultValue)]
    public int ThinkingTime2
    {
        get => _thinkingTime2;
        set
        {
            if (_thinkingTime2 != value && value > 0)
            {
                _thinkingTime2 = value;
                OnPropertyChanged();
            }
        }
    }

    private const int SpecialQuestionThinkingTimeDefaultValue = 30;

    private int _specialQuestionThinkingTime = SpecialQuestionThinkingTimeDefaultValue;

    /// <summary>
    /// Time for thinking on special question.
    /// </summary>
    [DefaultValue(SpecialQuestionThinkingTimeDefaultValue)]
    public int SpecialQuestionThinkingTime
    {
        get => _specialQuestionThinkingTime;
        set
        {
            if (_specialQuestionThinkingTime != value && value > 0)
            {
                _specialQuestionThinkingTime = value;
                OnPropertyChanged();
            }
        }
    }

    private const int FinalQuestionThinkingTimeDefaultValue = 30;

    private int _finalQuestionThinkingTime = FinalQuestionThinkingTimeDefaultValue;

    /// <summary>
    /// Time for thinking on final question.
    /// </summary>
    [DefaultValue(FinalQuestionThinkingTimeDefaultValue)]
    public int FinalQuestionThinkingTime
    {
        get => _finalQuestionThinkingTime;
        set
        {
            if (_finalQuestionThinkingTime != value && value > 0)
            {
                _finalQuestionThinkingTime = value;
                OnPropertyChanged();
            }
        }
    }

    private int _screenNumber = 0;

    [DefaultValue(0)]
    public int ScreenNumber
    {
        get => _screenNumber;
        set
        {
            if (_screenNumber != value)
            {
                _screenNumber = value;
                OnPropertyChanged();
            }
        }
    }

    private Settings _siUISettings = new Settings();

    public Settings SIUISettings
    {
        get => _siUISettings;
        set
        {
            if (_siUISettings != value && value != null)
            {
                _siUISettings = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _dropStatsOnBack = true;

    /// <summary>
    /// Откатывать статистику при возврате
    /// </summary>
    [DefaultValue(true)]
    public bool DropStatsOnBack
    {
        get => _dropStatsOnBack;
        set
        {
            if (_dropStatsOnBack != value)
            {
                _dropStatsOnBack = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _showRight = false;

    /// <summary>
    /// Show right answers on the screen.
    /// </summary>
    [DefaultValue(false)]
    public bool ShowRight
    {
        get => _showRight;
        set
        {
            if (_showRight != value)
            {
                _showRight = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _showPlayers = false;

    /// <summary>
    /// Show players and scores.
    /// </summary>
    [DefaultValue(false)]
    public bool ShowPlayers
    {
        get => _showPlayers;
        set { if (_showPlayers != value) { _showPlayers = value; OnPropertyChanged(); } }
    }

    private bool _showTableCaption = true;

    /// <summary>
    /// Show table caption on the screen.
    /// </summary>
    [DefaultValue(true)]
    public bool ShowTableCaption
    {
        get => _showTableCaption;
        set
        {
            if (_showTableCaption != value)
            {
                _showTableCaption = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _showQuestionBorder = true;

    /// <summary>
    /// Show border around question when the players could press the button.
    /// </summary>
    [DefaultValue(true)]
    public bool ShowQuestionBorder
    {
        get => _showQuestionBorder;
        set
        {
            if (_showQuestionBorder != value)
            {
                _showQuestionBorder = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _substractOnWrong = true;

    /// <summary>
    /// Subtract points for wrong answer.
    /// </summary>
    [DefaultValue(true)]
    public bool SubstractOnWrong
    {
        get => _substractOnWrong;
        set
        {
            if (_substractOnWrong != value)
            {
                _substractOnWrong = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _playSpecials = true;

    /// <summary>
    /// Play special questions in classic game mode.
    /// </summary>
    [DefaultValue(true)]
    public bool PlaySpecials
    {
        get => _playSpecials;
        set
        {
            if (_playSpecials != value)
            {
                _playSpecials = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _playSounds = true;

    /// <summary>
    /// Play application sounds.
    /// </summary>
    [DefaultValue(true)]
    public bool PlaySounds
    {
        get => _playSounds;
        set
        {
            if (_playSounds != value)
            {
                _playSounds = value;
                OnPropertyChanged();
            }
        }
    }    

    private GameModes _gameMode = GameModes.Tv;

    /// <summary>
    /// Default game mode.
    /// </summary>
    [DefaultValue(GameModes.Tv)]
    public GameModes GameMode
    {
        get => _gameMode;
        set
        {
            if (_gameMode != value)
            {
                _gameMode = value;
                OnPropertyChanged();
            }
        }
    }

    private string _videoUrl = "";

    /// <summary>
    /// Uri of video played at the beginning of the game.
    /// </summary>
    [DefaultValue("")]
    public string VideoUrl
    {
        get => _videoUrl;
        set
        {
            if (_videoUrl != value)
            {
                _videoUrl = value;
                OnPropertyChanged();
            }
        }
    }

    private string _restriction = "12+";

    /// <summary>
    /// Default package age restriction.
    /// </summary>
    [DefaultValue("12+")]
    public string Restriction
    {
        get => _restriction;
        set
        {
            if (_restriction != value)
            {
                _restriction = value;
                OnPropertyChanged();
            }
        }
    }

    private ObservableCollection<string> _recent = new();

    /// <summary>
    /// Played package files history.
    /// </summary>
    public ObservableCollection<string> Recent
    {
        get => _recent;
        set
        {
            if (_recent != value)
            {
                _recent = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _falseStart = true;

    /// <summary>
    /// Игра с фальстартами
    /// </summary>
    [DefaultValue(true)]
    public bool FalseStart
    {
        get => _falseStart;
        set
        {
            if (_falseStart != value)
            {
                _falseStart = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _falseStartMultimedia = true;

    /// <summary>
    /// Use false starts while playing multimedia questions.
    /// </summary>
    [DefaultValue(true)]
    public bool FalseStartMultimedia
    {
        get => _falseStartMultimedia;
        set
        {
            if (_falseStartMultimedia != value)
            {
                _falseStartMultimedia = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _showTextNoFalstart = false;

    /// <summary>
    /// Показывать текст вопросов
    /// </summary>
    [DefaultValue(false)]
    public bool ShowTextNoFalstart
    {
        get => _showTextNoFalstart;
        set
        {
            if (_showTextNoFalstart != value)
            {
                _showTextNoFalstart = value;
                OnPropertyChanged();
            }
        }
    }

    private PlayerKeysModes _usePlayersKeys = PlayerKeysModes.None;

    [DefaultValue(PlayerKeysModes.None)]
    public PlayerKeysModes UsePlayersKeys
    {
        get => _usePlayersKeys;
        set
        {
            if (_usePlayersKeys != value)
            {
                _usePlayersKeys = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _signalsAfterTimer = false;

    [DefaultValue(false)]
    public bool SignalsAfterTimer
    {
        get => _signalsAfterTimer;
        set
        {
            if (_signalsAfterTimer != value)
            {
                _signalsAfterTimer = value;
                OnPropertyChanged();
            }
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private double _blockingTime = 3.0;

    [DefaultValue(3.0)]
    public double BlockingTime
    {
        get => _blockingTime;
        set
        {
            if (_blockingTime != value)
            {
                _blockingTime = value;
                OnPropertyChanged();
            }
        }
    }

    private string _comPort = "";

    /// <summary>
    /// Используемый COM-порт
    /// </summary>
    [DefaultValue("")]
    public string ComPort
    {
        get => _comPort;
        set
        {
            if (_comPort != value)
            {
                _comPort = value;
                OnPropertyChanged();
            }
        }
    }

    private ErrorInfoList _delayedErrors = new();

    public ErrorInfoList DelayedErrors
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

    private KeyCollection2 _playerKeys2 = new();

    [XmlIgnore]
    public KeyCollection2 PlayerKeys2
    {
        get => _playerKeys2;
        set
        {
            if (_playerKeys2 != value)
            {
                _playerKeys2 = value;
                OnPropertyChanged();
            }
        }
    }

    private List<int> _playerKeysPublic = new();
    
    public List<int> PlayerKeysPublic
    {
        get => _playerKeysPublic;
        set { _playerKeysPublic = value; OnPropertyChanged(); }
    }

    private PlayersViewMode _playersView = PlayersViewMode.Hidden;

    [DefaultValue(PlayersViewMode.Hidden)]
    public PlayersViewMode PlayersView
    {
        get => _playersView;
        set
        {
            if (_playersView != value)
            {
                _playersView = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _saveLogs = false;

    [DefaultValue(false)]
    public bool SaveLogs
    {
        get => _saveLogs;
        set
        {
            if (_saveLogs != value)
            {
                _saveLogs = value;
                OnPropertyChanged();
            }
        }
    }

    private string _logsFolder;

    public string LogsFolder
    {
        get => _logsFolder;
        set
        {
            if (_logsFolder != value)
            {
                _logsFolder = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _endQuestionOnRightAnswer = true;

    [DefaultValue(true)]
    public bool EndQuestionOnRightAnswer
    {
        get => _endQuestionOnRightAnswer;
        set
        {
            if (_endQuestionOnRightAnswer != value)
            {
                _endQuestionOnRightAnswer = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _automaticGame = false;

    [DefaultValue(false)]
    public bool AutomaticGame
    {
        get { return _automaticGame; }
        set
        {
            if (_automaticGame != value)
            {
                _automaticGame = value;
                OnPropertyChanged();
            }
        }
    }

    private int _webPort = 80;

    /// <summary>
    /// Имя порта для веб-доступа
    /// </summary>
    [DefaultValue(80)]
    public int WebPort
    {
        get => _webPort;
        set
        {
            if (_webPort != value)
            {
                _webPort = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _showLostButtonPlayers = false;

    /// <summary>
    /// Show names of players who lost the buttons.
    /// </summary>
    [DefaultValue(false)]
    public bool ShowLostButtonPlayers
    {
        get => _showLostButtonPlayers;
        set
        {
            if (_showLostButtonPlayers != value)
            {
                _showLostButtonPlayers = value;
                OnPropertyChanged();
            }
        }
    }

    public SoundsSettings Sounds { get; set; } = new SoundsSettings();

    public SpecialsAliases SpecialsAliases { get; set; } = new SpecialsAliases();

    #endregion

    public void Save(Stream stream, XmlSerializer serializer = null)
    {
        _playerKeysPublic = new List<int>(_playerKeys2.Cast<int>());
        if (serializer == null)
            serializer = new XmlSerializer(typeof(AppSettings));

        serializer.Serialize(stream, this);
    }

    /// <summary>
    /// Загрузить пользовательские настройки
    /// </summary>
    public static AppSettings Load(Stream stream, XmlSerializer serializer = null)
    {
        if (serializer == null)
            serializer = new XmlSerializer(typeof(AppSettings));

        var settings = (AppSettings)serializer.Deserialize(stream);
        settings._playerKeys2 = new KeyCollection2(settings._playerKeysPublic.Cast<GameKey>());

        return settings;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;
}
