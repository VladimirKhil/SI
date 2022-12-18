namespace SIQuester.ViewModel.Model;

/// <summary>
/// Provides storage pending changes.
/// </summary>
public readonly struct StorageChanges
{
    /// <summary>
    /// Added items.
    /// </summary>
    public string[] Added { get; init; }

    /// <summary>
    /// Removed items.
    /// </summary>
    public string[] Removed { get; init; }

    /// <summary>
    /// Renamed items.
    /// </summary>
    public Dictionary<string, string> Renamed { get; init; }
}
