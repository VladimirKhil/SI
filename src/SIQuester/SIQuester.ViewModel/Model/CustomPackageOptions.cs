namespace SIQuester.Model;

/// <summary>
/// Defines custom package options.
/// </summary>
public sealed class CustomPackageOptions
{
    /// <summary>
    /// Standard round count.
    /// </summary>
    public int RoundCount { get; set; } = 3;

    /// <summary>
    /// Theme per round count.
    /// </summary>
    public int ThemeCount { get; set; } = 6;

    /// <summary>
    /// Question per theme count.
    /// </summary>
    public int QuestionCount { get; set; } = 5;

    /// <summary>
    /// Base question price in first round.
    /// </summary>
    public int BaseQuestionPrice { get; set; } = 100;

    /// <summary>
    /// Is final round present.
    /// </summary>
    public bool HasFinal { get; set; } = true;

    /// <summary>
    /// Final theme count.
    /// </summary>
    public int FinalThemeCount { get; set; } = 7;
}
