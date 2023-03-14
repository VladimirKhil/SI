namespace SIEngine.Core;

/// <summary>
/// Defines a false start mode.
/// This mode affects the position of AskAnswer step with button mode in question script.
/// </summary>
public enum FalseStartMode
{
    /// <summary>
    /// False starts are enabled for all questions. Default value.
    /// </summary>
    Enabled,

    /// <summary>
    /// False starts are enabled for text content only. AskAnswer is executed before multimedia content.
    /// </summary>
    TextContentOnly,

    /// <summary>
    /// False starts are disabled.
    /// </summary>
    Disabled
}
