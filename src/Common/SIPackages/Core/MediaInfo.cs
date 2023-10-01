namespace SIPackages.Core;

/// <summary>
/// Defines a package media object info.
/// </summary>
public readonly struct MediaInfo
{
    private readonly Lazy<Stream?> _getStream;

    /// <summary>
    /// Does this info contain a link to stream.
    /// </summary>
    public readonly bool HasStream { get; }

    /// <summary>
    /// Media stream.
    /// </summary>
    public readonly Stream? Stream => _getStream.Value;

    /// <summary>
    /// Media stream length.
    /// </summary>
    public readonly long? StreamLength { get; }

    /// <summary>
    /// Media uri (if media could be accessed directly).
    /// </summary>
    public readonly Uri? Uri { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="MediaInfo" /> struct.
    /// </summary>
    /// <param name="getStream">Stream factory.</param>
    /// <param name="streamLength">Stream length.</param>
    /// <param name="uri">Media uri.</param>
    public MediaInfo(Func<Stream> getStream, long streamLength, Uri? uri = null)
    {
        HasStream = true;
        _getStream = new Lazy<Stream?>(getStream);
        StreamLength = streamLength;
        Uri = uri;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="MediaInfo" /> struct.
    /// </summary>
    /// <param name="mediaUri">Media uri.</param>
    /// <param name="streamLength">Media stream length.</param>
    public MediaInfo(string mediaUri, long? streamLength = null)
    {
        HasStream = false;
        _getStream = new Lazy<Stream?>((Stream?)null);
        StreamLength = streamLength;

        if (Uri.TryCreate(mediaUri, UriKind.Absolute, out var uri))
        {
            Uri = uri;
        }
        else
        {
            Uri = null;
        }
    }
}
