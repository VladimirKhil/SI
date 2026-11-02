namespace SICore.Models;

/// <summary>
/// Defines a player answer validation requested from the showman.
/// </summary>
/// <param name="playerIndex">Index of the player that has provided the answer.</param>
/// <param name="answer">Player answer.</param>
internal sealed class AnswerValidation(int playerIndex, string answer)
{
    /// <summary>
    /// Index of the player that has provided the answer.
    /// </summary>
    internal int PlayerIndex { get; set; } = playerIndex;

    /// <summary>
    /// Player answer.
    /// </summary>
    internal string Answer { get; } = answer;

    /// <summary>
    /// Showman verdict (null while the validation is pending).
    /// </summary>
    internal (bool IsRight, double Factor)? Verdict { get; set; }
}
