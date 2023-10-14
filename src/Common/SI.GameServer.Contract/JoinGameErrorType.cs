namespace SI.GameServer.Contract;

/// <summary>
/// Defines join game error types.
/// </summary>
public enum JoinGameErrorType
{
    /// <summary>
    /// Invalid role value.
    /// </summary>
    InvalidRole,

    /// <summary>
    /// Game with provided identifier not found.
    /// </summary>
    GameNotFound,

    /// <summary>
    /// Internal server error.
    /// </summary>
    InternalServerError,

    /// <summary>
    /// Forbidden to join this game (you are banned).
    /// </summary>
    Forbidden,

    /// <summary>
    /// Common join error.
    /// </summary>
    CommonJoinError,
}
