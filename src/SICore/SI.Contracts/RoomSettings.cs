using SI.Contracts.Models;

namespace SI.Contracts;

/// <summary>
/// Defines game room settings.
/// </summary>
public sealed class RoomSettings
{
    /// <summary>
    /// Game host name.
    /// </summary>
    public required string HostName { get; set; }

    /// <summary>
    /// Showman account.
    /// </summary>
    public Account? Showman { get; set; }

    /// <summary>
    /// Player accounts.
    /// </summary>
    public Account[] Players { get; set; } = [];

    /// <summary>
    /// Viewer accounts.
    /// </summary>
    public Account[] Viewers { get; set; } = [];

    /// <summary>
    /// Room name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Room password.
    /// </summary>
    public string Password { get; set; } = "";

    /// <summary>
    /// Voice chat URI.
    /// </summary>
    public string VoiceChatUri { get; set; } = "";

    /// <summary>
    /// Defines a private game.
    /// </summary>
    /// <remarks>
    /// Private games are invisible in lobby game lists. They also do not allow to join for anybody except the host.
    /// In private games game name and password do not matter.
    /// </remarks>
    public bool IsPrivate { get; set; }

    /// <summary>
    /// Marks an autogame.
    /// </summary>
    /// <remarks>
    /// Auto games starts automatically by timer or when they are full.
    /// Human players join these games automatically when they decide to play with random opponents.
    /// These games do not have a host.
    /// </remarks>
    public bool IsAutomatic { get; set; }
}
