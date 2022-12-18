namespace SIQuester.ViewModel.Model;

/// <summary>
/// Provides document collection changes.
/// </summary>
public readonly struct MediaChanges
{
    /// <summary>
    /// Images changes.
    /// </summary>
    public StorageChanges ImagesChanges { get; init; }

    /// <summary>
    /// Audio changes.
    /// </summary>
    public StorageChanges AudioChanges { get; init; }

    /// <summary>
    /// Video changes.
    /// </summary>
    public StorageChanges VideoChanges { get; init; }
}
