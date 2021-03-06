using System.IO;

namespace SIPackages.Core
{
    /// <summary>
    /// Wraps the stream adding <see cref="Length" /> property to it.
    /// </summary>
    public sealed class StreamInfo
    {
        /// <summary>
        /// Wrapped stream.
        /// </summary>
        public Stream Stream { get; set; }

        /// <summary>
        /// Stream length.
        /// </summary>
        public long Length { get; set; }
    }
}
