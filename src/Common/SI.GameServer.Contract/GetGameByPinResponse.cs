namespace SI.GameServer.Contract;

/// <summary>
/// Defines a response to a request to get a game by its PIN.
/// </summary>
/// <param name="HostUri">Game host uri.</param>
/// <param name="GameId">Game identifier.</param>
public sealed record GetGameByPinResponse(Uri HostUri, int GameId);
