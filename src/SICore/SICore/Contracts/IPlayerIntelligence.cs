using SICore.Models;
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

    /// <summary>
    /// Deletes a theme from game table.
    /// </summary>
    int DeleteTheme(List<ThemeInfo> roundTable);

    /// <summary>
    /// Selects a player to answer the question.
    /// </summary>
    int SelectPlayer(
        List<PlayerAccount> players,
        int myIndex,
        List<ThemeInfo> roundTable,
        int roundPassedTimePercentage);

    /// <summary>
    /// Makes a stake.
    /// </summary>
    (StakeModes mode, int sum) MakeStake(
        List<PlayerAccount> players,
        int myIndex,
        List<ThemeInfo> roundTable,
        StakeInfo stakeInfo,
        int questionIndex,
        int previousStakerIndex,
        bool[] vars,
        int roundPassedTimePercentage);

    /// <summary>
    /// Handles question start.
    /// </summary>
    void OnQuestionStart(bool shortThink, double difficulty);

    /// <summary>
    /// Handles button press start.
    /// </summary>
    int OnStartCanPressButton();

    /// <summary>
    /// Handles button press end.
    /// </summary>
    void OnEndCanPressButton();

    /// <summary>
    /// Gives the answer.
    /// </summary>
    (bool knows, bool isSure, int answerTime) OnAnswer();

    /// <summary>
    /// Handles player outcome.
    /// </summary>
    void OnPlayerOutcome(
        List<PlayerAccount> players,
        int myIndex,
        int playerIndex,
        List<ThemeInfo> roundTable,
        bool isRight,
        int roundPassedTimePercentage);
}
