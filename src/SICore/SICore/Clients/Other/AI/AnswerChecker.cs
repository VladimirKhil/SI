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
