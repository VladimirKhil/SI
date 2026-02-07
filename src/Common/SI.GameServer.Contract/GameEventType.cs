namespace SI.GameServer.Contract;

/// <summary>
/// Defines types of game events for Server-Sent Events.
/// </summary>
public enum GameEventType
{
    /// <summary>
    /// Initial snapshot of current games.
    /// </summary>
    Snapshot,

    /// <summary>
    /// A new game was created.
    /// </summary>
    Created,

    /// <summary>
    /// An existing game was changed.
    /// </summary>
    Changed,

    /// <summary>
    /// A game was deleted.
    /// </summary>
    Deleted
}
