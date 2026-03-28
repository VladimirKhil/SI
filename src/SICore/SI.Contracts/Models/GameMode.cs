namespace SI.Contracts;

/// <summary>
/// Defines game modes.
/// </summary>
public enum GameMode
{
    /// <summary>
    /// Classic mode with question selection.
    /// </summary>
    Classic,

    /// <summary>
    /// Simplified mode with sequential question play.
    /// </summary>
    Sequential,

    /// <summary>
    /// Quiz mode.
    /// </summary>
    Quiz,

    /// <summary>
    /// Turn taking mode.
    /// </summary>
    TurnTaking,
}
