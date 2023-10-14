using SIData;

namespace SI.GameServer.Contract;

/// <summary>
/// Contains data required to join a game.
/// </summary>
/// <param name="GameId">Game identifier.</param>
/// <param name="UserName">User name.</param>
/// <param name="Role">Role to join.</param>
/// <param name="Sex">User sex.</param>
/// <param name="Password">Game password.</param>
public sealed record JoinGameRequest(int GameId, string UserName, GameRole Role, Sex Sex, string? Password = null);
