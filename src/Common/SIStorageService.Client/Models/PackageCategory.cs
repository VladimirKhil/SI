namespace SIStorageService.Client.Models;

/// <summary>
/// Defines a package category.
/// </summary>
public sealed class PackageCategory
{
    /// <summary>
    /// Unique package category identifier.
    /// </summary>
    public int ID { get; set; }

    /// <summary>
    /// Category name.
    /// </summary>
    public string? Name { get; set; }
}
