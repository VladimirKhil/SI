namespace SICore.Models;

/// <summary>
/// Defines stake modes flags.
/// </summary>
[Flags]
public enum StakeModes
{
    /// <summary>
    /// Fixed stake.
    /// </summary>
    Stake = 1,

    /// <summary>
    /// Pass.
    /// </summary>
    Pass = 2,

    /// <summary>
    /// All-in.
    /// </summary>
    AllIn = 4
}