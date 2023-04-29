namespace SI.GameServer.Contract;

/// <summary>
/// Defines well-known package type.
/// </summary>
public enum PackageType
{
    /// <summary>
    /// Prepared content. It's files are publicly accessed but root content file is protected with secret.
    /// </summary>
    Content,

    /// <summary>
    /// Library file. It is accessed a as single file with no password and should be extracted before use.
    /// </summary>
    LibraryItem
}
