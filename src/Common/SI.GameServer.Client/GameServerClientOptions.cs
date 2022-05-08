using System;

namespace SI.GameServer.Client
{
    /// <summary>
    /// Provides SIGame server client options.
    /// </summary>
    public sealed class GameServerClientOptions
    {
        /// <summary>
        /// SIGame server Uri.
        /// </summary>
        public string ServiceUri { get; set; }

        /// <summary>
        /// Client timeout.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(6); // Large value for uploading packages
    }
}
