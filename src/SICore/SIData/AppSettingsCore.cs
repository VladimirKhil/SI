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
    public const bool DefaultPlayAllQuestionsInFinalRound = false;
    public const bool DefaultAllowEveryoneToPlayHiddenStakes = true;
    public const bool DefaultOral = false;
    public const bool DefaultOralPlayersActions = true;
    public const bool DefaultManaged = false;
    public const bool DefaultIgnoreWrong = false;
    public const bool DefaultDisplaySources = false;
    public const bool DefaultUsePingPenalty = false;
    public const bool DefaultPreloadRoundContent = true;
    public const GameModes DefaultGameMode = GameModes.Tv;
    public const int DefaultRandomRoundsCount = 3;
    public const int DefaultRandomThemesCount = 6;
    public const int DefaultRandomQuestionsBasePrice = 100;
    public const bool DefaultUseApellations = true;

    /// <summary>
    /// Time settings.
    /// </summary>
    public TimeSettings TimeSettings { get; set; }

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
    /// Скорость чтения вопроса (символов в секунду)
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultReadingSpeed)]
    public int ReadingSpeed
    {
        get => _readingSpeed;
        set { _readingSpeed = value; OnPropertyChanged(); }
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

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _ignoreWrong = DefaultIgnoreWrong;

    /// <summary>
    /// Wrong answer did not lead to penalty.
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultIgnoreWrong)]
    public bool IgnoreWrong
    {
        get => _ignoreWrong;
        set { _ignoreWrong = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _displaySources = DefaultDisplaySources;

    /// <summary>
    /// Display package items sources.
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultDisplaySources)]
    public bool DisplaySources
    {
        get => _displaySources;
        set { _displaySources = value; OnPropertyChanged(); }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _usePingPenalty = DefaultUsePingPenalty;

    /// <summary>
    /// Should the players with good ping get penalty.
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultUsePingPenalty)]
    public bool UsePingPenalty
    {
        get => _usePingPenalty;
        set { _usePingPenalty = value; OnPropertyChanged(); }
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

    /// <summary>
    /// Game culture.
    /// </summary>
    [XmlAttribute]
    [DefaultValue("en-US")]
    public string? Culture { get; set; }

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
        PlayAllQuestionsInFinalRound = origin.PlayAllQuestionsInFinalRound;
        AllowEveryoneToPlayHiddenStakes = origin.AllowEveryoneToPlayHiddenStakes;
        _oral = origin._oral;
        _ignoreWrong = origin._ignoreWrong;
        _usePingPenalty = origin.UsePingPenalty;
        _preloadRoundContent = origin.PreloadRoundContent;
        _gameMode = origin._gameMode;

        _randomRoundsCount = origin._randomRoundsCount;
        _randomThemesCount = origin._randomThemesCount;
        _randomQuestionsBasePrice = origin._randomQuestionsBasePrice;

        _useApellations = origin.UseApellations;

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
        PlayAllQuestionsInFinalRound = settings.PlayAllQuestionsInFinalRound;
        AllowEveryoneToPlayHiddenStakes = settings.AllowEveryoneToPlayHiddenStakes;
        Oral = settings._oral;
        OralPlayersActions = settings.OralPlayersActions;
        _ignoreWrong = settings._ignoreWrong;
        _usePingPenalty = settings.UsePingPenalty;
        _preloadRoundContent = settings.PreloadRoundContent;
        _gameMode = settings._gameMode;

        _randomRoundsCount = settings._randomRoundsCount;
        _randomThemesCount = settings._randomThemesCount;
        _randomQuestionsBasePrice = settings._randomQuestionsBasePrice;

        _useApellations = settings.UseApellations;

        Culture = settings.Culture;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;
}
