namespace SI.Contracts.Models;

/// <summary>
/// Defines a computer player or showman account data.
/// </summary>
/// <remarks>All probabilities are integer percents in the range 0 - 100.</remarks>
public sealed class ComputerAccount
{
    /// <summary>Account name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Account gender.</summary>
    public Gender Gender { get; set; }

    /// <summary>Account avatar URI.</summary>
    public string AvatarUri { get; set; } = "";

    /// <summary>Play style.</summary>
    public PlayerStyle Style { get; set; }

    /// <summary>Erudition.</summary>
    public int Erudition { get; set; }

    /// <summary>Reaction speed.</summary>
    public int ReactionSpeed { get; set; }

    /// <summary>Default boldness (stakes aggressiveness).</summary>
    public int Boldness { get; set; }

    /// <summary>Probability to select the player with less money.</summary>
    public int LessMoneyPlayerProbability { get; set; }

    /// <summary>Favorite numbers.</summary>
    public int[] FavoriteNumbers { get; set; } = [];

    /// <summary>Theme choosing behavior.</summary>
    public ThemeChoiceSettings ThemeChoice { get; set; } = new();

    /// <summary>Question choosing behavior.</summary>
    public QuestionChoiceSettings QuestionChoice { get; set; } = new();

    /// <summary>Pass and all-in behavior.</summary>
    public RiskSettings Risk { get; set; } = new();

    /// <summary>Critical situation detection thresholds.</summary>
    public CriticalSituationSettings CriticalSituation { get; set; } = new();
}

/// <summary>
/// Defines how a computer player chooses a theme.
/// </summary>
/// <remarks>The remaining probability (up to 100) means random theme.</remarks>
public sealed class ThemeChoiceSettings
{
    /// <summary>Probability to choose the theme by its number.</summary>
    public int ByNumberProbability { get; set; }

    /// <summary>Probability to stay in the same theme as before.</summary>
    public int SameThemeProbability { get; set; }

    /// <summary>Probability to choose the theme by priority.</summary>
    public int ByPriorityProbability { get; set; }

    /// <summary>Theme priority: theme numbers (1-9) from most to least preferred.</summary>
    public int[] Priority { get; set; } = [];
}

/// <summary>
/// Defines how a computer player chooses a question.
/// </summary>
/// <remarks>The remaining probability (up to 100) means random question.</remarks>
public sealed class QuestionChoiceSettings
{
    /// <summary>Probability to choose a cheaper question than the previous one.</summary>
    public int CheaperProbability { get; set; }

    /// <summary>Probability to choose a more expensive question than the previous one.</summary>
    public int MoreExpensiveProbability { get; set; }

    /// <summary>Probability to choose a question of the same price.</summary>
    public int SamePriceProbability { get; set; }

    /// <summary>Probability to choose the question by priority.</summary>
    public int ByPriorityProbability { get; set; }

    /// <summary>Question priority: question numbers (1-9) from most to least preferred.</summary>
    public int[] Priority { get; set; } = [];
}

/// <summary>
/// Defines pass and all-in behavior.
/// </summary>
public sealed class RiskSettings
{
    /// <summary>Probability to pass on a first category question.</summary>
    public int PassOnFirstCategoryProbability { get; set; }

    /// <summary>Probability to pass on a last category question.</summary>
    public int PassOnLastCategoryProbability { get; set; }

    /// <summary>Probability to go all-in on a first category question.</summary>
    public int AllInOnFirstCategoryProbability { get; set; }

    /// <summary>Probability to go all-in on a last category question.</summary>
    public int AllInOnLastCategoryProbability { get; set; }
}

/// <summary>
/// Defines when the situation is considered critical for a computer player.
/// </summary>
public sealed class CriticalSituationSettings
{
    /// <summary>Maximum number of questions left till the end of the round when the situation can become critical.</summary>
    public int MaxQuestionsLeft { get; set; }

    /// <summary>Minimum own score share relative to the leader score when the situation can become critical.</summary>
    public int MinScoreShare { get; set; }
}
