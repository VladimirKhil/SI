using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace SIPackages
{
    public static class ZipHelper
    {
        internal static int MaxFileNameLength = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 100 : 255 / 2; // / 2 из-за кириллических символов

        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, bool unescape = false)
        {
            Directory.CreateDirectory(destinationDirectoryName);

            using var stream = File.OpenRead(sourceArchiveFileName);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            foreach (var entry in archive.Entries)
            {
                ExtractEntryToDirectory(entry, destinationDirectoryName, unescape);
            }
        }

        public static string PrepareForExtraction(ZipArchiveEntry entry, string destinationDirectoryName, bool unescape)
        {
            var name = unescape ? Uri.UnescapeDataString(entry.Name) : entry.Name;
            if (name.Length > MaxFileNameLength)
                name = CalculateHash(name);

            var index = entry.FullName.IndexOf('/');

            var targetDir = index > -1 ? Path.Combine(destinationDirectoryName, entry.FullName.Substring(0, index)) : destinationDirectoryName;
            Directory.CreateDirectory(targetDir);

            return Path.Combine(targetDir, name);
        }

        public static void ExtractEntryToDirectory(ZipArchiveEntry entry, string destinationDirectoryName, bool unescape = false)
        {
            var targetFile = PrepareForExtraction(entry, destinationDirectoryName, unescape);

            if (Path.GetFileName(targetFile).Length > MaxFileNameLength)
            {
                throw new ArgumentOutOfRangeException(nameof(targetFile), $"Wrong target file: \"{targetFile}\", entry.Name: \"{entry.Name}\"");
            }

            if (!entry.FullName.EndsWith("/"))
            {
                entry.ExtractToFile(targetFile, true);
            }
        }

        internal static string CalculateHash(string value)
        {
            ulong hashedValue = 3074457345618258791ul;
            for (int i = 0; i < value.Length; i++)
            {
                hashedValue += value[i];
                hashedValue *= 3074457345618258799ul;
            }

            var result = hashedValue.ToString("X2");

            var extIndex = value.LastIndexOf('.');
            if (extIndex > -1)
                result += value.Substring(extIndex);

            return result;
        }
    }
}
