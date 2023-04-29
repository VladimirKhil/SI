namespace SI.GameServer.Contract;

/// <summary>
/// Contains basic information about a game.
/// </summary>
public class SimpleGameInfo
{
    /// <summary>
    /// Unique game identifier.
    /// </summary>
    public int GameID { get; set; }

    /// <summary>
    /// Game name.
    /// </summary>
    public string GameName { get; set; } = "";

    /// <summary>
    /// Does the game require a password to enter.
    /// </summary>
    public bool PasswordRequired { get; set; }
}
