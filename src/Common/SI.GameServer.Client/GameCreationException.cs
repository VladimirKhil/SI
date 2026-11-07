using SI.GameServer.Contract;

namespace SI.GameServer.Client;

/// <summary>
/// Represents a game creation failure reported by the game server.
/// </summary>
public sealed class GameCreationException(GameCreationResultCode code, string? message = null) : Exception(message ?? code.ToString())
{
    /// <summary>
    /// Creation error code.
    /// </summary>
    public GameCreationResultCode Code { get; } = code;
}
