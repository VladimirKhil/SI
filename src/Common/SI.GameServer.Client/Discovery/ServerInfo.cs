namespace SI.GameServer.Client.Discovery
{
    /// <summary>
    /// Defines a game server information.
    /// </summary>
    internal sealed class ServerInfo
    {
        /// <summary>
        /// Server Uri.
        /// </summary>
        public string? Uri { get; set; }

        /// <summary>
        /// Server supported protocol version.
        /// </summary>
        public int ProtocolVersion { get; set; }
    }
}
