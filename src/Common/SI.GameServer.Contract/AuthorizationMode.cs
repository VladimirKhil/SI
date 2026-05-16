namespace SI.GameServer.Contract;

/// <summary>
/// Defines supported authorization modes for joining a game.
/// </summary>
public enum AuthorizationMode
{
    /// <summary>
    /// No external authorization is used.
    /// </summary>
    None,

    /// <summary>
    /// Steam authorization is used.
    /// </summary>
    Steam,
}
