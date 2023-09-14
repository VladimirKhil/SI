using SIPackages.Containers;
using SIPackages.Core;

namespace SIEngine.Tests;

/// <summary>
/// Defines a fake package source.
/// </summary>
internal sealed class PackageContainerMock : ISIPackageContainer
{
    private readonly Dictionary<string, HashSet<string>> _streams = new();

    public ISIPackageContainer CopyTo(Stream stream, bool close, out bool isNew) => throw new NotImplementedException();

    public void CreateStream(string name, string contentType) => CreateStream("", name, contentType);

    public void CreateStream(string category, string name, string contentType)
    {
        if (!_streams.TryGetValue(category, out var categoryStreams))
        {
            _streams[category] = categoryStreams = new HashSet<string>();
        }

        categoryStreams.Add(name);
    }

    public Task CreateStreamAsync(
        string category,
        string name,
        string contentType,
        Stream stream,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public bool DeleteStream(string category, string name) => throw new NotImplementedException();

    public void Dispose() => throw new NotImplementedException();

    public void Flush() => throw new NotImplementedException();

    public string[] GetEntries(string category)
    {
        if (!_streams.TryGetValue(category, out var categoryStreams))
        {
            return Array.Empty<string>();
        }

        return categoryStreams.ToArray();
    }

    public StreamInfo GetStream(string name, bool read = true) => throw new NotImplementedException();

    public StreamInfo GetStream(string category, string name, bool read = true) => throw new NotImplementedException();

    public long GetStreamLength(string name) => throw new NotImplementedException();

    public long GetStreamLength(string category, string name) => throw new NotImplementedException();

    public MediaInfo? TryGetMedia(string category, string name) => throw new NotImplementedException();
}
