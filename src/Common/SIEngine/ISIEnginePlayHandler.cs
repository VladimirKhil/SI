using SIEngine.Rules;

namespace SIEngine;

/// <summary>
/// Handles SIEngine play events.
/// </summary>
public interface ISIEnginePlayHandler
{
    /// <summary>
    /// Detects whether round play should be continued.
    /// </summary>
    /// <param name="questionSelectionStrategyType">Question selection strategy type.</param>
    bool ShouldPlayRound(QuestionSelectionStrategyType questionSelectionStrategyType);
}
