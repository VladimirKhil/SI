namespace SICore.Models;

/// <summary>
/// Defines statistics for a question.
/// </summary>
public sealed class QuestionStats
{
    /// <summary>
    /// Number of times question was shown.
    /// </summary>
    public int ShownCount { get; set; }

    /// <summary>
    /// Gets or sets the number of players that have seen the question.
    /// </summary>
    public int PlayerSeenCount { get; set; }

    /// <summary>
    /// Gets or sets the number of correct answers for the question.
    /// </summary>
    public int CorrectCount { get; set; }

    /// <summary>
    /// Gets or sets the number of wrong answers for the question.
    /// </summary>
    public int WrongCount { get; set; }
}
