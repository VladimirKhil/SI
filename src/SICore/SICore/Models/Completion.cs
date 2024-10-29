namespace SICore.Models;

/// <summary>
/// Defines a media content completion that waits all persons to finish watching media.
/// </summary>
internal sealed class Completion
{
    /// <summary>
    /// Current number of watchers.
    /// </summary>
    internal int Current { get; set; }

    /// <summary>
    /// Total awaited number of watchers.
    /// </summary>
    internal int Total { get; set; }

    public Completion(int total) => Total = total;
}
