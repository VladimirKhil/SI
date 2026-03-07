namespace SI.GameServer.Client;

/// <summary>
/// Defines SIHost client options.
/// </summary>
public sealed class SIHostClientOptions
{
    /// <summary>
    /// SIHost service Uri.
    /// </summary>
    public Uri? ServiceUri { get; set; }

    /// <summary>
    /// Proxy Uri to access the host if direct access is not possible.
    /// Used only for initial connection establishment.
    /// </summary>
    public Uri? ProxyUri { get; set; }

    /// <summary>
    /// Initial handshake timeout.
    /// </summary>
    public TimeSpan HandshakeTimeout { get; set; } = TimeSpan.FromMinutes(2);
}
