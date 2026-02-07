namespace SI.GameServer.Contract;

/// <summary>
/// Represents a game event from Server-Sent Events stream.
/// </summary>
public sealed class GameEvent
{
    /// <summary>
    /// Type of the game event.
    /// </summary>
    public GameEventType Type { get; set; }

    /// <summary>
    /// Game info for Created and Changed events.
    /// </summary>
    public GameInfo? GameInfo { get; set; }

    /// <summary>
    /// Game ID for Deleted events.
    /// </summary>
    public int? GameId { get; set; }

    /// <summary>
    /// Games chunk for Snapshot events.
    /// </summary>
    public GameInfo[]? Games { get; set; }

    /// <summary>
    /// Indicates whether this is the last chunk in the snapshot sequence.
    /// </summary>
    public bool IsLastChunk { get; set; }
}
