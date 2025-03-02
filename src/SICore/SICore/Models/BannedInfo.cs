namespace SICore.Models;

/// <summary>
/// Defaines a banned user info.
/// </summary>
/// <param name="Ip">User IP.</param>
/// <param name="UserName">User name.</param>
public record BannedInfo(string Ip, string UserName);
