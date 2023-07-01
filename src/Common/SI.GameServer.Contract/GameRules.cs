namespace SI.GameServer.Contract;

/// <summary>
/// Defines game rules filter.
/// </summary>
[Flags]
public enum GameRules
{
    None = 0,
    FalseStart = 1,
    Oral = 2,
    IgnoreWrong = 4,

    /// <summary>
    /// Ping penalty.
    /// </summary>
    PingPenalty = 8,
}
