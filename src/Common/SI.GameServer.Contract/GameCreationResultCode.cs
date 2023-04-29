namespace SI.GameServer.Contract;

/// <summary>
/// Defines game creation result codes.
/// </summary>
public enum GameCreationResultCode
{
    Ok,
    NoPackage,
    TooMuchGames,
    ServerUnderMaintainance,
    BadPackage,
    GameNameCollision,
    InternalServerError,
    ServerNotReady,
    YourClientIsObsolete,
    UnknownError,
    JoinError,
    WrongGameSettings,
    TooManyGamesByAddress,
    UnsupportedPackageType,
    UnsupportedContentUri,
}
