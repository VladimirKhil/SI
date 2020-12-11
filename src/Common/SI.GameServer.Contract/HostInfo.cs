using System;

namespace SI.GameServer.Contract
{
    public sealed class HostInfo
    {
        public string Host { get; set; }
        public int Port { get; set; }

        [Obsolete]
        public string PackagesPublicBaseUrl { get; set; }

        public string[] ContentPublicBaseUrls { get; set; }
    }
}
