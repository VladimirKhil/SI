using SIEngine.Rules;
using SIPackages;

namespace SIEngine.QuestionSelectionStrategies;

internal static class QuestionSelectionStrategyFactory
{
    internal static ISelectionStrategy GetStrategy(
        Round round,
        EngineOptions engineOptions,
        QuestionSelectionStrategyType questionSelectionStrategyType,
        ISIEnginePlayHandler playHandler,
        Action<int, int> selectionCallback,
        Action endRoundCallback) =>
        questionSelectionStrategyType switch
        {
            QuestionSelectionStrategyType.SelectByPlayer => new SelectByPlayerStrategy(round, playHandler, selectionCallback, endRoundCallback),
            QuestionSelectionStrategyType.Sequential => new SequentialStrategy(round, playHandler, selectionCallback),
            QuestionSelectionStrategyType.RemoveOtherThemes => new RemoveOtherThemesStrategy(round, engineOptions, playHandler, selectionCallback),
            _ => throw new InvalidOperationException($"Invalid stategy type {questionSelectionStrategyType}")
        };
}
