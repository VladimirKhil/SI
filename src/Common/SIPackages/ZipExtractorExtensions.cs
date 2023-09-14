using ZipUtils;

namespace SIPackages;

/// <summary>
/// Provides method to extract package to folder.
/// </summary>
internal static class PackageExtractor
{
    /// <summary>
    /// Extarcts package to folder.
    /// </summary>
    /// <param name="sourceArchiveFilePath">Arhive file path.</param>
    /// <param name="destinationFolderPath">Target folder path.</param>
    /// <param name="maxAllowedDataLength">Maximum allowed length of extracted data in archive.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Map of archive file names to extracted file names.</returns>
    internal static Task<IReadOnlyDictionary<string, string>> ExtractPackageToFolderAsync(
        string sourceArchiveFilePath,
        string destinationFolderPath,
        long maxAllowedDataLength = long.MaxValue,
        CancellationToken cancellationToken = default) =>
        ZipExtractor.ExtractArchiveFileToFolderAsync(
            sourceArchiveFilePath,
            destinationFolderPath,
            new ExtractionOptions(NamingModeSelector)
            {
                MaxAllowedDataLength = maxAllowedDataLength,
                FileFilter = FileFilter
            },
            cancellationToken);

    private static UnzipNamingMode NamingModeSelector(string name) => name switch
    {
        "content.xml" or "Texts/authors.xml" or "Texts/sources.xml" => UnzipNamingMode.KeepOriginal,
        _ => UnzipNamingMode.Hash // This guarantees that we never use user-provided file names
    };

    private static bool FileFilter(string filePath)
    {
        if (filePath == "content.xml")
        {
            return true;
        }

        var folderName = Path.GetDirectoryName(filePath);

        return folderName switch
        {
            "Images" or "Audio" or "Video" or "Html" or "Texts" => true,
            _ => false
        };
    }
}
