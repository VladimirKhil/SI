namespace SICore.Models;

/// <summary>
/// Defines game host options.
/// </summary>
public sealed class HostOptions
{
    /// <summary>
    /// Interval for accepting buttons when selecting random winner.
    /// </summary>
    public TimeSpan ButtonsAcceptInterval { get; set; } = TimeSpan.FromMilliseconds(400);
}
