using System.ComponentModel.DataAnnotations;

namespace SIData;

/// <summary>
/// Defines game modes.
/// </summary>
public enum GameModes
{
    /// <summary>
    /// Classic mode (with question selection).
    /// </summary>
    [Display(Description = "GameModes_Tv")]
    Tv,

    /// <summary>
    /// Simplified mode (with question sequential play).
    /// </summary>
    [Display(Description = "GameModes_Sport")]
    Sport,

    /// <summary>
    /// Quiz mode.
    /// </summary>
    Quiz,

    /// <summary>
    /// Turn taking mode.
    /// </summary>
    TurnTaking,
}
