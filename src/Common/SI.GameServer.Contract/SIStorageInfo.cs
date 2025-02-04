namespace SI.GameServer.Contract;

/// <summary>
/// Contains information about SI Storage service.
/// </summary>
public sealed class SIStorageInfo
{
    /// <summary>
    /// Storage service identifier.
    /// </summary>
    public string Id { get; set; } = "";

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

    /// <summary>
    /// Maximum package search page size.
    /// </summary>
    public int MaximumPageSize { get; set; } = 20;

    /// <summary>
    /// Public storage uri.
    /// </summary>
    public Uri? Uri { get; set; }

    /// <summary>
    /// Package properties supported by storage.
    /// </summary>
    public string[] PackageProperties { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Facets supported by storage.
    /// </summary>
    public string[] Facets { get; set; } = Array.Empty<string>();
}
