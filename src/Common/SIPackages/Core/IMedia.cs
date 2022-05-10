using System;

namespace SIPackages.Core
{
    /// <summary>
    /// Defines a package media object.
    /// </summary>
    public interface IMedia
    {
        /// <summary>
        /// Gets media stream information.
        /// </summary>
        Func<StreamInfo> GetStream { get; }

        /// <summary>
        /// Media uri.
        /// </summary>
        string Uri { get; }
    }
}
