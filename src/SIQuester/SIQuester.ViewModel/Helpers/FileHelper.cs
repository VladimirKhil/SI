﻿namespace SIQuester.ViewModel.Helpers;

internal static class FileHelper
{
    /// <summary>
    /// Forms unique non-existing name for provided path.
    /// </summary>
    /// <param name="filePath">Base file path. If file does not exists, this path will be returned.</param>
    /// <exception cref="InvalidOperationException">Cannot generate unique name for path.</exception>
    internal static string GenerateUniqueFilePath(string filePath) => GenerateUniqueFileName(filePath, File.Exists);

    /// <summary>
    /// Forms unique name for file.
    /// </summary>
    /// <param name="fileName">Original file name.</param>
    /// <param name="duplicateCondition">Duplicates search condition.</param>
    /// <exception cref="InvalidOperationException">Cannot generate unique name for file.</exception>
    internal static string GenerateUniqueFileName(string fileName, Predicate<string> duplicateCondition)
    {
        if (!duplicateCondition(fileName))
        {
            return fileName;
        }

        var index = 0;
        var directoryName = Path.GetDirectoryName(fileName);
        var extension = Path.GetExtension(fileName);
        var baseName = Path.GetFileNameWithoutExtension(fileName);

        do
        {
            var newName = Path.ChangeExtension($"{baseName}_{index++}.", extension);

            if (directoryName != null && directoryName.Length > 0)
            {
                newName = Path.Combine(directoryName, newName);
            }

            if (!duplicateCondition(newName))
            {
                return newName;
            }
        } while (index < 10_000);

        throw new InvalidOperationException($"Cannot generate unique file name for path {fileName}");
    }
}
