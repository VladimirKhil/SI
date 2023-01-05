using System.ComponentModel.DataAnnotations;

namespace SIData;

/// <summary>
/// Describes game agent roles.
/// </summary>
public enum GameRole
{
    /// <summary>
    /// Game viewer.
    /// </summary>
    [Display(Description = "GameRole_Viewer")]
    Viewer,

    /// <summary>
    /// Game player.
    /// </summary>
    [Display(Description = "GameRole_Player")]
    Player,

    /// <summary>
    /// Game showman.
    /// </summary>
    [Display(Description = "GameRole_Showman")]
    Showman
}
