using System.ComponentModel.DataAnnotations;

namespace SImulator.ViewModel.Model;

/// <summary>
/// Defines game modes.
/// </summary>
public enum GameModes
{
    /// <summary>
    /// Classic mode (with final round and special questions support).
    /// </summary>
    [Display(Description = "GameModes_Tv")]
    Tv,
    /// <summary>
    /// Simplified mode (without final round and special questions).
    /// </summary>
    [Display(Description = "GameModes_Sport")]
    Sport
}
