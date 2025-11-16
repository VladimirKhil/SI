using SICore.Models;

namespace SICore.Contracts;

// TODO: move option properties into Options

/// <summary>
/// Defines a host that runs the game.
/// </summary>
public interface IGameHost
{
    /// <summary>
    /// Gets host options.
    /// </summary>
    HostOptions Options { get; }

    void SendError(Exception exc, bool isWarning = false);

    /// <summary>
    /// Maximum recommended image size.
    /// </summary>
    int MaxImageSizeKb { get; }

    /// <summary>
    /// Maximum recommended audio size.
    /// </summary>
    int MaxAudioSizeKb { get; }

    /// <summary>
    /// Maximum recommended video size.
    /// </summary>
    int MaxVideoSizeKb { get; }

    bool AreCustomAvatarsSupported { get; }

    void SaveReport(Results.GameResult result);

    /// <summary>
    /// Process advertisement request.
    /// </summary>
    string? GetAd(string localization, out int adId);

    void LogWarning(string message);
}
