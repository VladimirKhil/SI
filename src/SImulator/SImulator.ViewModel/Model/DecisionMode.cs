namespace SImulator.ViewModel.Model;

/// <summary>
/// Defines player decision mode.
/// </summary>
public enum DecisionMode
{
    /// <summary>
    /// No decision required.
    /// </summary>
    None,

    /// <summary>
    /// Choosing player to start game.
    /// </summary>
    StarterChoosing,

    /// <summary>
    /// Choosing question answerer.
    /// </summary>
    AnswererChoosing,

    /// <summary>
    /// Making simple stake.
    /// </summary>
    SimpleStake,

    /// <summary>
    /// Making stake.
    /// </summary>
    Stake,
}
