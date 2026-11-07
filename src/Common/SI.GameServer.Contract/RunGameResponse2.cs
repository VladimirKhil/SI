namespace SI.GameServer.Contract;

/// <summary>
/// Defines a successful run game response.
/// </summary>
/// <remarks>Success is indicated by the HTTP status code; failures are returned as <see cref="RunGameError2" />.</remarks>
/// <param name="HostUri">Game host uri.</param>
/// <param name="GameId">Game identifier.</param>
/// <param name="IsHost">Should the person be a game host.</param>
public sealed record RunGameResponse2(Uri? HostUri, int GameId, bool IsHost);

/// <summary>
/// Defines a run game error response.
/// </summary>
/// <param name="Code">Creation error code.</param>
public sealed record RunGameError2(GameCreationResultCode Code);
