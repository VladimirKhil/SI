﻿using SIPackages.Core;
using ZipUtils;

namespace SIPackages.Containers;

/// <summary>
/// Defines a package container based on filesystem folder.
/// </summary>
internal sealed class FolderSIPackageContainer : ISIPackageContainer
{
    private readonly string _folder;
    private readonly IReadOnlyDictionary<string, ExtractedFileInfo> _fileNameMap;
    private readonly bool _deleteOnClose;

    public FolderSIPackageContainer(
        string folder,
        IReadOnlyDictionary<string, ExtractedFileInfo> fileNameMap,
        bool deleteOnClose = true)
    {
        _folder = folder;
        _fileNameMap = fileNameMap;
        _deleteOnClose = deleteOnClose;
    }

    public ISIPackageContainer CopyTo(Stream stream, bool close, out bool isNew) => throw new NotImplementedException();

    internal static ISIPackageContainer Open(string folder, IReadOnlyDictionary<string, ExtractedFileInfo> fileNameMap) =>
        new FolderSIPackageContainer(folder, fileNameMap);

    internal static ISIPackageContainer Create(string folder) => new FolderSIPackageContainer(folder, new Dictionary<string, ExtractedFileInfo>(), false);

    public void CreateStream(string name)
    {
        using (File.Create(Path.Combine(_folder, name))) { }
    }

    public void CreateStream(string category, string name)
    {
        Directory.CreateDirectory(Path.Combine(_folder, category));
        using (File.Create(Path.Combine(_folder, category, name))) { }
    }

    public async Task CreateStreamAsync(
        string category,
        string name,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.Combine(_folder, category));
        using var fs = File.Create(Path.Combine(_folder, category, name));
        await stream.CopyToAsync(fs, cancellationToken);
    }

    public bool DeleteStream(string name) => throw new NotImplementedException();

    public bool DeleteStream(string category, string name) => throw new NotImplementedException();

    public void Dispose()
    {
        if (!_deleteOnClose)
        {
            return;
        }

        Directory.Delete(_folder, true);
    }

    public void Flush() { }

    public IEnumerable<string> GetEntries(string category)
    {
        var directoryInfo = new DirectoryInfo(Path.Combine(_folder, category));

        if (!directoryInfo.Exists)
        {
            return Array.Empty<string>();
        }

        return directoryInfo.GetFiles().Select(file => file.Name);
    }

    public StreamInfo? GetStream(string name, bool read = true)
    {
        var mappedName = GetName(name);

        var file = new FileInfo(Path.Combine(_folder, mappedName));

        if (!file.Exists)
        {
            return null;
        }

        return new StreamInfo(read ? file.OpenRead() : file.Open(FileMode.Open), file.Length);
    }

    public StreamInfo? GetStream(string category, string name, bool read = true) => GetStream($"{category}/{name}", read);

    public long GetStreamLength(string name)
    {
        var mappedName = GetName(name);

        var file = new FileInfo(Path.Combine(_folder, mappedName));

        if (!file.Exists)
        {
            return -1;
        }

        return file.Length;
    }

    public long GetStreamLength(string category, string name) => GetStreamLength($"{category}/{name}");

    public MediaInfo? TryGetMedia(string category, string name)
    {
        var fullName = $"{category}/{name}";
        var mappedName = GetName(fullName);

        var file = new FileInfo(Path.Combine(_folder, mappedName));

        if (!file.Exists)
        {
            return null;
        }

        return new MediaInfo(file.OpenRead, file.Length, new Uri(file.FullName));
    }

    private string GetName(string name)
    {
        if (!_fileNameMap.TryGetValue(name, out var mappedName))
        {
            return name;
        }

        return mappedName.Name;
    }
}
