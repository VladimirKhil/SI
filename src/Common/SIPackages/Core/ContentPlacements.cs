namespace SIPackages.Core;

/// <summary>
/// Defines well-known places where content could be presented.
/// </summary>
public static class ContentPlacements
{
    /// <summary>
    /// Main screen (game table). Supports text, image, video and HTML content types.
    /// </summary>
    public const string Screen = "screen";

    /// <summary>
    /// Showman replic. Supports only text (ex-oral) content type.
    /// </summary>
    public const string Replic = "replic";

    /// <summary>
    /// Invisible background content. Supports only audio content type.
    /// </summary>
    public const string Background = "background";
}
