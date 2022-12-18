namespace SIPackages.Core;

/// <summary>
/// Defines a package media object.
/// </summary>
public interface IMedia
{
    /// <summary>
    /// Gets media stream information.
    /// </summary>
    Func<StreamInfo>? GetStream { get; }

    /// <summary>
    /// Gets media stream length.
    /// </summary>
    long StreamLength { get; }

    /// <summary>
    /// Gets Media uri.
    /// </summary>
    string Uri { get; }
}
