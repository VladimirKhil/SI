using System;

namespace Services.SI
{
    /// <summary>
    /// Contains package link.
    /// </summary>
    public sealed class PackageLink
    {
        /// <summary>
        /// Package name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Package URI.
        /// </summary>
        public Uri Uri { get; set; }
    }
}
