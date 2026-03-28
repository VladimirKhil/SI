using SIPackages.Containers;
using SIPackages.Core;

namespace SIPackages;

/// <summary>
/// Defines a package files storage. All files belong to a single category.
/// </summary>
/// <inheritdoc cref="IEnumerable{T}" />
public sealed class DataCollection : IEnumerable<string>
{
    private ISIPackageContainer _packageContainer;
    private readonly Action<string>? _fileChanged;
    private readonly Action<string>? _fileRemoved;
    private readonly Action<string, string>? _fileRenamed;

    /// <summary>
    /// Current items in the collection.
    /// </summary>
    private readonly List<string> _files;

    /// <summary>
    /// Collection name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Collection item count.
    /// </summary>
    public int Count => _files.Count;

    /// <summary>
    /// Initilizes a new instance of <see cref="DataCollection" /> class.
    /// </summary>
    /// <param name="package">Package that owns the collection.</param>
    /// <param name="name">Collection name.</param>
    internal DataCollection(
        ISIPackageContainer package,
        string name,
        Action<string>? fileChanged = null,
        Action<string>? fileRemoved = null,
        Action<string, string>? fileRenamed = null)
    {
        Name = name;
        _packageContainer = package;
        _fileChanged = fileChanged;
        _fileRemoved = fileRemoved;
        _fileRenamed = fileRenamed;

        _files = new List<string>(_packageContainer.GetEntries(Name));
    }

    /// <summary>
    /// Checks if the collection contains a file.
    /// </summary>
    /// <param name="fileName">File name.</param>
    internal bool Contains(string fileName) => _files.Contains(fileName);

    /// <summary>
    /// Gets collection file.
    /// </summary>
    /// <param name="fileName">File name.</param>
    public StreamInfo? GetFile(string fileName) => _packageContainer.GetStream(Name, fileName);

    /// <summary>
    /// Gets collection file length.
    /// </summary>
    /// <param name="fileName">File name.</param>
    public long GetFileLength(string fileName) => _packageContainer.GetStreamLength(Name, fileName);

    /// <inheritdoc />
    public IEnumerator<string> GetEnumerator() => _files.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Adds file to the collection.
    /// </summary>
    /// <param name="fileName">File name.</param>
    /// <param name="stream">File stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task AddFileAsync(string fileName, Stream stream, CancellationToken cancellationToken = default)
    {
        await _packageContainer.CreateStreamAsync(Name, fileName, stream, cancellationToken);

        if (!_files.Contains(fileName))
        {
            _files.Add(fileName);
        }

        _fileChanged?.Invoke(fileName);
    }

    /// <summary>
    /// Removes file from the collection.
    /// </summary>
    /// <param name="fileName">File name.</param>
    public void RemoveFile(string fileName)
    {
        var deleted = _packageContainer.DeleteStream(Name, fileName);
        var removed = _files.Remove(fileName);

        if (deleted || removed)
        {
            _fileRemoved?.Invoke(fileName);
        }
    }

    /// <summary>
    /// Renames a file.
    /// </summary>
    /// <param name="oldName">Old file name.</param>
    /// <param name="newName">New file name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RenameFileAsync(string oldName, string newName, CancellationToken cancellationToken = default)
    {
        var streamInfo = _packageContainer.GetStream(Name, oldName)
            ?? throw new InvalidOperationException($"Cannot rename file {oldName}: file does not exist");
        
        using (var stream = streamInfo.Stream)
        {
            await _packageContainer.CreateStreamAsync(Name, newName, stream, cancellationToken);
        }

        _files.Add(newName);
        _packageContainer.DeleteStream(Name, oldName);
        _files.Remove(oldName);
        _fileRenamed?.Invoke(oldName, newName);
    }

    internal void UpdateContainer(ISIPackageContainer packageContainer) => _packageContainer = packageContainer;
}
