namespace SIStorageService.Client.Models;

/// <summary>
/// Contains package link.
/// </summary>
public sealed class PackageLink
{
    /// <summary>
    /// Package name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Package uri.
    /// </summary>
    public Uri? Uri { get; set; }
}
