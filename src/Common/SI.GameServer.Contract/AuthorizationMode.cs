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
    [Obsolete("Steam authorization is deprecated and will be removed in future versions.")]
    Steam,

    /// <summary>
    /// AccountService authorization is used.
    /// </summary>
    Account,
}
