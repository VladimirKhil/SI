namespace SIStorageService.Client.Models;

/// <summary>
/// Defines an object with name.
/// </summary>
public sealed class NamedObject
{
    /// <summary>
    /// Object unique identifier.
    /// </summary>
    public int ID { get; set; }

    /// <summary>
    /// Object name.
    /// </summary>
    public string? Name { get; set; }
}
