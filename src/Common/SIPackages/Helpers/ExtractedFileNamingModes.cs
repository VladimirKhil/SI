namespace SIPackages.Helpers;

/// <summary>
/// Defines files naming modes while extracting.
/// </summary>
[Obsolete("Use ZipUtils.ZipExtractor")]
public enum ExtractedFileNamingModes
{
    /// <summary>
    /// Keep original files names.
    /// </summary>
    KeepOriginal,

    /// <summary>
    /// Unescape files names.
    /// </summary>
    Unescape,

    /// <summary>
    /// Hash files names.
    /// </summary>
    Hash
}
