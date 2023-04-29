namespace SI.GameServer.Contract;

/// <summary>
/// Represents a game creation result.
/// </summary>
public sealed class GameCreationResult
{
    public GameCreationResultCode Code { get; set; }

    public string? ErrorMessage { get; set; }

    public int GameId { get; set; }

    public bool IsHost { get; set; }

    public GameCreationResult() { }

    public GameCreationResult(GameCreationResultCode gameCreationResultCode) => Code = gameCreationResultCode;
}
