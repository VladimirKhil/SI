namespace SICore.Models;

/// <summary>
/// Defines reasons for selecting a player.
/// </summary>
public enum SelectPlayerReason
{
    /// <summary>
    /// Selects a player to be a chooser.
    /// </summary>
    Chooser,

    /// <summary>
    /// Selects a player to be a next staker.
    /// </summary>
    Staker,

    /// <summary>
    /// Selects a player to be a theme deleter.
    /// </summary>
    Deleter,

    /// <summary>
    /// Selects a player to answer the question.
    /// </summary>
    Answerer,
}
