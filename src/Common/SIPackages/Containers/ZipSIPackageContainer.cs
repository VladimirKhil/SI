using SIPackages.Core;
using System.IO.Compression;

namespace SIPackages.Containers;

// TODO: Keep Uri.EscapeUriString only for backward compatibility; use original names in zip archive

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

    private ZipSIPackageContainer(Stream stream, ZipArchive zipArchive)
    {
        _stream = stream;
        _zipArchive = zipArchive;
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

    public string[] GetEntries(string category)
    {
        if (_zipArchive.Mode == ZipArchiveMode.Create)
        {
            return Array.Empty<string>();
        }

        return _zipArchive.Entries
            .Where(entry => entry.FullName.StartsWith(category))
            .Select(entry => Uri.UnescapeDataString(entry.Name))
            .ToArray();
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

        return new StreamInfo { Stream = stream, Length = _zipArchive.Mode == ZipArchiveMode.Read ? entry.Length : 0 };
    }

    public StreamInfo? GetStream(string category, string name, bool read = true) => GetStream($"{category}/{Uri.EscapeUriString(name)}", read);

    public void CreateStream(string name, string contentType) => _zipArchive.CreateEntry(Uri.EscapeUriString(name), CompressionLevel.Optimal);

    public void CreateStream(string category, string name, string contentType) =>
        _zipArchive.CreateEntry($"{category}/{Uri.EscapeUriString(name)}", CompressionLevel.NoCompression);

    public async Task CreateStreamAsync(
        string category,
        string name,
        string contentType,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        var entry = _zipArchive.CreateEntry($"{category}/{Uri.EscapeUriString(name)}", CompressionLevel.NoCompression);

        using var writeStream = entry.Open();
        await stream.CopyToAsync(writeStream, cancellationToken);
    }

    public bool DeleteStream(string category, string name)
    {
        var entryName = $"{category}/{Uri.EscapeUriString(name)}";
        var entry = _zipArchive.GetEntry(entryName);

        if (entry == null)
        {
            return false;
        }

        entry.Delete();
        return true;
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
            _stream.Dispose(); // what about _zipPackage?
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

    public long GetStreamLength(string category, string name) => GetStreamLength($"{category}/{Uri.EscapeUriString(name)}");

    public MediaInfo? TryGetMedia(string category, string name)
    {
        var fullName = $"{category}/{Uri.EscapeUriString(name)}";
        var entry = _zipArchive.GetEntry(fullName);

        if (entry == null)
        {
            return null;
        }

        return new MediaInfo(entry.Open, entry.Length, null);
    }
}
