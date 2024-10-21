using SIPackages;
using SIPackages.Core;

namespace SIQuester.ViewModel.Helpers;

/// <summary>
/// Provides helper functions for package items.
/// </summary>
internal static class PackageItemsHelper
{
    /// <summary>
    /// Creates a question with provided price.
    /// </summary>
    /// <param name="price">Question price.</param>
    internal static Question CreateQuestion(int price)
    {
        var question = new Question
        {
            Price = price,
        };

        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new() { Type = ContentTypes.Text, Value = "" },
            }
        };

        question.Right.Add("");

        return question;
    }
}
