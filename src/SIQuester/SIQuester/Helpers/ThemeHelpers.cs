using SIPackages;
using SIQuester.Model;
using SIQuester.ViewModel;

namespace SIQuester.Helpers;

internal static class ThemeHelpers
{
    internal static int[] CapturePrices(this ThemeViewModel themeViewModel) => themeViewModel.Questions.Select(q => q.Model.Price).ToArray();

    internal static void ResetPrices(this ThemeViewModel themeViewModel, int[] prices)
    {
        var prevoiusIndex = 0;
        var questions = themeViewModel.Questions;

        for (var i = 0; i < questions.Count; i++)
        {
            var question = questions[i].Model;

            if (question.Price == Question.InvalidPrice) // Price is not recounted
            {
                continue;
            }

            if (prevoiusIndex < prices.Length)
            {
                question.Price = prices[prevoiusIndex++];
            }
            else if (i == 0)
            {
                question.Price = AppSettings.Default.QuestionBase;
            }
            else if (i == 1)
            {
                question.Price = questions[0].Model.Price * 2;
            }
            else
            {
                var delta = questions[i - 1].Model.Price - questions[i - 2].Model.Price;
                question.Price = delta > 0 ? questions[i - 1].Model.Price + delta : questions[i - 1].Model.Price;
            }
        }
    }
}
