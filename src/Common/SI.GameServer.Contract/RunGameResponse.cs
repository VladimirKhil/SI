namespace SI.GameServer.Contract;

/// <summary>
/// Defines a run game response.
/// </summary>
public sealed class RunGameResponse
{
    /// <summary>
    /// Is run successfull.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Game host uri.
    /// </summary>
    public Uri? HostUri { get; set; }

    /// <summary>
    /// Game identifier.
    /// </summary>
    public int GameId { get; set; }

    /// <summary>
    /// Should the person be a game host.
    /// </summary>
    public bool IsHost { get; set; }

    /// <summary>
    /// Creation error code.
    /// </summary>
    public GameCreationResultCode ErrorType { get; set; }
}
