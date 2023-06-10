using SICore.Results;
using SIStatisticsService.Contract.Models;

namespace SIGame.ViewModel.Helpers;

internal static class GameResultExtensions
{
    /// <summary>
    /// Converts game report to SIStatistics service format.
    /// </summary>
    /// <param name="gameResult">Game report to convert.</param>
    public static GameReport ToGameReport(this GameResult gameResult)
    {
        var reports = new List<SIStatisticsService.Contract.Models.QuestionReport>();

        foreach (var item in gameResult.AcceptedAnswers)
        {
            reports.Add(item.ConvertToReport(QuestionReportType.Accepted));
        }

        foreach (var item in gameResult.ApellatedAnswers)
        {
            reports.Add(item.ConvertToReport(QuestionReportType.Apellated));
        }

        foreach (var item in gameResult.RejectedAnswers)
        {
            reports.Add(item.ConvertToReport(QuestionReportType.Rejected));
        }

        foreach (var item in gameResult.ComplainedQuestions)
        {
            reports.Add(item.ConvertToReport(QuestionReportType.Complained));
        }

        return new GameReport
        {
            Info = new GameResultInfo
            {
                FinishTime = DateTimeOffset.UtcNow,
                Duration = gameResult.Duration,
                Name = gameResult.Name,
                Package = new PackageInfo
                {
                    Authors = gameResult.PackageAuthors,
                    Hash = gameResult.PackageHash,
                    Name = gameResult.PackageName
                },
                Platform = GamePlatforms.Local,
                Results = gameResult.Results,
                Reviews = gameResult.Reviews
            },
            QuestionReports = reports.ToArray()
        };
    }

    private static SIStatisticsService.Contract.Models.QuestionReport ConvertToReport(
        this SICore.Results.QuestionReport item,
        QuestionReportType reportType) =>
        new()
        {
            QuestionText = item.QuestionText,
            ThemeName = item.ThemeName,
            ReportText = item.ReportText,
            ReportType = reportType
        };
}
