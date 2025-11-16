using Microsoft.Extensions.Logging;
using SICore.Contracts;
using SICore.Models;
using SICore.Results;
using SIGame.ViewModel.Helpers;
using SIGame.ViewModel.Properties;
using SIGame.ViewModel.Settings;
using SIStatisticsService.Contract;
using SIStatisticsService.Contract.Models;

namespace SIGame.ViewModel;

internal sealed class GameHost : IGameHost
{
    public HostOptions Options { get; } = new();

    private readonly AppState _appState;

    private readonly ISIStatisticsServiceClient _siStatisticsServiceClient;
    private readonly ILogger<GameHost> _logger;

    public GameHost(
        AppState appState,
        ISIStatisticsServiceClient siStatisticsServiceClient,
        ILogger<GameHost> logger)
    {
        _appState = appState;
        _siStatisticsServiceClient = siStatisticsServiceClient;
        _logger = logger;
    }

    public void SendError(Exception exc, bool isWarning = false) => PlatformSpecific.PlatformManager.Instance.SendErrorReport(exc);

    public void LogWarning(string message) => _logger.LogWarning("Game warning: {warning}", message);

    /// <summary>
    /// Sends game results info to server.
    /// </summary>
    public async void SaveReport(GameResult gameResult)
    {
        GameReport? gameReport = null;

        try
        {
            gameReport = gameResult.ToGameReport();
            await _siStatisticsServiceClient.SendGameReportAsync(gameReport);
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

    public string? GetAd(string localization, out int adId)
    {
        adId = -1;
        return null;
    }

    public int MaxImageSizeKb => int.MaxValue;

    public int MaxAudioSizeKb => int.MaxValue;

    public int MaxVideoSizeKb => int.MaxValue;

    public bool AreCustomAvatarsSupported => true;
}
