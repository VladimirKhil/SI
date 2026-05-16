using SIData;

namespace SI.GameServer.Contract;

/// <summary>
/// Contains data required to join a game with optional authorization.
/// </summary>
/// <param name="GameId">Game identifier.</param>
/// <param name="UserName">User name.</param>
/// <param name="Role">Role to join.</param>
/// <param name="Sex">User sex.</param>
/// <param name="AuthorizationMode">Authorization mode.</param>
/// <param name="SteamAuthTicket">Steam authentication ticket.</param>
/// <param name="Password">Game password.</param>
/// <param name="Pin">Game PIN.</param>
public sealed record JoinGameRequest2(
    int GameId,
    string UserName,
    GameRole Role,
    Sex Sex,
    AuthorizationMode AuthorizationMode = AuthorizationMode.None,
    string? SteamAuthTicket = null,
    string? Password = null,
    int? Pin = null);
