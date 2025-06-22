using Microsoft.Extensions.DependencyInjection;
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

public sealed class BackLink : IGameHost
{
    public HostOptions Options { get; } = new();

    internal static BackLink Default { get; set; }

    private readonly AppState _appState;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackLink> _logger;

    internal BackLink(
        AppState appState,
        IServiceProvider serviceProvider)
    {
        _appState = appState;
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<BackLink>>();
    }

    public void SendError(Exception exc, bool isWarning = false) => PlatformSpecific.PlatformManager.Instance.SendErrorReport(exc);

    public void LogWarning(string message) => _logger.LogWarning("Game warning: {warning}", message);

    /// <summary>
    /// Sends game results info to server.
    /// </summary>
    public async void SaveReport(GameResult gameResult, CancellationToken cancellationToken = default)
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
