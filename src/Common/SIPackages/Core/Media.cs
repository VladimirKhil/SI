using System;

namespace SIPackages.Core
{
    /// <summary>
    /// Represents a common media source (Uri or direct stream).
    /// </summary>
    public sealed class Media: IMedia
    {
        /// <summary>
        /// Gets media as a stream factory.
        /// </summary>
        public Func<StreamInfo> GetStream { get; }

        /// <summary>
        /// Gets media Uri.
        /// </summary>
        public string Uri { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="Media" /> class.
        /// </summary>
        public Media(Func<StreamInfo> getStream, string uri)
        {
            GetStream = getStream;
            Uri = uri;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Media" /> class.
        /// </summary>
        public Media(string uri)
        {
            Uri = uri;
        }
    }
}
