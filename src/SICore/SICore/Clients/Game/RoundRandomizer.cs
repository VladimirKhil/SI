using SIPackages.Core;
using SIPackages;

namespace SICore.Clients.Game;

/// <summary>
/// Randomizes round question types.
/// </summary>
internal static class RoundRandomizer
{
    /// <summary>
    /// Generates random special questions in round.
    /// </summary>
    /// <param name="round">Round to regenerate.</param>
    /// <remarks>This method updates its argument.</remarks>
    internal static void RandomizeSpecials(Round round)
    {
        var unusedIndicies = new List<int>();
        var maxQuestionsInTheme = round.Themes.Max(theme => theme.Questions.Count);
        var secretCount = 1 + Random.Shared.Next(3);
        var stakeCount = 1 + Random.Shared.Next(3);
        var noRiskCount = Random.Shared.Next(2);

        for (var themeIndex = 0; themeIndex < round.Themes.Count; themeIndex++)
        {
            var theme = round.Themes[themeIndex];

            for (var questionIndex = 0; questionIndex < theme.Questions.Count; questionIndex++)
            {
                var question = theme.Questions[questionIndex];

                if (question.TypeName != QuestionTypes.Secret
                    && question.TypeName != QuestionTypes.SecretPublicPrice
                    && question.TypeName != QuestionTypes.SecretNoQuestion)
                {
                    unusedIndicies.Add(themeIndex * maxQuestionsInTheme + questionIndex);
                    question.TypeName = QuestionTypes.Simple;
                }
                else
                {
                    secretCount--;
                }
            }
        }

        while (unusedIndicies.Count > 0 && noRiskCount > 0)
        {
            var num = Random.Shared.Next(unusedIndicies.Count);
            var val = unusedIndicies[num];
            unusedIndicies.RemoveAt(num);
            noRiskCount--;

            var themeIndex = val / maxQuestionsInTheme;
            var questionIndex = val % maxQuestionsInTheme;
            round.Themes[themeIndex].Questions[questionIndex].TypeName = QuestionTypes.NoRisk;
        }

        while (unusedIndicies.Count > 0 && stakeCount > 0)
        {
            var num = Random.Shared.Next(unusedIndicies.Count);
            var val = unusedIndicies[num];
            unusedIndicies.RemoveAt(num);
            stakeCount--;

            var themeIndex = val / maxQuestionsInTheme;
            var questionIndex = val % maxQuestionsInTheme;
            round.Themes[themeIndex].Questions[questionIndex].TypeName = QuestionTypes.Stake;
        }

        while (unusedIndicies.Count > 0 && secretCount > 0)
        {
            var num = Random.Shared.Next(unusedIndicies.Count);
            var val = unusedIndicies[num];
            unusedIndicies.RemoveAt(num);
            secretCount--;

            var themeIndex = val / maxQuestionsInTheme;
            var questionIndex = val % maxQuestionsInTheme;
            var question = round.Themes[themeIndex].Questions[questionIndex];

            question.TypeName = QuestionTypes.Secret;
            question.Parameters ??= new StepParameters();

            question.Parameters[QuestionParameterNames.Theme] = new StepParameter { SimpleValue = round.Themes[themeIndex].Name };
            question.Parameters[QuestionParameterNames.Price] = new StepParameter { NumberSetValue = GenerateRandomSecretQuestionCost() };

            var var = Random.Shared.Next(2);

            var selectionMode = var == 0
                ? StepParameterValues.SetAnswererSelect_Any
                : StepParameterValues.SetAnswererSelect_ExceptCurrent;

            question.Parameters[QuestionParameterNames.SelectionMode] = new StepParameter { SimpleValue = selectionMode };
        }
    }

    private static NumberSet GenerateRandomSecretQuestionCost()
    {
        var option = Random.Shared.Next(3);

        if (option == 0) // Fixed value
        {
            // 100 - 2000
            var price = (Random.Shared.Next(20) + 1) * 100;
            return new NumberSet(price);
        }

        if (option == 1) // Minimum or maximum in round
        {
            return new NumberSet(0);
        }

        // Range value

        var sumMin = (Random.Shared.Next(10) + 1) * 100;
        var sumMax = sumMin + (Random.Shared.Next(10) + 1) * 100;
        var maxSteps = (sumMax - sumMin) / 100;

        var possibleSteps = Enumerable.Range(1, maxSteps).Where(step => maxSteps % step == 0).ToArray();
        var stepIndex = Random.Shared.Next(possibleSteps.Length);
        var steps = possibleSteps[stepIndex];

        return new NumberSet { Minimum = sumMin, Maximum = sumMax, Step = steps * 100 };
    }
}
