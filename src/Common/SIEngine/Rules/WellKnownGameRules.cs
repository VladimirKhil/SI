using SIPackages.Core;

namespace SIEngine.Rules;

/// <summary>
/// Defines well-known game rules.
/// </summary>
internal static class WellKnownGameRules
{
    /// <summary>
    /// Classic game rules.
    /// </summary>
    internal static readonly GameRules Classic = new()
    {
        ShowGameThemes = true,
        DefaultRoundRules = new()
        {
            DefaultQuestionType = QuestionTypes.Simple,
            QuestionSelectionStrategyType = QuestionSelectionStrategyType.SelectByPlayer
        }
    };

    /// <summary>
    /// Simple game rules.
    /// </summary>
    internal static readonly GameRules Simple = new()
    {
        ShowGameThemes = false,
        DefaultRoundRules = new()
        {
            DefaultQuestionType = QuestionTypes.Simple,
            QuestionSelectionStrategyType = QuestionSelectionStrategyType.Sequential
        }
    };

    static WellKnownGameRules()
    {
        Classic.RoundRules[RoundTypes.Final] = new()
        {
            DefaultQuestionType = QuestionTypes.StakeAll,
            QuestionSelectionStrategyType = QuestionSelectionStrategyType.RemoveOtherThemes
        };

        Simple.RoundRules[RoundTypes.Final] = new()
        {
            DefaultQuestionType = QuestionTypes.StakeAll,
            QuestionSelectionStrategyType = QuestionSelectionStrategyType.Sequential
        };
    }
}
