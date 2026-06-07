namespace SI.GameServer.Contract;

/// <summary>
/// Defines JoinGame2 result types.
/// </summary>
public enum JoinGame2Result
{
    /// <summary>
    /// Successful join.
    /// </summary>
    Success,

    /// <summary>
    /// Invalid role value.
    /// </summary>
    InvalidRole,

    /// <summary>
    /// Invalid user name value.
    /// </summary>
    InvalidUserName,

    /// <summary>
    /// Game with provided identifier not found.
    /// </summary>
    GameNotFound,

    /// <summary>
    /// Internal server error.
    /// </summary>
    InternalServerError,

    /// <summary>
    /// Forbidden to join this game.
    /// </summary>
    Forbidden,

    /// <summary>
    /// Common join error.
    /// </summary>
    CommonJoinError,

    /// <summary>
    /// Authorization mode is not supported.
    /// </summary>
    AuthorizationModeNotSupported,

    /// <summary>
    /// Authorization data is missing.
    /// </summary>
    AuthorizationDataMissing,

    /// <summary>
    /// Authorization failed.
    /// </summary>
    AuthorizationFailed,

    /// <summary>
    /// Authorization service error.
    /// </summary>
    AuthorizationServiceError,

    /// <summary>
    /// Indicates that the authorization failed due to an invalid username.
    /// </summary>
    AuthorizationInvalidUserName,

    /// <summary>
    /// Role is forbidden.
    /// </summary>
    ForbiddenRole,

    /// <summary>
    /// Wrong password.
    /// </summary>
    WrongPassword,

    /// <summary>
    /// Name is already occupied.
    /// </summary>
    NameIsOccupied,

    /// <summary>
    /// Position was not found.
    /// </summary>
    PositionNotFound,

    /// <summary>
    /// Place is already occupied.
    /// </summary>
    PlaceIsOccupied,

    /// <summary>
    /// Free place was not found.
    /// </summary>
    FreePlaceNotFound,
}
