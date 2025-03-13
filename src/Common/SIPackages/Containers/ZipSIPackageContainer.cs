using SIPackages.Core;
using System.IO.Compression;

namespace SIPackages.Containers;

/// <summary>
/// Define a Zip-based package source.
/// </summary>
/// <remarks>
/// Inner file names could be Uri-escaped.
/// </remarks>
/// <inheritdoc cref="ISIPackageContainer" />
internal sealed class ZipSIPackageContainer : ISIPackageContainer
{
    private readonly Stream _stream;
    private readonly ZipArchive _zipArchive;
    private readonly Dictionary<string, Dictionary<string, string>> _nameMap = new();

    private ZipSIPackageContainer(Stream stream, ZipArchive zipArchive)
    {
        _stream = stream;
        _zipArchive = zipArchive;

        // Fill the categories name maps
        foreach (var entry in _zipArchive.Entries)
        {
            var nameSegments = entry.FullName.Split('/', 2);

            if (nameSegments.Length < 2)
            {
                continue;
            }

            var category = nameSegments[0];
            var name = Uri.UnescapeDataString(entry.Name);
            
            if (!_nameMap.TryGetValue(category, out var categoryMap))
            {
                _nameMap[category] = categoryMap = new Dictionary<string, string>();
            }

            categoryMap[name] = entry.Name;
        }
    }

    /// <summary>
    /// Creates a new instance of <see cref="ZipSIPackageContainer" /> class.
    /// </summary>
    /// <param name="stream">Stream that would contain package data.</param>
    /// <param name="leaveOpen">Should the stream be left open after packages disposal.</param>
    /// <returns>Created package.</returns>
    public static ZipSIPackageContainer Create(Stream stream, bool leaveOpen = false) =>
        new(stream, new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen));

    public static ZipSIPackageContainer Open(Stream stream, bool read = true) =>
        new(stream, new ZipArchive(stream, read ? ZipArchiveMode.Read : ZipArchiveMode.Update, false));

    public IEnumerable<string> GetEntries(string category)
    {
        if (_zipArchive.Mode == ZipArchiveMode.Create)
        {
            return Array.Empty<string>();
        }

        if (!_nameMap.TryGetValue(category, out var categoryMap))
        {
            return Array.Empty<string>();
        }

        return categoryMap.Keys;
    }

    public StreamInfo? GetStream(string name, bool read = true)
    {
        var entry = _zipArchive.GetEntry(name);

        if (entry == null)
        {
            return null;
        }

        var stream = entry.Open();

        if (!read)
        {
            stream.SetLength(0);
        }

        return new StreamInfo(stream, _zipArchive.Mode == ZipArchiveMode.Read ? entry.Length : 0);
    }

    public StreamInfo? GetStream(string category, string name, bool read = true)
    {
        if (!_nameMap.TryGetValue(category, out var categoryMap))
        {
            return null;
        }

        if (!categoryMap.TryGetValue(name, out var entryName))
        {
            return null;
        }

        return GetStream($"{category}/{entryName}", read);
    }

    public void CreateStream(string name) => _zipArchive.CreateEntry(name, CompressionLevel.Optimal);

    public void CreateStream(string category, string name)
    {
        var entryName = Uri.EscapeUriString(name); // TODO: do not escape after everyone updates to the new version
        _zipArchive.CreateEntry($"{category}/{entryName}", CompressionLevel.NoCompression);

        if (!_nameMap.TryGetValue(category, out var categoryMap))
        {
            _nameMap[category] = categoryMap = new Dictionary<string, string>();
        }

        categoryMap[name] = entryName;
    }

    public async Task CreateStreamAsync(
        string category,
        string name,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        var entryName = Uri.EscapeUriString(name); // TODO: do not escape after everyone updates to the new version
        var entry = _zipArchive.CreateEntry($"{category}/{entryName}", CompressionLevel.NoCompression);

        if (!_nameMap.TryGetValue(category, out var categoryMap))
        {
            _nameMap[category] = categoryMap = new Dictionary<string, string>();
        }

        categoryMap[name] = entryName;

        using var writeStream = entry.Open();
        await stream.CopyToAsync(writeStream, cancellationToken);
    }

    public bool DeleteStream(string name)
    {
        var entry = _zipArchive.GetEntry(name);

        if (entry == null)
        {
            return false;
        }

        entry.Delete();
        return true;
    }

    public bool DeleteStream(string category, string name)
    {
        if (!_nameMap.TryGetValue(category, out var categoryMap))
        {
            return false;
        }

        if (!categoryMap.TryGetValue(name, out var entryName))
        {
            return false;
        }

        return DeleteStream($"{category}/{entryName}");
    }

    public ISIPackageContainer CopyTo(Stream stream, bool closeCurrent, out bool isNew)
    {
        if (_stream.Length == 0)
        {
            // It is a new package. There is nothing to copy
            isNew = true;
            return Create(stream);
        }

        isNew = false;

        _stream.Position = 0; // strictly required
        _stream.CopyTo(stream);
        stream.Position = 0;

        // Reopening current package
        if (closeCurrent)
        {
            _stream.Dispose(); // what about _zipArchive?
        }

        return Open(stream, false);
    }

    public void Dispose() => _zipArchive.Dispose();

    public void Flush() => _stream.Flush();

    public long GetStreamLength(string name)
    {
        var entry = _zipArchive.GetEntry(name);
        return entry?.Length ?? -1;
    }

    public long GetStreamLength(string category, string name)
    {
        if (!_nameMap.TryGetValue(category, out var categoryMap))
        {
            return -1;
        }

        if (!categoryMap.TryGetValue(name, out var entryName))
        {
            return -1;
        }

        return GetStreamLength($"{category}/{entryName}");
    }

    public MediaInfo? TryGetMedia(string category, string name)
    {
        if (!_nameMap.TryGetValue(category, out var categoryMap))
        {
            return null;
        }

        if (!categoryMap.TryGetValue(name, out var entryName))
        {
            return null;
        }

        var fullName = $"{category}/{entryName}";
        var entry = _zipArchive.GetEntry(fullName);

        if (entry == null)
        {
            return null;
        }

        return new MediaInfo(entry.Open, entry.Length, null);
    }
}
