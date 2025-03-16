namespace SI.GameServer.Client;

/// <summary>
/// Provides SIGame server client options.
/// </summary>
public sealed class GameServerClientOptions
{
    public const string ConfigurationSectionName = "GameServerClient";

    /// <summary>
    /// SIGame server Uri.
    /// </summary>
    public string? ServiceUri { get; set; }

    /// <summary>
    /// SIGame service discovery Uri.
    /// </summary>
    public Uri? ServiceDiscoveryUri { get; set; } = new Uri("https://vladimirkhil.com/api/si/servers");

    /// <summary>
    /// Requests localization culture.
    /// </summary>
    public string? Culture { get; set; }

    /// <summary>
    /// Client timeout.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(6); // Large value for uploading packages
}
