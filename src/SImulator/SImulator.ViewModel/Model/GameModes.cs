using System.ComponentModel.DataAnnotations;

namespace SImulator.ViewModel.Model;

/// <summary>
/// Defines game modes.
/// </summary>
public enum GameModes
{
    /// <summary>
    /// Classic mode.
    /// </summary>
    [Display(Description = "GameModes_Tv")]
    Tv,
  
    /// <summary>
    /// Simplified mode.
    /// </summary>
    [Display(Description = "GameModes_Sport")]
    Sport,

    /// <summary>
    /// Quiz mode.
    /// </summary>
    [Display(Description = "GameModes_Quiz")]
    Quiz,

    /// <summary>
    /// Turn-taking mode.
    /// </summary>
    [Display(Description = "GameModes_TurnTaking")]
    TurnTaking
}
