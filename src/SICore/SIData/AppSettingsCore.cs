using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace SIData;

/// <inheritdoc cref="IAppSettingsCore" />
public class AppSettingsCore : IAppSettingsCore, INotifyPropertyChanged
{
    public const int DefaultMultimediaPort = 6999;
    public const int DefaultReadingSpeed = 20;
    public const bool DefaultFalseStart = true;
    public const bool DefaultHintShowman = false;
    public const bool DefaultPartialText = false;
    public const bool DefaultPartialImages = false;
    public const bool DefaultPlayAllQuestionsInFinalRound = false;
    public const bool DefaultAllowEveryoneToPlayHiddenStakes = true;
    public const bool DefaultOral = false;
    public const bool DefaultOralPlayersActions = true;
    public const bool DefaultManaged = false;
    public const PenaltyType DefaultQuestionWithButtonPenalty = PenaltyType.SubtractPoints;
    public const PenaltyType DefaultQuestionForYourselfPenalty = PenaltyType.None;
    public const PenaltyType DefaultQuestionForAllPenalty = PenaltyType.SubtractPoints;
    public const int DefaultQuestionForYourselfFactor = 2;
    public const ButtonPressMode DefaultButtonPressMode = ButtonPressMode.RandomWithinInterval;
    public const bool DefaultPreloadRoundContent = true;
    public const GameModes DefaultGameMode = GameModes.Tv;
    public const int DefaultRandomRoundsCount = 3;
    public const int DefaultRandomThemesCount = 6;
    public const int DefaultRandomQuestionsBasePrice = 100;
    public const bool DefaultUseApellations = true;
    public const bool DefaultDisplayAnswerOptionsOneByOne = true;
    public const bool DefaultDisplayAnswerOptionsLabels = true;

    /// <summary>
    /// Time settings.
    /// </summary>
    public TimeSettings TimeSettings { get; set; } = new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private int _multimediaPort = DefaultMultimediaPort;

    /// <summary>
    /// Номер порта для мультимедиа-вопросов
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultMultimediaPort)]
    public int MultimediaPort
    {
        get => _multimediaPort;
        set
        {
            if (_multimediaPort != value)
            {
                _multimediaPort = value;
                OnPropertyChanged();
            }
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private int _readingSpeed = DefaultReadingSpeed;

    /// <summary>
    /// Text reading speed (characters per second).
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultReadingSpeed)]
    public int ReadingSpeed
    {
        get => _readingSpeed;
        set { if (_readingSpeed != value) { _readingSpeed = value; OnPropertyChanged(); } }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _falseStart = DefaultFalseStart;

    /// <summary>
    /// False start game flag.
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultFalseStart)]
    public bool FalseStart
    {
        get => _falseStart;
        set { _falseStart = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _hintShowman = DefaultHintShowman;

    /// <summary>
    /// Send right answer to showman when the question starts.
    /// </summary>

    [XmlAttribute]
    [DefaultValue(DefaultHintShowman)]
    public bool HintShowman
    {
        get => _hintShowman;
        set { _hintShowman = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _partialText = DefaultPartialText;

    /// <summary>
    /// Partial text flag.
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultPartialText)]
    public bool PartialText
    {
        get => _partialText;
        set { _partialText = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _partialImages = DefaultPartialImages;

    /// <summary>
    /// Partial images flag.
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultPartialImages)]
    public bool PartialImages
    {
        get => _partialImages;
        set { _partialImages = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _playAllQuestionsInFinalRound = DefaultPlayAllQuestionsInFinalRound;

    /// <summary>
    /// Play all questions in final round.
    /// </summary>

    [XmlAttribute]
    [DefaultValue(DefaultPlayAllQuestionsInFinalRound)]
    public bool PlayAllQuestionsInFinalRound
    {
        get => _playAllQuestionsInFinalRound;
        set { _playAllQuestionsInFinalRound = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _allowEveryoneToPlayHiddenStakes = DefaultAllowEveryoneToPlayHiddenStakes;

    /// <summary>
    /// Allow all players to play hidden stakes question.
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultAllowEveryoneToPlayHiddenStakes)]
    public bool AllowEveryoneToPlayHiddenStakes
    {
        get => _allowEveryoneToPlayHiddenStakes;
        set { _allowEveryoneToPlayHiddenStakes = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _oral = DefaultOral;

    /// <summary>
    /// Oral game flag.
    /// </summary>

    [XmlAttribute]
    [DefaultValue(DefaultOral)]
    public bool Oral
    {
        get => _oral;
        set { _oral = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _oralPlayersActions = DefaultOralPlayersActions;

    /// <summary>
    /// Oral players actions game flag.
    /// </summary>

    [XmlAttribute]
    [DefaultValue(DefaultOralPlayersActions)]
    public bool OralPlayersActions
    {
        get => _oralPlayersActions;
        set { _oralPlayersActions = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _managed = DefaultManaged;

    /// <summary>
    /// Managed game flag.
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultManaged)]
    public bool Managed
    {
        get => _managed;
        set { _managed = value; OnPropertyChanged(); }
    }

    private PenaltyType _questionWithButtonPenalty = DefaultQuestionWithButtonPenalty;

    /// <summary>
    /// Question with button penalty.
    /// </summary>
    [DefaultValue(DefaultQuestionWithButtonPenalty)]
    public PenaltyType QuestionWithButtonPenalty
    {
        get => _questionWithButtonPenalty;
        set
        {
            if (_questionWithButtonPenalty != value)
            {
                _questionWithButtonPenalty = value;
                OnPropertyChanged();
            }
        }
    }

    private PenaltyType _questionForYourselfPenalty = DefaultQuestionForYourselfPenalty;

    /// <summary>
    /// Question for yourself penalty.
    /// </summary>
    [DefaultValue(DefaultQuestionForYourselfPenalty)]
    public PenaltyType QuestionForYourselfPenalty
    {
        get => _questionForYourselfPenalty;
        set
        {
            if (_questionForYourselfPenalty != value)
            {
                _questionForYourselfPenalty = value;
                OnPropertyChanged();
            }
        }
    }

    private PenaltyType _questionForAllPenalty = DefaultQuestionForAllPenalty;

    /// <summary>
    /// Question for all penalty.
    /// </summary>
    [DefaultValue(DefaultQuestionForAllPenalty)]
    public PenaltyType QuestionForAllPenalty
    {
        get => _questionForAllPenalty;
        set
        {
            if (_questionForAllPenalty != value)
            {
                _questionForAllPenalty = value;
                OnPropertyChanged();
            }
        }
    }

    private int _questionForYourselfFactor = DefaultQuestionForYourselfFactor;

    /// <summary>
    /// Question for yourself factor.
    /// </summary>
    [DefaultValue(DefaultQuestionForYourselfFactor)]
    public int QuestionForYourselfFactor
    {
        get => _questionForYourselfFactor;
        set
        {
            if (_questionForYourselfFactor != value)
            {
                _questionForYourselfFactor = value;
                OnPropertyChanged();
            }
        }
    }

    private ButtonPressMode _buttonPressMode = DefaultButtonPressMode;

    /// <summary>
    /// Button press mode.
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultButtonPressMode)]
    public ButtonPressMode ButtonPressMode
    {
        get => _buttonPressMode;

        set
        {
            if (_buttonPressMode != value)
            {
                _buttonPressMode = value;
                OnPropertyChanged();
            }
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _preloadRoundContent = DefaultPreloadRoundContent;

    /// <summary>
    /// Allows players to download round media content at the beginng of the round.
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultPreloadRoundContent)]
    public bool PreloadRoundContent
    {
        get => _preloadRoundContent;
        set { _preloadRoundContent = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private GameModes _gameMode = DefaultGameMode;

    /// <summary>
    /// Game mode.
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultGameMode)]
    public GameModes GameMode
    {
        get => _gameMode;
        set { _gameMode = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private int _randomRoundsCount = DefaultRandomRoundsCount;

    /// <summary>
    /// Random rounds count.
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultRandomRoundsCount)]
    public int RandomRoundsCount
    {
        get => _randomRoundsCount;
        set { _randomRoundsCount = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private int _randomThemesCount = DefaultRandomThemesCount;

    /// <summary>
    /// Random themes count.
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultRandomThemesCount)]
    public int RandomThemesCount
    {
        get => _randomThemesCount;
        set { _randomThemesCount = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private int _randomQuestionsBasePrice = DefaultRandomQuestionsBasePrice;

    /// <summary>
    /// Random questions base price.
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultRandomQuestionsBasePrice)]
    public int RandomQuestionsBasePrice
    {
        get => _randomQuestionsBasePrice;
        set { _randomQuestionsBasePrice = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _useApellations = DefaultUseApellations;

    /// <summary>
    /// Use apellations in game.
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultUseApellations)]
    public bool UseApellations
    {
        get => _useApellations;
        set { _useApellations = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _displayAnswerOptionsOneByOne = DefaultDisplayAnswerOptionsOneByOne;

    /// <summary>
    /// Display answer options one by one.
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultDisplayAnswerOptionsOneByOne)]
    public bool DisplayAnswerOptionsOneByOne
    {
        get => _displayAnswerOptionsOneByOne;
        set { _displayAnswerOptionsOneByOne = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _displayAnswerOptionsLabels = DefaultDisplayAnswerOptionsLabels;

    /// <summary>
    /// Display answer options labels.
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultDisplayAnswerOptionsLabels)]
    public bool DisplayAnswerOptionsLabels
    {
        get => _displayAnswerOptionsLabels;
        set { _displayAnswerOptionsLabels = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Game culture.
    /// </summary>
    [XmlAttribute]
    [DefaultValue("en-US")]
    public string? Culture { get; set; }

    public AppSettingsCore() { } // For serializers

    public AppSettingsCore(AppSettingsCore origin) => Set(origin);

    public void Set(AppSettingsCore settings)
    {
        MultimediaPort = settings._multimediaPort;
        ReadingSpeed = settings._readingSpeed;
        TimeSettings = settings.TimeSettings;
        FalseStart = settings._falseStart;
        PartialText = settings.PartialText;
        PartialImages = settings.PartialImages;
        Managed = settings.Managed;
        HintShowman = settings._hintShowman;
        PlayAllQuestionsInFinalRound = settings.PlayAllQuestionsInFinalRound;
        AllowEveryoneToPlayHiddenStakes = settings.AllowEveryoneToPlayHiddenStakes;
        Oral = settings._oral;
        OralPlayersActions = settings.OralPlayersActions;
        QuestionWithButtonPenalty = settings.QuestionWithButtonPenalty;
        QuestionForYourselfPenalty = settings.QuestionForYourselfPenalty;
        QuestionForAllPenalty = settings.QuestionForAllPenalty;
        QuestionForYourselfFactor = settings.QuestionForYourselfFactor;
        ButtonPressMode = settings.ButtonPressMode;
        _preloadRoundContent = settings.PreloadRoundContent;
        _gameMode = settings._gameMode;

        _randomRoundsCount = settings._randomRoundsCount;
        _randomThemesCount = settings._randomThemesCount;
        _randomQuestionsBasePrice = settings._randomQuestionsBasePrice;

        _useApellations = settings.UseApellations;
        _displayAnswerOptionsOneByOne = settings.DisplayAnswerOptionsOneByOne;
        _displayAnswerOptionsLabels = settings.DisplayAnswerOptionsLabels;

        Culture = settings.Culture;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;
}
