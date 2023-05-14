using SIPackages.Core;

namespace SIEngine.Rules;

/// <summary>
/// Defines round rules.
/// </summary>
public sealed class RoundRules
{
    /// <summary>
    /// Question selection strategy type.
    /// </summary>
    public QuestionSelectionStrategyType QuestionSelectionStrategyType { get; set; }

    /// <summary>
    /// Default question type.
    /// </summary>
    public string DefaultQuestionType { get; set; } = QuestionTypes.Simple;
}
