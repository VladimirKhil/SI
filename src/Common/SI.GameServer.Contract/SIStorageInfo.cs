namespace SI.GameServer.Contract;

/// <summary>
/// Contains information about SI Storage service.
/// </summary>
public sealed class SIStorageInfo
{
    /// <summary>
    /// Storage service uri.
    /// </summary>
    public Uri? ServiceUri { get; set; }

    /// <summary>
    /// Storage name.
    /// </summary>
    public string Name { get; set; } = "";
}
