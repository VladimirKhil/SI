namespace SIEngine.Rules;

/// <summary>
/// Defines well-known question selection strategy types.
/// </summary>
public enum QuestionSelectionStrategyType
{
    /// <summary>
    /// Next question is selected by moving player.
    /// </summary>
    SelectByPlayer,

    /// <summary>
    /// Questions are played sequentially one by one.
    /// </summary>
    Sequential,

    /// <summary>
    /// Remove other themes and play the first question in last theme. Only one question in round is played.
    /// </summary>
    RemoveOtherThemes
}
