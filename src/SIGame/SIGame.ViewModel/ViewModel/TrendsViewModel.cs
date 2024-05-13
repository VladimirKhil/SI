using SIStatisticsService.Contract.Models;

namespace SIGame.ViewModel;

public sealed class TrendsViewModel
{
    public PackageStatistic[] Packages { get; }

    public GamesStatistic Games { get; }

    public GamesResponse LatestGames { get; }

    public TrendsViewModel(PackageStatistic[] packages, GamesStatistic gameStatistcs, GamesResponse latestGames)
    {
        Packages = packages;
        Games = gameStatistcs;
        LatestGames = latestGames;
    }
}
