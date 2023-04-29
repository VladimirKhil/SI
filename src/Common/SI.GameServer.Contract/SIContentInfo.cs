namespace SI.GameServer.Contract;

/// <summary>
/// Contains information about SI Content service.
/// </summary>
public sealed class SIContentInfo
{
    /// <summary>
    /// Content service uri.
    /// </summary>
    public Uri? ServiceUri { get; set; }

    /// <summary>
    /// Content region code.
    /// </summary>
    public string Region { get; set; } = "";
}
