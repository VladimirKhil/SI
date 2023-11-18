namespace SICore.Models;

/// <summary>
/// Defines stake types.
/// </summary>
[Flags]
public enum StakeTypes
{
    /// <summary>
    /// Nominal.
    /// </summary>
    Nominal = 1,

    /// <summary>
    /// Fixed stake.
    /// </summary>
    Stake = 2,

    /// <summary>
    /// Pass.
    /// </summary>
    Pass = 4,

    /// <summary>
    /// All-in.
    /// </summary>
    AllIn = 8
}
