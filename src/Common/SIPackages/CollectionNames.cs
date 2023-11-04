using SIPackages.Core;

namespace SIPackages;

/// <summary>
/// Defines well-known package collection names.
/// </summary>
public static class CollectionNames
{
    /// <summary>
    /// Texts collection name.
    /// </summary>
    public const string TextsStorageName = "Texts";

    /// <summary>
    /// Images collection name.
    /// </summary>
    public const string ImagesStorageName = "Images";

    /// <summary>
    /// Audio collection name.
    /// </summary>
    public const string AudioStorageName = "Audio";

    /// <summary>
    /// Video collection name.
    /// </summary>
    public const string VideoStorageName = "Video";

    /// <summary>
    /// Html collection name.
    /// </summary>
    public const string HtmlStorageName = "Html";

    /// <summary>
    /// Tries to get collection name by media type.
    /// </summary>
    /// <param name="mediaType">Collection media type.</param>
    /// <returns>Found collection name or null.</returns>
    public static string? TryGetCollectionName(string mediaType) => mediaType switch
    {
        AtomTypes.Image => ImagesStorageName,
        AtomTypes.Audio or AtomTypes.AudioNew => AudioStorageName,
        AtomTypes.Video => VideoStorageName,
        AtomTypes.Html => HtmlStorageName,
        _ => null,
    };

    /// <summary>
    /// Tries to get media type by collection name.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <returns>Found media type or null.</returns>
    public static string? TryGetContentType(string collectionName) => collectionName switch
    {
        ImagesStorageName => ContentTypes.Image,
        AudioStorageName => ContentTypes.Audio,
        VideoStorageName => ContentTypes.Video,
        HtmlStorageName => ContentTypes.Html,
        _ => null,
    };

    /// <summary>
    /// Gets media collection by media type.
    /// </summary>
    /// <param name="mediaType">Collection media type.</param>
    /// <returns>Found collection or null.</returns>
    public static string GetCollectionName(string mediaType) => TryGetCollectionName(mediaType)
        ?? throw new ArgumentException($"Invalid media type {mediaType}", nameof(mediaType));
}
