using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace SIPackages
{
    /// <summary>
    /// Provides helper methods for working with zip archives.
    /// </summary>
    public static class ZipHelper
    {
        /// <summary>
        /// Defines a maxumim file length on current platform.
        /// </summary>
        internal static int MaxFileNameLength = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 100 : 255 / 2; // / 2 because of 2-byte non-ASCII symbols

        /// <summary>
        /// Etracts zip archive to directory.
        /// </summary>
        /// <param name="sourceArchiveFilePath">Arhive file path.</param>
        /// <param name="destinationDirectoryPath">Target directory path.</param>
        /// <param name="unescape">Should extracted items be unescaped.</param>
        public static void ExtractToDirectory(string sourceArchiveFilePath, string destinationDirectoryPath, bool unescape = false)
        {
            Directory.CreateDirectory(destinationDirectoryPath);

            using var stream = File.OpenRead(sourceArchiveFilePath);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            foreach (var entry in archive.Entries)
            {
                ExtractEntryToDirectory(entry, destinationDirectoryPath, unescape);
            }
        }

        /// <summary>
        /// Creates directory for entity extraction and returns target file name.
        /// </summary>
        /// <param name="entry">Entry to extract.</param>
        /// <param name="destinationDirectoryPath">Target directory path.</param>
        /// <param name="unescape">Should extracted items be unescaped.</param>
        /// <returns>Target entity file name.</returns>
        public static string PrepareForExtraction(ZipArchiveEntry entry, string destinationDirectoryPath, bool unescape)
        {
            var name = unescape ? Uri.UnescapeDataString(entry.Name) : entry.Name;
            if (name.Length > MaxFileNameLength)
            {
                name = CalculateHash(name);
            }

            var index = entry.FullName.IndexOf('/');
            var targetDir = index > -1 ? Path.Combine(destinationDirectoryPath, entry.FullName[..index]) : destinationDirectoryPath;
            
            Directory.CreateDirectory(targetDir);

            return Path.Combine(targetDir, name);
        }

        /// <summary>
        /// Extracts entity to directory.
        /// </summary>
        /// <param name="entry">Entry to extract.</param>
        /// <param name="destinationDirectoryPath">Target directory path.</param>
        /// <param name="unescape">Should extracted items be unescaped.</param>
        /// <exception cref="ArgumentOutOfRangeException">Target file name if too long.</exception>
        public static void ExtractEntryToDirectory(ZipArchiveEntry entry, string destinationDirectoryPath, bool unescape = false)
        {
            var targetFile = PrepareForExtraction(entry, destinationDirectoryPath, unescape);

            if (Path.GetFileName(targetFile).Length > MaxFileNameLength)
            {
                throw new InvalidOperationException(
                    $"Too long target file name: \"{targetFile}\", entry.Name: \"{entry.Name}\". " +
                    $"Maximum allowed length: {MaxFileNameLength}");
            }
            
            if (!entry.FullName.EndsWith('/')) // Not a directory
            {
                entry.ExtractToFile(targetFile, true);
            }
        }

        /// <summary>
        /// Creates a unqiue value hash. Used when an original value is too long to be used as a file name.
        /// </summary>
        /// <param name="value">Value to hash.</param>
        /// <returns>Hashed value.</returns>
        internal static string CalculateHash(string value)
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
                result += value[extIndex..];
            }

            return result;
        }
    }
}
