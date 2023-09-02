namespace SIEngine;

/// <summary>
/// Provides <see cref="ISIEngine" /> options allowing them to be changed during the play.
/// </summary>
public sealed class EngineOptions
{
    /// <summary>
    /// Use buttons to press on common question.
    /// </summary>
    public bool IsPressMode { get; init; }

    /// <summary>
    /// Use buttons to press on multimedia question.
    /// </summary>
    public bool IsMultimediaPressMode { get; init; }

    /// <summary>
    /// Show right answers.
    /// </summary>
    public bool ShowRight { get; init; }

    /// <summary>
    /// Show players score.
    /// </summary>
    public bool ShowScore { get; init; }

    /// <summary>
    /// Run game automatically.
    /// </summary>
    public bool AutomaticGame { get; init; }

    /// <summary>
    /// Play special questions.
    /// </summary>
    public bool PlaySpecials { get; init; }

    /// <summary>
    /// Question thinking time for automatic game mode.
    /// </summary>
    public int ThinkingTime { get; init; }

    /// <summary>
    /// Play all questions in final round.
    /// </summary>
    public bool PlayAllQuestionsInFinalRound { get; set; }
}
