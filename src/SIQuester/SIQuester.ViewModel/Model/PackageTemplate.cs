using SIQuester.Model;

namespace SIQuester.ViewModel.Model;

/// <summary>
/// Defines a package template model.
/// </summary>
public sealed class PackageTemplate
{
    /// <summary>
    /// Template name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Template decription.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Template type.
    /// </summary>
    public PackageType Type { get; set; }

    /// <summary>
    /// Template file path.
    /// </summary>
    public string? FileName { get; set; }
}
