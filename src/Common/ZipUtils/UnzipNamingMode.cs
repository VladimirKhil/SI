namespace ZipUtils;

/// <summary>
/// Defines files naming mode while extracting them from archive.
/// </summary>
public enum UnzipNamingMode
{
    /// <summary>
    /// Keep original file name.
    /// </summary>
    KeepOriginal,

    /// <summary>
    /// Unescape file name from URI format.
    /// </summary>
    Unescape,

    /// <summary>
    /// Hash file name (for safety).
    /// </summary>
    Hash
}
