namespace SI.Contracts;

/// <summary>
/// Defines game time settings, in seconds.
/// </summary>
public sealed class TimeSettings
{
    public const int DefaultQuestionSelection = 30;
    public const int DefaultThemeSelection = 30;
    public const int DefaultPlayerSelection = 30;
    public const int DefaultButtonPressing = 5;
    public const int DefaultAnswerGiving = 25;
    public const int DefaultSoloAnswerGiving = 25;
    public const int DefaultHiddenAnswerPrinting = 45;
    public const int DefaultStakeMaking = 30;
    public const int DefaultShowmanDecision = 30;
    public const int DefaultRound = 3600;
    public const int DefaultButtonBlocking = 3;
    public const int DefaultRightAnswer = 2;
    public const int DefaultImage = 5;
    public const int DefaultPartialImage = 3;

    public static readonly TimeSettings Default = new()
    {
        QuestionSelection = DefaultQuestionSelection,
        ThemeSelection = DefaultThemeSelection,
        PlayerSelection = DefaultPlayerSelection,
        ButtonPressing = DefaultButtonPressing,
        AnswerGiving = DefaultAnswerGiving,
        SoloAnswerGiving = DefaultSoloAnswerGiving,
        HiddenAnswerPrinting = DefaultHiddenAnswerPrinting,
        StakeMaking = DefaultStakeMaking,
        ShowmanDecision = DefaultShowmanDecision,
        Round = DefaultRound,
        ButtonBlocking = DefaultButtonBlocking,
        RightAnswer = DefaultRightAnswer,
        Image = DefaultImage,
        PartialImage = DefaultPartialImage,
    };

    /// <summary>
    /// Gets or sets the time interval allowed for selecting a question.
    /// </summary>
    public int QuestionSelection { get; set; } = DefaultQuestionSelection;

    /// <summary>
    /// Gets or sets the time interval allowed for seleting a theme.
    /// </summary>
    public int ThemeSelection { get; set; } = DefaultThemeSelection;

    /// <summary>
    /// Gets or sets the time interval allowed for seleting a player.
    /// </summary>
    public int PlayerSelection { get; set; } = DefaultPlayerSelection;

    /// <summary>
    /// Gets or sets the time interval allowed for pressing a button.
    /// </summary>
    public int ButtonPressing { get; set; } = DefaultButtonPressing;

    /// <summary>
    /// Gets or sets the time interval allowed for giving an answer.
    /// </summary>
    public int AnswerGiving { get; set; } = DefaultAnswerGiving;

    /// <summary>
    /// Gets or sets the time interval allowed for giving a solo answer.
    /// </summary>
    public int SoloAnswerGiving { get; set; } = DefaultSoloAnswerGiving;

    /// <summary>
    /// Gets or sets the time interval allowed for printing an answer.
    /// </summary>
    public int HiddenAnswerPrinting { get; set; } = DefaultHiddenAnswerPrinting;

    /// <summary>
    /// Gets or sets the time interval for making a stake.
    /// </summary>
    public int StakeMaking { get; set; } = DefaultStakeMaking;

    /// <summary>
    /// Gets or sets the time interval for showman's decisions.
    /// </summary>
    public int ShowmanDecision { get; set; } = DefaultShowmanDecision;

    /// <summary>
    /// Gets or sets the round time.
    /// </summary>
    public int Round { get; set; } = DefaultRound;

    /// <summary>
    /// Gets or sets the time interval for blocking button after pressing.
    /// </summary>
    public int ButtonBlocking { get; set; } = DefaultButtonBlocking;

    /// <summary>
    /// Gets or sets the time interval for showing the right answer.
    /// </summary>
    public int RightAnswer { get; set; } = DefaultRightAnswer;

    /// <summary>
    /// Gets or sets the time interval for image display.
    /// </summary>
    public int Image { get; set; } = DefaultImage;

    /// <summary>
    /// Gets or sets the time interval for partial image rendering.
    /// </summary>
    public int PartialImage { get; set; } = DefaultPartialImage;
}
