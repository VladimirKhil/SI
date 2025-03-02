﻿namespace SI.GameServer.Contract;

/// <summary>
/// Provides game server information.
/// </summary>
public sealed class HostInfo
{
    /// <summary>
    /// Server public name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Base Urls that are considered valid for in-game content files.
    /// </summary>
    public string[] ContentPublicBaseUrls { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Contains information about well-known SIContent services.
    /// </summary>
    public SIContentInfo[] ContentInfos { get; set; } = Array.Empty<SIContentInfo>();

    /// <summary>
    /// Contains information about well-known SIStorage services.
    /// </summary>
    public SIStorageInfo[] StorageInfos { get; set; } = Array.Empty<SIStorageInfo>();

    /// <summary>
    /// Contains information about well-known SIHost services.
    /// </summary>
    public Dictionary<string, string> SIHosts { get; set; } = new();

    /// <summary>
    /// Server license text.
    /// </summary>
    public string License { get; set; } = "";

    /// <summary>
    /// Maximum allowed package size in MB.
    /// </summary>
    public int MaxPackageSizeMb { get; set; } = 100;

    /// <summary>
    /// Base uri for game links.
    /// </summary>
    public Uri? BaseGameLinkUri { get; set; }
}
