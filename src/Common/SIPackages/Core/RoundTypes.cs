namespace SIPackages.Core;

/// <summary>
/// Provides well-known round types.
/// </summary>
public static class RoundTypes
{
    /// <summary>
    /// [Table] Simple round type.
    /// </summary>
    public const string Standart = "standart";

    /// <summary>
    /// Table round type.
    /// </summary>
    /// <remarks>Reserved for <see cref="Standart"/> type replacement.</remarks>
    public const string Table = "table";

    /// <summary>
    /// [Theme list] Final round type.
    /// </summary>
    public const string Final = "final";

    /// <summary>
    /// Theme list round type.
    /// </summary>
    /// <remarks>Reserved for <see cref="Final"/> type replacement.</remarks>
    public const string ThemeList = "themeList";
}
