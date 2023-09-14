using SIPackages.Core;

namespace SIPackages.Containers;

/// <summary>
/// Defines a SIGame package container.
/// </summary>
public interface ISIPackageContainer : IDisposable
{
    /// <summary>
    /// Gets container entries by category.
    /// </summary>
    /// <param name="category">Category name.</param>
    string[] GetEntries(string category);

    /// <summary>
    /// Gets stream length.
    /// </summary>
    /// <param name="name">Object name.</param>
    long GetStreamLength(string name);

    /// <summary>
    /// Gets stream length.
    /// </summary>
    /// <param name="category">Object category.</param>
    /// <param name="name">Object name.</param>
    long GetStreamLength(string category, string name);

    /// <summary>
    /// Gets object stream.
    /// </summary>
    /// <param name="name">Object name.</param>
    /// <param name="read">Will the stream be read (or written to otherwise).</param>
    StreamInfo? GetStream(string name, bool read = true);

    /// <summary>
    /// Gets object stream.
    /// </summary>
    /// <param name="category">Object category.</param>
    /// <param name="name">Object name.</param>
    /// <param name="read">Should a stream be read-only.</param>
    StreamInfo? GetStream(string category, string name, bool read = true);

    /// <summary>
    /// Tries to find object stream and return a media object to access it.
    /// </summary>
    /// <param name="category">Object category.</param>
    /// <param name="name">Object name.</param>
    MediaInfo? TryGetMedia(string category, string name);

    /// <summary>
    /// Creates a stream.
    /// </summary>
    /// <param name="name">Stream name.</param>
    /// <param name="contentType">Stream content type.</param>
    void CreateStream(string name, string contentType);

    /// <summary>
    /// Creates a stream.
    /// </summary>
    /// <param name="category"></param>
    /// <param name="name">Stream name.</param>
    /// <param name="contentType">Stream content type.</param>
    void CreateStream(string category, string name, string contentType);

    /// <summary>
    /// Creates a stream.
    /// </summary>
    /// <param name="category">Stream category.</param>
    /// <param name="name">Stream name.</param>
    /// <param name="contentType">Stream content type.</param>
    /// <param name="stream">Stream data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreateStreamAsync(string category, string name, string contentType, Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a stream.
    /// </summary>
    /// <param name="category">Stream category.</param>
    /// <param name="name">Stream name.</param>
    /// <returns>Has the stream been deleted.</returns>
    bool DeleteStream(string category, string name);

    /// <summary>
    /// Copies the whole source to the stream.
    /// </summary>
    /// <param name="stream">Target stream.</param>
    /// <param name="close">Should this object be closed.</param>
    /// <param name="isNew">Has a new source been created.</param>
    /// <returns>Created copy.</returns>
    ISIPackageContainer CopyTo(Stream stream, bool close, out bool isNew);

    /// <summary>
    /// Flushes container changes.
    /// </summary>
    void Flush();
}
