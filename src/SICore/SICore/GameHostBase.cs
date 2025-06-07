using SICore.Contracts;
using SICore.Models;

namespace SICore.PlatformSpecific;

/// <summary>
/// Defines base class for game hosts.
/// </summary>
public abstract class GameHostBase : IGameHost
{
    public abstract HostOptions Options { get; }

    public abstract void SendError(Exception exc, bool isWarning = false);

    public abstract void SaveReport(Results.GameResult result, CancellationToken cancellationToken = default);

    public virtual string? GetAd(string localization, out int adId)
    {
        adId = -1;
        return null;
    }

    public abstract void LogWarning(string message);

    public abstract int MaxImageSizeKb { get; }

    public abstract int MaxAudioSizeKb { get; }

    public abstract int MaxVideoSizeKb { get; }

    public abstract bool AreCustomAvatarsSupported { get; }
}
