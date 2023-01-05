using System.ComponentModel.DataAnnotations;

namespace SIData;

/// <summary>
/// Defines a player style.
/// </summary>
public enum PlayerStyle
{
    /// <summary>
    /// Agressive style.
    /// </summary>
    [Display(Description = "PlayerStyle_Agressive")]
    Agressive,

    /// <summary>
    /// Normal style.
    /// </summary>
    [Display(Description = "PlayerStyle_Normal")]
    Normal,

    /// <summary>
    /// Careful style.
    /// </summary>
    [Display(Description = "PlayerStyle_Careful")]
    Careful
}
