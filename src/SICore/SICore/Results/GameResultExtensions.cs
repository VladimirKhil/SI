using SICore.BusinessLogic;
using System.Text;
using R = SICore.Properties.Resources;

namespace SICore.Results;

/// <summary>
/// Provides extension method for converting game result to string.
/// </summary>
public static class GameResultExtensions
{
    private const string DescriptionFormat = "{0}: {1}";

    /// <summary>
    /// Converts game result to string.
    /// </summary>
    /// <param name="gameResult">Game result.</param>
    /// <param name="localizer">Localizer.</param>
    public static string ToString(this GameResult gameResult, ILocalizer localizer)
    {
        var result = new StringBuilder();
        result.AppendFormat(DescriptionFormat, localizer[nameof(R.PackageName)], gameResult.PackageName).AppendLine().AppendLine();
        result.Append(localizer[nameof(R.GameResults)]).AppendLine(":");

        foreach (var item in gameResult.Results)
        {
            result.AppendFormat(DescriptionFormat, item.Key, item.Value).AppendLine();
        }

        result.AppendLine().Append(localizer[nameof(R.ApellatedAnswers)]).AppendLine(":");
        PrintCollection(gameResult.ApellatedAnswers, result, localizer[nameof(R.Apellation)], localizer);

        result.AppendLine().Append(localizer[nameof(R.WrongAnswers)]).AppendLine(":");
        PrintCollection(gameResult.RejectedAnswers, result, localizer[nameof(R.WrongAns)], localizer);

        return result.ToString().Replace(Environment.NewLine, "\r");
    }

    private static void PrintCollection(
        IEnumerable<QuestionReport> collection,
        StringBuilder result,
        string answerTitle,
        ILocalizer localizer)
    {
        result.Append(answerTitle).AppendLine().AppendLine();

        foreach (var answerInfo in collection)
        {
            result
                .AppendFormat(DescriptionFormat, localizer[nameof(R.Question)], answerInfo.QuestionText)
                .AppendLine()
                .AppendFormat(DescriptionFormat, localizer[nameof(R.Answer)], answerInfo.ReportText)
                .AppendLine()
                .AppendLine();
        }
    }
}
