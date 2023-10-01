using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SIPackages.Helpers;

/// <summary>
/// Provides helper methods for working with zip archives.
/// </summary>
[Obsolete("Use ZipUtils.ZipExtractor")]
public static class ZipHelper
{
    private const int DefaultMaxArchiveDataLength = 2 * 100 * 1024 * 1024;

    /// <summary>
    /// Defines a maxumim file length on current platform.
    /// </summary>
    internal static int MaxFileNameLength =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 100 : 255 / 2; // / 2 because of 2-byte non-ASCII symbols

    /// <summary>
    /// Etracts zip archive to directory.
    /// </summary>
    /// <param name="sourceArchiveFilePath">Arhive file path.</param>
    /// <param name="destinationDirectoryPath">Target directory path.</param>
    /// <param name="extractedFileNamingMode">Extracted files naming mode.</param>
    /// <param name="maxAllowedDataLength">
    /// Maximum allowed length of extracted data in the archive.
    /// <see cref="DefaultMaxArchiveDataLength" /> by default.
    /// </param>
    /// <param name="entryFilter">Optional filter for archive entries.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of filtered file names.</returns>
    /// <exception cref="InvalidOperationException" />
    public static async Task<string[]> ExtractToDirectoryAsync(
        string sourceArchiveFilePath,
        string destinationDirectoryPath,
        ExtractedFileNamingModes extractedFileNamingMode = ExtractedFileNamingModes.KeepOriginal,
        long maxAllowedDataLength = DefaultMaxArchiveDataLength,
        Predicate<ZipArchiveEntry>? entryFilter = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(destinationDirectoryPath);

        using var stream = File.OpenRead(sourceArchiveFilePath);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        // Quickly check the value from the zip header
        var declaredSize = archive.Entries.Sum(entry => entry.Length);

        if (declaredSize > maxAllowedDataLength)
        {
            throw new InvalidOperationException($"Archive data is too big ({declaredSize} bytes)");
        }

        var skippedFiles = new List<string>();

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entryFilter != null && !entryFilter(entry))
            {
                skippedFiles.Add(entry.FullName);
                continue;
            }

            await ExtractEntryToDirectoryAsync(entry, destinationDirectoryPath, extractedFileNamingMode, cancellationToken);
        }

        return skippedFiles.ToArray();
    }

    /// <summary>
    /// Creates directory for entity extraction and returns target file name.
    /// </summary>
    /// <param name="entry">Entry to extract.</param>
    /// <param name="destinationDirectoryPath">Target directory path.</param>
    /// <param name="extractedFileNamingMode">Extracted files naming mode.</param>
    /// <returns>Target entity file name or null if file extraction must be skipped.</returns>
    public static string? PrepareForExtraction(
        ZipArchiveEntry entry,
        string destinationDirectoryPath,
        ExtractedFileNamingModes extractedFileNamingMode = ExtractedFileNamingModes.KeepOriginal)
    {
        var directorySeparatorIndex = entry.FullName.IndexOf(Path.AltDirectorySeparatorChar);

        string targetDir;

        if (directorySeparatorIndex > -1)
        {
            var subDirectoryName = entry.FullName[..directorySeparatorIndex];

            if (!ValidateDirectoryName(subDirectoryName))
            {
                return null;
            }

            targetDir = Path.Combine(destinationDirectoryPath, subDirectoryName);
            Directory.CreateDirectory(targetDir);
        }
        else
        {
            if (!ValidateRootFileName(entry.Name))
            {
                return null;
            }

            targetDir = destinationDirectoryPath;
        }

        var name = extractedFileNamingMode == ExtractedFileNamingModes.Unescape ? Uri.UnescapeDataString(entry.Name) : entry.Name;

        if (name != SIDocument.ContentFileName &&
            name != SIDocument.AuthorsFileName &&
            name != SIDocument.SourcesFileName &&
            (extractedFileNamingMode == ExtractedFileNamingModes.Hash || name.Length > MaxFileNameLength))
        {
            name = CalculateHash(name);
        }

        return Path.Combine(targetDir, name);
    }

    private static bool ValidateRootFileName(string fileName) =>
        fileName == SIDocument.ContentFileName;

    private static bool ValidateDirectoryName(string subDirectoryName) =>
        subDirectoryName == CollectionNames.TextsStorageName ||
        subDirectoryName == CollectionNames.ImagesStorageName ||
        subDirectoryName == CollectionNames.AudioStorageName ||
        subDirectoryName == CollectionNames.VideoStorageName ||
        subDirectoryName == CollectionNames.HtmlStorageName;

    /// <summary>
    /// Extracts entity to directory.
    /// </summary>
    /// <param name="entry">Entry to extract.</param>
    /// <param name="destinationDirectoryPath">Target directory path.</param>
    /// <param name="extractedFileNamingMode">Extracted files naming mode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Target file name if too long.</exception>
    public static async Task ExtractEntryToDirectoryAsync(
        ZipArchiveEntry entry,
        string destinationDirectoryPath,
        ExtractedFileNamingModes extractedFileNamingMode = ExtractedFileNamingModes.KeepOriginal,
        CancellationToken cancellationToken = default)
    {
        var targetFile = PrepareForExtraction(entry, destinationDirectoryPath, extractedFileNamingMode);

        if (targetFile == null)
        {
            // Skipping file extraction
            return;
        }

        if (Path.GetFileName(targetFile).Length > MaxFileNameLength)
        {
            throw new InvalidOperationException(
                $"Too long target file name: \"{targetFile}\", entry.Name: \"{entry.Name}\". " +
                $"Maximum allowed length: {MaxFileNameLength}");
        }

        if (!entry.FullName.EndsWith(Path.AltDirectorySeparatorChar)) // Not a directory
        {
            await ZipFileExtensionsPatched.ExtractToFileAsync(entry, targetFile, true, cancellationToken);
        }
    }

    /// <summary>
    /// Creates a unqiue value hash. Used when an original value is too long to be used as a file name.
    /// </summary>
    /// <param name="value">Value to hash.</param>
    /// <returns>Hashed value.</returns>
    public static string CalculateHash(string value)
    {
        var hashedValue = 3074457345618258791ul;

        for (var i = 0; i < value.Length; i++)
        {
            hashedValue += value[i];
            hashedValue *= 3074457345618258799ul;
        }

        var result = hashedValue.ToString("X2");

        var extIndex = value.LastIndexOf('.');

        if (extIndex > -1)
        {
            var extLength = Math.Min(4, value.Length - extIndex - 1);
            result += '.' + Regex.Replace(value.Substring(extIndex + 1, extLength), "[^a-zA-Z0-9]+", "");
        }

        return result;
    }
}
