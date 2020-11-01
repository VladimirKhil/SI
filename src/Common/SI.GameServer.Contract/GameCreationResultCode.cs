namespace SI.GameServer.Contract
{
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
TooManyGamesByAddress
}
}
