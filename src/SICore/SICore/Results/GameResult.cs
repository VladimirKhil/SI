namespace SICore.Results;

/// <summary>
/// Defines a game result report.
/// </summary>
public sealed class GameResult
{
    /// <summary>
    /// Game name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Game start time.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Game duration.
    /// </summary>
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Имя пакета
    /// </summary>
    public string? PackageName { get; set; }

    /// <summary>
    /// Game package hash.
    /// </summary>
    public string? PackageHash { get; set; }

    /// <summary>
    /// Game package authors.
    /// </summary>
    public string[] PackageAuthors { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Players results.
    /// </summary>
    public Dictionary<string, int> Results { get; } = new();

    /// <summary>
    /// Persons reviews.
    /// </summary>
    public Dictionary<string, string> Reviews { get; } = new();

    /// <summary>
    /// Automatically accepted answers.
    /// </summary>
    public List<QuestionReport> AcceptedAnswers { get; } = new();

    /// <summary>
    /// Appellated right answers.
    /// </summary>
    public List<QuestionReport> ApellatedAnswers { get; } = new();

    /// <summary>
    /// Wrong answers.
    /// </summary>
    public List<QuestionReport> RejectedAnswers { get; } = new();

    /// <summary>
    /// Complained by users questions.
    /// </summary>
    public List<QuestionReport> ComplainedQuestions { get; } = new();
}
