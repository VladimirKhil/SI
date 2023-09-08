namespace SIQuester.ViewModel.Helpers;

internal static class FileHelper
{
    /// <summary>
    /// Forms unique non-existing file name for provided path.
    /// </summary>
    /// <param name="filePath">Base file path. If file does not exists, this path will be returned.</param>
    /// <exception cref="InvalidOperationException">Cannot generate unique file name for path.</exception>
    internal static string GenerateUniqueFileName(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return filePath;
        }

        var index = 0;
        var extension = Path.GetExtension(filePath);
        var baseName = Path.GetFileNameWithoutExtension(filePath);

        do
        {
            var newName = Path.ChangeExtension($"{baseName}_{index++}.", extension);

            if (!File.Exists(newName))
            {
                return newName;
            }
        } while (index < 10_000);

        throw new InvalidOperationException($"Cannot generate unique file name for path {filePath}");
    }
}
