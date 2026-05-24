using Notions;
using System.Text;

namespace SICore;

internal static class AnswerChecker
{
    private const double MinimumCharacterDistance = 0.81;
    private const double MinimumNumberDistance = 0.99;

    /// <summary>
    /// Validates the answer by comparing it to the right answer, considering both numeric and non-numeric parts.
    /// </summary>
    /// <param name="answer">Given answer.</param>
    /// <param name="rightAnswers">Right answers.</param>
    /// <returns>True if the given answer is considered correct, otherwise false.</returns>
    internal static bool IsAnswerRight(string answer, IEnumerable<string> rightAnswers)
    {
        // Extract digits and non-digits parts from both answers
        var (answerDigits, answerNonDigits) = SeparateDigitsAndNonDigits(answer);

        foreach (var rightAnswer in rightAnswers)
        {
            var (rightAnswerDigits, rightAnswerNonDigits) = SeparateDigitsAndNonDigits(rightAnswer);

            // Calculate distances for both parts
            var characterDistance = Notion.AnswerValidatingCommon2(answerNonDigits, rightAnswerNonDigits);
            var numberDistance = Notion.AnswerValidatingCommon2(answerDigits, rightAnswerDigits);

            // Check if both distances meet the minimum requirements
            var isRight = characterDistance > MinimumCharacterDistance && numberDistance > MinimumNumberDistance;

            if (isRight)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsNumberAnswerRight(string playerAnswer, string rightAnswer, double deviation) =>
        int.TryParse(rightAnswer, out var rightNumber) &&
        int.TryParse(playerAnswer, out var playerNumber) &&
        Math.Abs(playerNumber - rightNumber) <= deviation;

    internal static bool IsPointAnswerRight(string playerAnswer, string rightAnswer, double deviation)
    {
        var rightPointParsed = ParsePoint(rightAnswer);

        if (!rightPointParsed.HasValue)
        {
            return false;
        }

        var (x, y, aspectRatio) = rightPointParsed.Value;
        var playerPointParsed = ParsePlayerPoint(playerAnswer, aspectRatio);

        return playerPointParsed.HasValue && CalculatePointDistance((x, y), playerPointParsed.Value) <= deviation;
    }

    /// <summary>
    /// Parses a point answer in "x,y" or "x,y,aspectRatio" format.
    /// </summary>
    /// <param name="pointString">String in "x,y" or "x,y,aspectRatio" format where x and y are doubles.</param>
    /// <returns>Parsed point as tuple (x, y, aspectRatio), or null if parsing fails.</returns>
    private static (double X, double Y, double aspectRatio)? ParsePoint(string pointString, double defaultAspectRation = 1.0)
    {
        if (string.IsNullOrWhiteSpace(pointString))
        {
            return null;
        }

        var parts = pointString.Split(',');

        if (parts.Length < 2 || parts.Length > 3)
        {
            return null;
        }

        var aspectRatio = parts.Length > 2
            ? double.TryParse(parts[2].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var ar)
                ? ar
                : defaultAspectRation
            : defaultAspectRation;

        if (!double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x) ||
            !double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y))
        {
            return null;
        }

        if (aspectRatio > 0.0 && Math.Abs(aspectRatio - 1.0) > double.Epsilon)
        {
            x *= aspectRatio;
        }

        return (x, y, aspectRatio);
    }

    /// <summary>
    /// Parses player point answer in "x,y" format and normalizes it with a known aspect ratio.
    /// </summary>
    private static (double X, double Y)? ParsePlayerPoint(string pointString, double aspectRatio)
    {
        if (string.IsNullOrWhiteSpace(pointString))
        {
            return null;
        }

        var parts = pointString.Split(',');

        if (parts.Length != 2)
        {
            return null;
        }

        if (!double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x) ||
            !double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y))
        {
            return null;
        }

        if (aspectRatio > 0.0 && Math.Abs(aspectRatio - 1.0) > double.Epsilon)
        {
            x *= aspectRatio;
        }

        return (x, y);
    }

    /// <summary>
    /// Calculates Euclidean distance between two points.
    /// </summary>
    private static double CalculatePointDistance((double X, double Y) point1, (double X, double Y) point2)
    {
        var dx = point1.X - point2.X;
        var dy = point1.Y - point2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Separates the input string into two parts: one containing only digits and the other containing non-digit characters.
    /// </summary>
    /// <param name="s">The input string.</param>
    /// <returns>A tuple where the first item is a string of digits and the second item is a string of non-digit characters.</returns>
    private static (string Digits, string NonDigits) SeparateDigitsAndNonDigits(string s)
    {
        var digits = new StringBuilder();
        var nonDigits = new StringBuilder();
        var length = s.Length;

        for (var i = 0; i < length; i++)
        {
            if (char.IsDigit(s[i]))
            {
                digits.Append(s[i]);
            }
            else
            {
                nonDigits.Append(s[i]);
            }
        }

        return (digits.ToString(), nonDigits.ToString());
    }
}
