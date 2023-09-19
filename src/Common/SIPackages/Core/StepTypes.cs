namespace SIPackages.Core;

/// <summary>
/// Defines well-known script step types.
/// </summary>
public static class StepTypes
{
    /// <summary>
    /// Set answer type (and options).
    /// </summary>
    public const string SetAnswerType = "setAnswerType";

    /// <summary>
    /// Display content.
    /// </summary>
    public const string ShowContent = "showContent";

    /// <summary>
    /// Ask answer.
    /// </summary>
    public const string AskAnswer = "askAnswer";

    /// <summary>
    /// Set answerer.
    /// </summary>
    public const string SetAnswerer = "setAnswerer";

    /// <summary>
    /// Announce price.
    /// </summary>
    public const string AnnouncePrice = "announcePrice";

    /// <summary>
    /// Set price.
    /// </summary>
    public const string SetPrice = "setPrice";

    /// <summary>
    /// Set theme.
    /// </summary>
    public const string SetTheme = "setTheme";

    /// <summary>
    /// Accept (finish question pretending that active player is right).
    /// </summary>
    public const string Accept = "accept";
}
