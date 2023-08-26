using System.ComponentModel;

namespace SIQuester.ViewModel.Model;

/// <summary>
/// Defines NumberSet modes.
/// </summary>
public enum NumberSetMode
{
    /// <summary>
    /// Single fixed value.
    /// </summary>
    [Description("NumberSetModeFixedValue")]
    FixedValue,

    /// <summary>
    /// Minimum or maximum value in the round.
    /// </summary>
    [Description("NumberSetModeMinimumOrMaximumInRound")]
    MinimumOrMaximumInRound,

    /// <summary>
    /// Range value.
    /// </summary>
    [Description("NumberSetModeRange")]
    Range,

    /// <summary>
    /// Range value.
    /// </summary>
    [Description("NumberSetModeRangeWithStep")]
    RangeWithStep,
}
