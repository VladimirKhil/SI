using SIStatisticsService.Contract.Models;

namespace SIGame.ViewModel.Settings;

/// <summary>
/// Defines global application state.
/// </summary>
public sealed class AppState
{
    /// <summary>
    /// Maximum size of delayed reports collection.
    /// </summary>
    private const int DelayedReportsCapacity = 10;

    /// <summary>
    /// Delayed game reports.
    /// </summary>
    public List<GameReport> DelayedReports { get; set; } = new();

    internal bool TryAddDelayedReport(GameReport gameReport)
    {
        if (DelayedReports.Count >= DelayedReportsCapacity)
        {
            return false;
        }

        DelayedReports.Add(gameReport);
        return true;
    }
}
