using SIUI.Model;

namespace SICore.Contracts;

/// <summary>
/// Defines an intelligence behavior for player.
/// </summary>
internal interface IPlayerIntelligence
{
    /// <summary>
    /// Selects a question on game table.
    /// </summary>
    (int themeIndex, int questionIndex) SelectQuestion(
        List<ThemeInfo> table,
        (int ThemeIndex, int QuestionIndex) previousSelection,
        int currentScore,
        int bestOpponentScore,
        int roundPassedTimePercentage);
}
