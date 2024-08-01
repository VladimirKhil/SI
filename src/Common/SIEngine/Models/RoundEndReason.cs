namespace SIEngine.Models;

/// <summary>
/// Defines round end reasons.
/// </summary>
public enum RoundEndReason
{
    /// <summary>
    /// Round is completed.
    /// </summary>
    Completed,

    /// <summary>
    /// Round time is over.
    /// </summary>
    Timeout,

    /// <summary>
    /// Manual round end.
    /// </summary>
    Manual,
}
