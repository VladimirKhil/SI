namespace SICore.Network;

public static class NetworkConstants
{
    /// <summary>
    /// Game entity name, used for communication with the game.
    /// </summary>
    public const string GameName = "@";

    /// <summary>
    /// Multicast receiver.
    /// </summary>
    public const string Everybody = "*";

    /// <summary>
    /// Node receiver, used for communication with the other nodes.
    /// </summary>
    public const string Node = "_";
}
