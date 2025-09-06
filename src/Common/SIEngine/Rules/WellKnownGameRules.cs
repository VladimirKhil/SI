using SIPackages.Core;

namespace SIEngine.Rules;

/// <summary>
/// Defines well-known game rules.
/// </summary>
public static class WellKnownGameRules
{
    /// <summary>
    /// Classic game rules.
    /// </summary>
    public static readonly GameRules Classic = new()
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
    public static readonly GameRules Simple = new()
    {
        ShowGameThemes = false,
        DefaultRoundRules = new()
        {
            DefaultQuestionType = QuestionTypes.Simple,
            QuestionSelectionStrategyType = QuestionSelectionStrategyType.Sequential
        }
    };

    /// <summary>
    /// Quiz game rules.
    /// </summary>
    public static readonly GameRules Quiz = new()
    {
        ShowGameThemes = false,
        DefaultRoundRules = new()
        {
            DefaultQuestionType = QuestionTypes.ForAll,
            QuestionSelectionStrategyType = QuestionSelectionStrategyType.Sequential
        }
    };

    /// <summary>
    /// Turn-taking game rules.
    /// </summary>
    public static readonly GameRules TurnTaking = new()
    {
        ShowGameThemes = false,
        DefaultRoundRules = new()
        {
            DefaultQuestionType = QuestionTypes.NoRisk,
            QuestionSelectionStrategyType = QuestionSelectionStrategyType.SelectByPlayer
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
            QuestionSelectionStrategyType = QuestionSelectionStrategyType.RemoveOtherThemes
        };

        Quiz.RoundRules[RoundTypes.Final] = new()
        {
            DefaultQuestionType = QuestionTypes.StakeAll,
            QuestionSelectionStrategyType = QuestionSelectionStrategyType.Sequential
        };

        TurnTaking.RoundRules[RoundTypes.Final] = new()
        {
            DefaultQuestionType = QuestionTypes.StakeAll,
            QuestionSelectionStrategyType = QuestionSelectionStrategyType.RemoveOtherThemes
        };
    }
}
