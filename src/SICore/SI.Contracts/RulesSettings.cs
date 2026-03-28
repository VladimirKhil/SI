namespace SI.Contracts;

/// <summary>
/// Defines game rules settings.
/// </summary>
public sealed class RulesSettings
{
    public const int DefaultReadingSpeed = 20;
    public const bool DefaultFalseStart = true;
    public const bool DefaultPartialText = false;
    public const bool DefaultPartialImages = false;
    public const bool DefaultPlayAllThemesInThemesRemovalRound = false;
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
    public const GameMode DefaultGameMode = GameMode.Classic;
    public const bool DefaultUseAppellations = true;
    public const bool DefaultDisplayAnswerOptionsOneByOne = true;
    public const bool DefaultDisplayAnswerOptionsLabels = true;
    public const bool DefaultPrependThemeCommentsToQuestion = true;
    public const bool DefaultAppendRightAnswerTextToComplexAnswer = true;

    public static readonly RulesSettings Default = new()
    {
        ReadingSpeed = DefaultReadingSpeed,
        FalseStart = DefaultFalseStart,
        PartialText = DefaultPartialText,
        PartialImages = DefaultPartialImages,
        PlayAllThemesInThemesRemovalRound = DefaultPlayAllThemesInThemesRemovalRound,
        AllowEveryoneToPlayHiddenStakes = DefaultAllowEveryoneToPlayHiddenStakes,
        Oral = DefaultOral,
        OralPlayersActions = DefaultOralPlayersActions,
        Managed = DefaultManaged,
        QuestionWithButtonPenalty = DefaultQuestionWithButtonPenalty,
        QuestionForYourselfPenalty = DefaultQuestionForYourselfPenalty,
        QuestionForAllPenalty = DefaultQuestionForAllPenalty,
        QuestionForYourselfFactor = DefaultQuestionForYourselfFactor,
        ButtonPressMode = DefaultButtonPressMode,
        PreloadRoundContent = DefaultPreloadRoundContent,
        GameMode = DefaultGameMode,
        UseAppellations = DefaultUseAppellations,
        DisplayAnswerOptionsOneByOne = DefaultDisplayAnswerOptionsOneByOne,
        DisplayAnswerOptionsLabels = DefaultDisplayAnswerOptionsLabels,
        PrependThemeCommentsToQuestion = DefaultPrependThemeCommentsToQuestion,
        AppendRightAnswerTextToComplexAnswer = DefaultAppendRightAnswerTextToComplexAnswer,
    };

    /// <summary>
    /// Gets or sets the text reading speed, in characters per second.
    /// </summary>
    public int ReadingSpeed { get; set; } = DefaultReadingSpeed;

    /// <summary>
    /// Gets or sets a value indicating whether false start is enabled.
    /// </summary>
    public bool FalseStart { get; set; } = DefaultFalseStart;

    /// <summary>
    /// Gets or sets a value indicating whether partial text is enabled.
    /// </summary>
    public bool PartialText { get; set; } = DefaultPartialText;

    /// <summary>
    /// Gets or sets a value indicating whether partial images are enabled.
    /// </summary>
    public bool PartialImages { get; set; } = DefaultPartialImages;

    /// <summary>
    /// Gets or sets a value indicating whether all themes are played in the themes removal round.
    /// </summary>
    public bool PlayAllThemesInThemesRemovalRound { get; set; } = DefaultPlayAllThemesInThemesRemovalRound;

    /// <summary>
    /// Gets or sets a value indicating whether everyone can play hidden stakes questions.
    /// </summary>
    public bool AllowEveryoneToPlayHiddenStakes { get; set; } = DefaultAllowEveryoneToPlayHiddenStakes;

    /// <summary>
    /// Gets or sets a value indicating whether oral game mode is enabled.
    /// </summary>
    public bool Oral { get; set; } = DefaultOral;

    /// <summary>
    /// Gets or sets a value indicating whether oral players actions are enabled.
    /// </summary>
    public bool OralPlayersActions { get; set; } = DefaultOralPlayersActions;

    /// <summary>
    /// Gets or sets a value indicating whether the game is managed.
    /// </summary>
    public bool Managed { get; set; } = DefaultManaged;

    /// <summary>
    /// Gets or sets the penalty for questions with button.
    /// </summary>
    public PenaltyType QuestionWithButtonPenalty { get; set; } = DefaultQuestionWithButtonPenalty;

    /// <summary>
    /// Gets or sets the penalty for questions for yourself.
    /// </summary>
    public PenaltyType QuestionForYourselfPenalty { get; set; } = DefaultQuestionForYourselfPenalty;

    /// <summary>
    /// Gets or sets the penalty for questions for all.
    /// </summary>
    public PenaltyType QuestionForAllPenalty { get; set; } = DefaultQuestionForAllPenalty;

    /// <summary>
    /// Gets or sets the factor for questions for yourself.
    /// </summary>
    public int QuestionForYourselfFactor { get; set; } = DefaultQuestionForYourselfFactor;

    /// <summary>
    /// Gets or sets the button press mode.
    /// </summary>
    public ButtonPressMode ButtonPressMode { get; set; } = DefaultButtonPressMode;

    /// <summary>
    /// Gets or sets a value indicating whether round content is preloaded.
    /// </summary>
    public bool PreloadRoundContent { get; set; } = DefaultPreloadRoundContent;

    /// <summary>
    /// Gets or sets the game mode.
    /// </summary>
    public GameMode GameMode { get; set; } = DefaultGameMode;

    /// <summary>
    /// Gets or sets a value indicating whether appellations are used.
    /// </summary>
    public bool UseAppellations { get; set; } = DefaultUseAppellations;

    /// <summary>
    /// Gets or sets a value indicating whether answer options are displayed one by one.
    /// </summary>
    public bool DisplayAnswerOptionsOneByOne { get; set; } = DefaultDisplayAnswerOptionsOneByOne;

    /// <summary>
    /// Gets or sets a value indicating whether answer option labels are displayed.
    /// </summary>
    public bool DisplayAnswerOptionsLabels { get; set; } = DefaultDisplayAnswerOptionsLabels;

    /// <summary>
    /// Gets or sets a value indicating whether theme comments are prepended to question text.
    /// </summary>
    public bool PrependThemeCommentsToQuestion { get; set; } = DefaultPrependThemeCommentsToQuestion;

    /// <summary>
    /// Gets or sets a value indicating whether right answer text is appended to a complex answer.
    /// </summary>
    public bool AppendRightAnswerTextToComplexAnswer { get; set; } = DefaultAppendRightAnswerTextToComplexAnswer;
}
