using SIPackages.Core;

namespace SIPackages.Helpers;

/// <summary>
/// Provides extension methods for the <see cref="Theme"/> class.
/// </summary>
public static class ThemeExtensions
{
    /// <summary>
    /// Creates a new question in the theme.
    /// </summary>
    /// <param name="theme">Theme.</param>
    /// <param name="price">Question price.</param>
    /// <param name="isFinal">Does the question belong to the final round.</param>
    /// <param name="text">Question text.</param>
    public static Question CreateQuestion(this Theme theme, int price = -1, bool isFinal = false, string text = "")
    {
        int qPrice = DetectQuestionPrice(theme, price, isFinal);

        var quest = new Question
        {
            Price = qPrice
        };

        quest.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new() { Type = ContentTypes.Text, Value = text },
            }
        };

        quest.Right.Add("");
        theme.Questions.Add(quest);

        return quest;
    }

    private static int DetectQuestionPrice(Theme theme, int price, bool isFinal)
    {
        if (price != -1)
        {
            return price;
        }

        var validQuestions = theme.Questions.Where(q => q.Price != Question.InvalidPrice).ToList();

        var questionCount = validQuestions.Count;

        if (questionCount > 1)
        {
            var stepValue = validQuestions[1].Price - validQuestions[0].Price;
            return Math.Max(0, validQuestions[questionCount - 1].Price + stepValue);
        }

        if (questionCount > 0)
        {
            return validQuestions[0].Price * 2;
        }

        if (isFinal)
        {
            return 0;
        }

        return 100;
    }
}
