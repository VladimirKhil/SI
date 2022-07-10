using System;

namespace SI.GameServer.Contract
{
    public sealed class HostInfo
    {
        public string Name { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        [Obsolete]
        public string PackagesPublicBaseUrl { get; set; }

        public string[] ContentPublicBaseUrls { get; set; }

        public string License { get; set; }

        /// <summary>
        /// Maximum allowed package size in MB.
        /// </summary>
        public int MaxPackageSizeMb { get; set; } = 100;
    }
}
