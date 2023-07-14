using SIUI.ViewModel;
using System.Text.RegularExpressions;

namespace SICore.PlatformSpecific;

public abstract class BackLinkCore : IGameManager
{
    public abstract void OnFlash(bool flash = true);

    public abstract void OnError(Exception exc);

    public abstract void PlaySound(string? sound = null, double speed = 1.0);

    public abstract bool MakeLogs { get; }

    public abstract string LogsFolder { get; }

    public abstract bool TranslateGameToChat { get; }

    public abstract string GameButtonKey { get; }

    public abstract bool SendReport { get; }

    public abstract string PhotoUri { get; }

    public abstract int MaximumTableTextLength { get; }

    public abstract int MaximumReplicTextLength { get; }

    public abstract string GetPhotoUri(string name);

    public abstract void SendError(Exception exc, bool isWarning = false);

    public abstract void SaveReport(Results.GameResult result, CancellationToken cancellationToken = default);

    public abstract void OnPictureError(string remoteUri);

    public abstract void SaveBestPlayers(IEnumerable<PlayerAccount> players);

    public abstract SettingsViewModel GetSettings();

    public Stream CreateLog(string userName, out string logUri)
    {
        var name = Regex.Replace(userName, @"[^\d\w]", "");
        
        var userFolder = Path.Combine(LogsFolder, name);
        Directory.CreateDirectory(userFolder);

        var now = DateTimeOffset.Now;
        var protoFileName = $"{now.Year}.{now.Month}.{now.Day}_{now.Hour}.{now.Minute}.{now.Second}_{now.Offset.TotalMinutes}_log.html";
        logUri = Path.Combine(userFolder, protoFileName);

        return File.Create(logUri);
    }

    public virtual string? GetAd(string localization, out int adId)
    {
        adId = -1;
        return null;
    }

    public abstract void OnGameFinished(string packageId);

    public abstract void LogWarning(string message);

    public abstract bool AreAnswersShown { get; set; }

    public abstract bool ShowBorderOnFalseStart { get; }

    public abstract bool LoadExternalMedia { get; }

    public abstract int MaxImageSizeKb { get; }

    public abstract int MaxAudioSizeKb { get; }

    public abstract int MaxVideoSizeKb { get; }

    public abstract bool AreCustomAvatarsSupported { get; }
}
