using SICore.Models;

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
    /// Game language.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Game start time.
    /// </summary>
    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Game duration.
    /// </summary>
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Game package name.
    /// </summary>
    public string? PackageName { get; set; }

    /// <summary>
    /// Package source URI.
    /// </summary>
    public Uri? PackageSource { get; }

    /// <summary>
    /// Game package authors.
    /// </summary>
    public string[] PackageAuthors { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Game package authors contacts.
    /// </summary>
    public string? PackageAuthorsContacts { get; set; }

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

    /// <summary>
    /// Defines statistics for questions.
    /// </summary>
    public Dictionary<string, QuestionStats> QuestionsStats { get; } = new();

    public GameResult(Uri? packageSource = null)
    {
        PackageSource = packageSource;
    }

    internal void IncrementQuestionSeenCount(string questionKey, int humanPlayerCount)
    {
        if (!QuestionsStats.TryGetValue(questionKey, out var questionStats))
        {
            QuestionsStats[questionKey] = questionStats = new QuestionStats();
        }

        questionStats.ShownCount++;
        questionStats.PlayerSeenCount += humanPlayerCount;
    }

    internal void IncrementQuestionCorrectCount(string questionKey)
    {
        if (!QuestionsStats.TryGetValue(questionKey, out var questionStats))
        {
            QuestionsStats[questionKey] = questionStats = new QuestionStats();
        }

        questionStats.CorrectCount++;
    }

    internal void IncrementQuestionWrongCount(string questionKey)
    {
        if (!QuestionsStats.TryGetValue(questionKey, out var questionStats))
        {
            QuestionsStats[questionKey] = questionStats = new QuestionStats();
        }

        questionStats.WrongCount++;
    }
}
