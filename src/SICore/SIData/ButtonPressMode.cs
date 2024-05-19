using System.ComponentModel.DataAnnotations;

namespace SIData;

/// <summary>
/// Defines well-known button press handler modes.
/// </summary>
public enum ButtonPressMode
{
    /// <summary>
    /// Select winner randomly from all pressers withing an interval.
    /// </summary>
    [Display(Description = "ButtonPressMode_RandomWithinInterval")]
    RandomWithinInterval,

    /// <summary>
    /// First to press wins the button.
    /// </summary>
    [Display(Description = "ButtonPressMode_FirstWins")]
    FirstWins,

    /// <summary>
    /// First to press wins the button. Reaction value is calculated on the client side.
    /// </summary>
    [Display(Description = "ButtonPressMode_FirstWinsClient")]
    FirstWinsClient,
}
