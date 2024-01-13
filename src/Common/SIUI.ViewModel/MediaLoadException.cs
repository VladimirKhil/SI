namespace SIUI.ViewModel;

/// <summary>
/// Defines a media load exception.
/// </summary>
public sealed class MediaLoadException : Exception
{
    /// <summary>
    /// Media uri.
    /// </summary>
    public string MediaUri { get; }

    public MediaLoadException(string mediaUri, Exception innerException) : base("Media load error", innerException)
    {
        MediaUri = mediaUri;
    }
}
