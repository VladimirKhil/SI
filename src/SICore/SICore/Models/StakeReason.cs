namespace SICore.Models;

/// <summary>
/// Defines reasons for making stakes.
/// </summary>
public enum StakeReason
{
    /// <summary>
    /// Stake plays immediately.
    /// </summary>
    Simple,

    /// <summary>
    /// Only person with the highest stake plays.
    /// </summary>
    HighestPlays,

    /// <summary>
    /// Everybody makes hidden stakes.
    /// </summary>
    Hidden,
}
