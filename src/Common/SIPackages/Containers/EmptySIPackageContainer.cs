using SIPackages.Core;

namespace SIPackages.Containers;

/// <summary>
/// Defines an empty SI package container.
/// </summary>
/// <inheritdoc cref="ISIPackageContainer" />
public sealed class EmptySIPackageContainer : ISIPackageContainer
{
    /// <summary>
    /// Singleton empty SI package container.
    /// </summary>
    public static readonly EmptySIPackageContainer Instance = new();

    /// <inheritdoc />
    public ISIPackageContainer CopyTo(Stream stream, bool close, out bool isNew)
    {
        isNew = true;
        return ZipSIPackageContainer.Create(stream);
    }

    /// <inheritdoc />
    public void CreateStream(string name, string contentType) => throw new NotImplementedException();

    /// <inheritdoc />
    public void CreateStream(string category, string name, string contentType) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task CreateStreamAsync(
        string category,
        string name,
        string contentType,
        Stream stream,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public bool DeleteStream(string category, string name) => throw new NotImplementedException();

    /// <inheritdoc />
    public void Dispose() { }

    /// <inheritdoc />
    public void Flush() => throw new NotImplementedException();

    /// <inheritdoc />
    public string[] GetEntries(string category) => Array.Empty<string>();

    /// <inheritdoc />
    public StreamInfo? GetStream(string name, bool read = true) => null;

    /// <inheritdoc />
    public StreamInfo? GetStream(string category, string name, bool read = true) => null;

    /// <inheritdoc />
    public long GetStreamLength(string name) => throw new NotImplementedException();

    /// <inheritdoc />
    public long GetStreamLength(string category, string name) => throw new NotImplementedException();

    /// <inheritdoc />
    public MediaInfo? TryGetMedia(string category, string name) => throw new NotImplementedException();
}
