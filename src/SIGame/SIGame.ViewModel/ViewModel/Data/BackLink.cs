using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notions;
using SICore;
using SICore.Models;
using SICore.PlatformSpecific;
using SICore.Results;
using SIGame.ViewModel.Helpers;
using SIGame.ViewModel.Properties;
using SIGame.ViewModel.Settings;
using SIStatisticsService.Contract;
using SIStatisticsService.Contract.Models;

namespace SIGame.ViewModel;

public sealed class BackLink : GameHostBase
{
    public override HostOptions Options { get; } = new();

    internal static BackLink Default { get; set; }

    private readonly UserSettings _userSettings;
    private readonly AppState _appState;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackLink> _logger;

    internal BackLink(
        UserSettings userSettings,
        AppState appState,
        IServiceProvider serviceProvider)
    {
        _userSettings = userSettings;
        _appState = appState;
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<BackLink>>();
    }

    public override void OnFlash(bool flash = true) => PlatformSpecific.PlatformManager.Instance.Activate();

    public override void PlaySound(string sound = null, double speed = 1.0) => PlatformSpecific.PlatformManager.Instance.PlaySound(sound, speed);

    public override bool MakeLogs => _userSettings.GameSettings.AppSettings.MakeLogs;

    public override string GameButtonKey => PlatformSpecific.PlatformManager.Instance.GetKeyName(_userSettings.GameSettings.AppSettings.GameButtonKey2);

    public override int MaximumTableTextLength => _userSettings.GameSettings.AppSettings.ThemeSettings.MaximumTableTextLength;

    public override int MaximumReplicTextLength => _userSettings.GameSettings.AppSettings.ThemeSettings.MaximumReplicTextLength;

    public override void OnError(Exception exc) => PlatformSpecific.PlatformManager.Instance.ShowMessage(exc.ToString(), PlatformSpecific.MessageType.Error, true);

    public override void Log(string message) => _logger.LogInformation("Game info: {message}", message);

    public override void LogWarning(string message) => _logger.LogWarning("Game warning: {warning}", message);

    public override void SendError(Exception exc, bool isWarning = false) => PlatformSpecific.PlatformManager.Instance.SendErrorReport(exc);

    /// <summary>
    /// Sends game results info to server.
    /// </summary>
    public override async void SaveReport(GameResult gameResult, CancellationToken cancellationToken = default)
    {
        if (gameResult.Reviews.Count == 0)
        {
            return;
        }

        GameReport? gameReport = null;

        try
        {
            gameReport = gameResult.ToGameReport();
            var statisticsService = _serviceProvider.GetRequiredService<ISIStatisticsServiceClient>();
            await statisticsService.SendGameReportAsync(gameReport, cancellationToken);
        }
        catch (Exception)
        {
            PlatformSpecific.PlatformManager.Instance.ShowMessage(Resources.Error_SendingReport, PlatformSpecific.MessageType.Warning, true);
            
            if (gameReport != null)
            {
                _appState.TryAddDelayedReport(gameReport);
            }
        }
    }

    public override void OnPictureError(string remoteUri) =>
        PlatformSpecific.PlatformManager.Instance.ShowMessage(
            string.Format(Resources.Error_UploadingAvatar + ": {0}", remoteUri),
            PlatformSpecific.MessageType.Warning,
            true);

    public override string PhotoUri => Global.PhotoUri;

    public override string GetPhotoUri(string name) => Path.Combine(Global.PhotoUri, name.Translit() + ".jpg");

    public override bool SendReport => _userSettings.SendReport;

    public override void SaveBestPlayers(IEnumerable<PlayerAccount> players)
    {
        var bestPlayers = CommonSettings.Default.BestPlayers;

        foreach (var player in players)
        {
            var d = bestPlayers.Count - 1;

            while (d > -1 && player.Sum >= bestPlayers[d].Result)
            {
                d--;
            }

            bestPlayers.Insert(d + 1, new BestPlayer { Name = player.Name, Result = player.Sum });

            if (bestPlayers.Count > BestPlayer.Total)
            {
                bestPlayers.RemoveAt(bestPlayers.Count - 1);
            }
        }
    }

    public override void OnGameFinished(string packageId)
    {
        PlatformSpecific.PlatformManager.Instance.ExecuteOnUIThread(() =>
        {
            if (!string.IsNullOrEmpty(packageId) && !_userSettings.PackageHistory.Contains(packageId))
            {
                _userSettings.PackageHistory.Add(packageId);
            }
        });
    }

    public override bool ShowBorderOnFalseStart => _userSettings.GameSettings.AppSettings.ShowBorderOnFalseStart;

    public override bool LoadExternalMedia => _userSettings.LoadExternalMedia;

    public override int MaxImageSizeKb => int.MaxValue;

    public override int MaxAudioSizeKb => int.MaxValue;

    public override int MaxVideoSizeKb => int.MaxValue;

    public override bool AreCustomAvatarsSupported => true;
}
