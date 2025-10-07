namespace SIPackages.Models;

/// <summary>
/// Contains package quality settings.
/// </summary>
public static class Quality
{
    /// <summary>
    /// Defines the maximum file size for each collection in megabytes.
    /// </summary>
    public static readonly Dictionary<string, int> FileSizeMb = new()
    {
        [CollectionNames.ImagesStorageName] = 1,
        [CollectionNames.AudioStorageName] = 5,
        [CollectionNames.VideoStorageName] = 10,
        [CollectionNames.HtmlStorageName] = 1,
    };

    /// <summary>
    /// Defines file extensions for each collection.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string[]> FileExtensions = new Dictionary<string, string[]>()
    {
        [CollectionNames.ImagesStorageName] = new[] { ".jpg", ".jpe", ".jpeg", ".png", ".gif", ".webp", ".avif" },
        [CollectionNames.AudioStorageName] = new[] { ".mp3", ".opus" },
        [CollectionNames.VideoStorageName] = new[] { ".mp4" },
        [CollectionNames.HtmlStorageName] = new[] { ".html" },
    };
}
