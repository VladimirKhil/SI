namespace SIEngine.Core;

/// <summary>
/// Defines question engine options.
/// </summary>
public sealed class QuestionEngineOptions
{
    /// <summary>
    /// False starts mode.
    /// </summary>
    public FalseStartMode FalseStarts { get; set; } = FalseStartMode.Enabled;

    /// <summary>
    /// Show simple right answers.
    /// </summary>
    public bool ShowSimpleRightAnswers { get; set; }
}
