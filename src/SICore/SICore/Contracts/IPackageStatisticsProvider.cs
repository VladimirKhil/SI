namespace SICore.Contracts;

/// <summary>
/// Provides methods for retrieving statistical data related to package questions.
/// </summary>
/// <remarks>This interface defines operations for accessing specific statistics, such as retrieving appellated
/// answers for a given question within a specified round and theme.</remarks>
public interface IPackageStatisticsProvider
{
    /// <summary>
    /// Gets appellated answers for a question.
    /// </summary>
    /// <param name="roundIndex">Round index.</param>
    /// <param name="themeIndex">Theme index.</param>
    /// <param name="questionIndex">Question index.</param>
    ICollection<string> GetAppellatedAnswers(int roundIndex, int themeIndex, int questionIndex);

    /// <summary>
    /// Gets rejected answers for a question.
    /// </summary>
    /// <param name="roundIndex">Round index.</param>
    /// <param name="themeIndex">Theme index.</param>
    /// <param name="questionIndex">Question index.</param>
    ICollection<string> GetRejectedAnswers(int roundIndex, int themeIndex, int questionIndex);

    /// <summary>
    /// Gets package source.
    /// </summary>
    string? GetPackageSource();
}
