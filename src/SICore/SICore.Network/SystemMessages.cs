namespace SICore.Network;

public static class SystemMessages
{
    /// <summary>
    /// Разрыв соединения с игрой
    /// </summary>
    [Obsolete("Remove after switching to SIGame 8")]
    public const string Disconnect = "DISCONNECT";

    /// <summary>
    /// Game has been closed by server.
    /// </summary>
    public const string GameClosed = "GAME_CLOSED";

    /// <summary>
    /// Отказ в подключении
    /// </summary>
    public const string Refuse = "REFUSE";
}
