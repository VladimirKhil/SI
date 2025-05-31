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

    void OnError(Exception exc);

    bool ShowBorderOnFalseStart { get; }

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

    void SendError(Exception exc, bool isWarning = false);

    void SaveReport(Results.GameResult result, CancellationToken cancellationToken = default);

    void OnGameFinished(string packageId);

    /// <summary>
    /// Process advertisement request.
    /// </summary>
    string? GetAd(string localization, out int adId);

    void LogWarning(string message);
}
