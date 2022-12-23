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
    [Description("Фиксированное значение")]
    FixedValue,

    /// <summary>
    /// Minimum or maximum value in the round.
    /// </summary>
    [Description("Минимум или максимум в раунде")]
    MinimumOrMaximumInRound,

    /// <summary>
    /// Range value.
    /// </summary>
    [Description("Выбор")]
    Range,

    /// <summary>
    /// Range value.
    /// </summary>
    [Description("Выбор с шагом")]
    RangeWithStep,
}
