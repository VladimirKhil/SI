namespace SICore.Results;

/// <summary>
/// Defines a game question report.
/// </summary>
public sealed class QuestionReport
{
    /// <summary>
    /// Theme name.
    /// </summary>
    public string? ThemeName { get; set; }

    /// <summary>
    /// Question text.
    /// </summary>
    public string? QuestionText { get; set; }

    /// <summary>
    /// Report text (complain text or question answer).
    /// </summary>
    public string? ReportText { get; set; }
}
