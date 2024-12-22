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

    void OnFlash(bool flash = true);

    void OnError(Exception exc);

    void PlaySound(string? sound = null, double speed = 1.0);

    bool MakeLogs { get; }

    string LogsFolder { get; }

    Stream CreateLog(string userName, out string logUri);

    bool AttachContentToTable { get; }

    bool TranslateGameToChat { get; }

    string GameButtonKey { get; }

    bool SendReport { get; }

    string PhotoUri { get; }

    bool ShowBorderOnFalseStart { get; }

    bool LoadExternalMedia { get; }

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

    int MaximumTableTextLength { get; }

    int MaximumReplicTextLength { get; }

    bool AreCustomAvatarsSupported { get; }

    string GetPhotoUri(string name);

    void SendError(Exception exc, bool isWarning = false);

    void SaveReport(Results.GameResult result, CancellationToken cancellationToken = default);

    void OnPictureError(string remoteUri);

    void SaveBestPlayers(IEnumerable<PlayerAccount> players);

    void OnGameFinished(string packageId);

    /// <summary>
    /// Получить рекламное сообщение
    /// </summary>
    string? GetAd(string localization, out int adId);

    /// <summary>
    /// Logs a message.
    /// </summary>
    /// <param name="message">Message to log.</param>
    void Log(string message);

    void LogWarning(string message);
}
