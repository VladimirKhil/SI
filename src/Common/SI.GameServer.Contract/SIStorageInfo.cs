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

    /// <summary>
    /// Are random packages supported.
    /// </summary>
    public bool RandomPackagesSupported { get; set; } = true;

    /// <summary>
    /// Are integer identifiers supported.
    /// </summary>
    public bool IdentifiersSupported { get; set; } = true;
}
