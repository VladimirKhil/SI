namespace SIPackages.Containers;

/// <summary>
/// Provides methods for creating package containers.
/// </summary>
public static class PackageContainerFactory
{
    /// <summary>
    /// Creates a new package container based on stream.
    /// </summary>
    /// <param name="stream">Container stream.</param>
    /// <param name="leaveOpen">Should the stream be left open after container closing.</param>
    public static ISIPackageContainer CreatePackageContainer(Stream stream, bool leaveOpen = false) =>
        ZipSIPackageContainer.Create(stream, leaveOpen);

    /// <summary>
    /// Creates a new package container based on folder.
    /// </summary>
    /// <param name="folder">Container folder.</param>
    public static ISIPackageContainer CreatePackageContainer(string folder) => FolderSIPackageContainer.Create(folder);

    /// <summary>
    /// Gets a package container based on folder.
    /// </summary>
    /// <param name="folder">Container folder.</param>
    /// <param name="fileNameMap">File name map.</param>
    public static ISIPackageContainer GetPackageContainer(string folder, IReadOnlyDictionary<string, string> fileNameMap) =>
        FolderSIPackageContainer.Open(folder, fileNameMap);

    /// <summary>
    /// Gets a package container based on stream.
    /// </summary>
    /// <param name="stream">Container stream.</param>
    /// <param name="read">Should the container be opened in a read-only mode.</param>
    public static ISIPackageContainer GetPackageContainer(Stream stream, bool read = true) => ZipSIPackageContainer.Open(stream, read);
}
