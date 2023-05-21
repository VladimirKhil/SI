namespace SIEngine.Rules;

/// <summary>
/// Defines game rules.
/// </summary>
public sealed class GameRules
{
    /// <summary>
    /// Show themes at the game beginning.
    /// </summary>
    public bool ShowGameThemes { get; init; }

    /// <summary>
    /// Default round rules.
    /// </summary>
    public RoundRules DefaultRoundRules { get; init; } = new();

    /// <summary>
    /// Round rules for each supported round type.
    /// </summary>
    public Dictionary<string, RoundRules> RoundRules { get; } = new();

    /// <summary>
    /// Gets rules for round type.
    /// </summary>
    /// <param name="roundType">Round type.</param>
    public RoundRules GetRulesForRoundType(string roundType) => RoundRules.GetValueOrDefault(roundType, DefaultRoundRules);
}
