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
    /// <param name="upgraded">Should the question have upgraded format.</param>
    internal static Question CreateQuestion(int price, bool upgraded)
    {
        var question = new Question { Price = price };

        if (upgraded)
        {
            question.Parameters = new StepParameters
            {
                [QuestionParameterNames.Question] = new StepParameter
                {
                    Type = StepParameterTypes.Content,
                    ContentValue = new List<ContentItem>
                    {
                        new ContentItem { Type = AtomTypes.Text, Value = "" },
                    }
                }
            };
        }
        else
        {
            var atom = new Atom();
            question.Scenario.Add(atom);
        }

        question.Right.Add("");

        return question;
    }
}
