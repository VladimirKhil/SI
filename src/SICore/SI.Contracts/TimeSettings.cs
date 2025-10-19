namespace SI.Contracts;

/// <summary>
/// Defines game time settings, in seconds.
/// </summary>
public sealed class TimeSettings
{
    public static readonly TimeSettings Default = new()
    {
        QuestionSelection = 30,
        ThemeSelection = 30,
        PlayerSelection = 30,
        ButtonPressing = 5,
        AnswerGiving = 25,
        SoloAnswerGiving = 25,
        HiddenAnswerPrinting = 45,
        StakeMaking = 30,
        ShowmanDecision = 30,
        Round = 3600,
        ButtonBlocking = 3,
        RightAnswer = 2,
        Image = 5,
        PartialImage = 3,
    };

    /// <summary>
    /// Gets or sets the time interval allowed for selecting a question.
    /// </summary>
    public int QuestionSelection { get; set; } = Default.QuestionSelection;

    /// <summary>
    /// Gets or sets the time interval allowed for seleting a theme.
    /// </summary>
    public int ThemeSelection { get; set; } = Default.ThemeSelection;

    /// <summary>
    /// Gets or sets the time interval allowed for seleting a player.
    /// </summary>
    public int PlayerSelection { get; set; } = Default.PlayerSelection;

    /// <summary>
    /// Gets or sets the time interval allowed for pressing a button.
    /// </summary>
    public int ButtonPressing { get; set; } = Default.ButtonPressing;

    /// <summary>
    /// Gets or sets the time interval allowed for giving an answer.
    /// </summary>
    public int AnswerGiving { get; set; } = Default.AnswerGiving;

    /// <summary>
    /// Gets or sets the time interval allowed for giving a solo answer.
    /// </summary>
    public int SoloAnswerGiving { get; set; } = Default.SoloAnswerGiving;

    /// <summary>
    /// Gets or sets the time interval allowed for printing an answer.
    /// </summary>
    public int HiddenAnswerPrinting { get; set; } = Default.HiddenAnswerPrinting;

    /// <summary>
    /// Gets or sets the time interval for making a stake.
    /// </summary>
    public int StakeMaking { get; set; } = Default.StakeMaking;

    /// <summary>
    /// Gets or sets the time interval for showman's decisions.
    /// </summary>
    public int ShowmanDecision { get; set; } = Default.ShowmanDecision;

    /// <summary>
    /// Gets or sets the round time.
    /// </summary>
    public int Round { get; set; } = Default.Round;

    /// <summary>
    /// Gets or sets the time interval for blocking button after pressing.
    /// </summary>
    public int ButtonBlocking { get; set; } = Default.ButtonBlocking;

    /// <summary>
    /// Gets or sets the time interval for showing the right answer.
    /// </summary>
    public int RightAnswer { get; set; } = Default.RightAnswer;

    /// <summary>
    /// Gets or sets the time interval for image display.
    /// </summary>
    public int Image { get; set; } = Default.Image;

    /// <summary>
    /// Gets or sets the time interval for partial image rendering.
    /// </summary>
    public int PartialImage { get; set; } = Default.PartialImage;
}
