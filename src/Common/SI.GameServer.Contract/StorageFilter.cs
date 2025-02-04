namespace SI.GameServer.Contract;

/// <summary>
/// Defines a filter for a custom storage.
/// </summary>
public sealed class StorageFilter
{
    /// <summary>
    /// Filtered packages.
    /// </summary>
    public Dictionary<int, int> Packages { get; set; } = new();

    /// <summary>
    /// Filtered tags.
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();
}
