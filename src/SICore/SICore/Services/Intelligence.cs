using SICore.Contracts;
using SICore.Utils;
using SIData;
using SIUI.Model;

namespace SICore.Services;

/// <summary>
/// Provides a default implementation of the <see cref="IIntelligence" /> contract.
/// </summary>
internal sealed class Intelligence : IIntelligence
{
    private readonly ComputerAccount _account;

    public Intelligence(ComputerAccount account) => _account = account;

    /// <summary>
    /// Selects question from game table.
    /// </summary>
    public (int themeIndex, int questionIndex) SelectQuestion(
        List<ThemeInfo> table,
        (int ThemeIndex, int QuestionIndex) previousSelection,
        int currentScore,
        int bestOpponentScore,
        int roundPassedTimePercentage)
    {
        var themeIndex = -1;
        var questionIndex = -1;
        var pSelectByThemeIndex = _account.V1;

        if (_account.Style == PlayerStyle.Agressive && currentScore < 2 * bestOpponentScore)
        {
            pSelectByThemeIndex = 0;
        }
        else if (_account.Style == PlayerStyle.Normal && currentScore < bestOpponentScore)
        {
            pSelectByThemeIndex = 0;
        }

        var isCritical = IsCritical(table, currentScore, bestOpponentScore, roundPassedTimePercentage);

        if (isCritical)
        {
            pSelectByThemeIndex = 0;
        }

        var r = Random.Shared.Next(101);
        var selectByThemeIndex = r < pSelectByThemeIndex;

        if (table.Count == 0)
        {
            throw new InvalidOperationException("Game table is empty");
        }

        var maxQuestionCount = table.Max(theme => theme.Questions.Count(QuestionHelper.IsActive));

        if (maxQuestionCount == 0)
        {
            throw new InvalidOperationException("No questions in the game table");
        }

        var canSelectTheme = new bool[table.Count];
        var canSelectQuestion = new bool[maxQuestionCount];

        if (selectByThemeIndex)
        {
            for (var i = 0; i < table.Count; i++)
            {
                canSelectTheme[i] = table[i].Questions.Any(QuestionHelper.IsActive);
            }

            themeIndex = SelectThemeIndex(canSelectTheme, previousSelection.ThemeIndex);

            for (var i = 0; i < canSelectQuestion.Length; i++)
            {
                canSelectQuestion[i] = i < table[themeIndex].Questions.Count && table[themeIndex].Questions[i].IsActive();
            }

            questionIndex = SelectQuestionIndex(canSelectQuestion, previousSelection.QuestionIndex, currentScore, bestOpponentScore, isCritical);
        }
        else
        {
            for (var i = 0; i < canSelectQuestion.Length; i++)
            {
                canSelectQuestion[i] = table.Any(theme => theme.Questions.Count > i && theme.Questions[i].IsActive());
            }

            questionIndex = SelectQuestionIndex(canSelectQuestion, previousSelection.QuestionIndex, currentScore, bestOpponentScore, isCritical);

            for (var i = 0; i < table.Count; i++)
            {
                canSelectTheme[i] = table[i].Questions.Count > questionIndex && table[i].Questions[questionIndex].IsActive();
            }

            themeIndex = SelectThemeIndex(canSelectTheme, previousSelection.ThemeIndex);
        }

        return (themeIndex, questionIndex);
    }

    private int SelectQuestionIndex(bool[] canSelectQuestion, int previousIndex, int currentScore, int bestOpponentScore, bool isCritical)
    {
        // Question selection
        var questionCount = canSelectQuestion.Count(can => can);
        var questionIndex = -1;

        int pSelectLowerPrice = _account.V4,
            pSelectHigherPrice = _account.V5,
            pSelectExactPrice = _account.V6;

        var pSelectByQuestionPriority = _account.V7;
        var questionIndiciesPriority = _account.P2;

        if (_account.Style == PlayerStyle.Agressive && currentScore < 2 * bestOpponentScore)
        {
            pSelectLowerPrice = pSelectHigherPrice = pSelectExactPrice = 0;
            pSelectByQuestionPriority = 100;
        }
        else if (_account.Style == PlayerStyle.Normal && currentScore < bestOpponentScore)
        {
            pSelectLowerPrice = pSelectHigherPrice = pSelectExactPrice = 0;
            pSelectByQuestionPriority = 80;
        }

        if (isCritical)
        {
            pSelectLowerPrice = pSelectHigherPrice = pSelectExactPrice = 0;
            pSelectByQuestionPriority = 100;
            questionIndiciesPriority = "54321".ToCharArray();
        }

        if (questionCount == 1)
        {
            // Single question is available
            questionIndex = Array.FindIndex(canSelectQuestion, can => can);
        }
        else
        {
            bool canSelectLowerPrice = false, canSelectHigherPrice = false, canSelectExactPrice = false;
            int lowerPriceCount = 0, higherPriceCount = 0;

            if (previousIndex != -1)
            {
                for (var k = 0; k < canSelectQuestion.Length; k++)
                {
                    if (canSelectQuestion[k])
                    {
                        if (k < previousIndex) { canSelectLowerPrice = true; lowerPriceCount++; }
                        else if (k == previousIndex) canSelectExactPrice = true;
                        else { canSelectHigherPrice = true; higherPriceCount++; }
                    }
                }
            }

            var maxr = 100;

            if (!canSelectLowerPrice) maxr -= pSelectLowerPrice;
            if (!canSelectHigherPrice) maxr -= pSelectHigherPrice;
            if (!canSelectExactPrice) maxr -= pSelectExactPrice;

            var r = Random.Shared.Next(maxr);

            if (!canSelectLowerPrice) r += pSelectLowerPrice;
            if (!canSelectHigherPrice && r >= pSelectLowerPrice) r += pSelectHigherPrice;
            if (!canSelectExactPrice && r >= pSelectLowerPrice + pSelectHigherPrice) r += pSelectExactPrice;

            if (r < pSelectLowerPrice)
            {
                var k = Random.Shared.Next(lowerPriceCount);
                questionIndex = Math.Min(previousIndex, canSelectQuestion.Length);
                do if (canSelectQuestion[--questionIndex]) k--; while (k >= 0);
            }
            else if (r < pSelectLowerPrice + pSelectHigherPrice)
            {
                var k = Random.Shared.Next(higherPriceCount);
                questionIndex = Math.Max(previousIndex, -1);
                do if (canSelectQuestion[++questionIndex]) k--; while (k >= 0);
            }
            else if (r < pSelectLowerPrice + pSelectHigherPrice + pSelectExactPrice)
            {
                questionIndex = previousIndex;
            }
            else if (r < pSelectLowerPrice + pSelectHigherPrice + pSelectExactPrice + pSelectByQuestionPriority)
            {
                // Selecting a question according to the priority
                for (var k = 0; k < questionIndiciesPriority.Length; k++)
                {
                    var index = questionIndiciesPriority[k] - '0' - 1;

                    if (index > -1 && index < canSelectQuestion.Length && canSelectQuestion[index])
                    {
                        questionIndex = index;
                        break;
                    }
                }
            }

            if (questionIndex == -1)
            {
                var k = Random.Shared.Next(questionCount);
                questionIndex = -1;
                do if (canSelectQuestion[++questionIndex]) k--; while (k >= 0);
            }
        }

        if (questionIndex < 0 || questionIndex >= canSelectQuestion.Length || !canSelectQuestion[questionIndex])
        {
            throw new InvalidOperationException($"Question index was not defined correctly: {questionIndex} of {canSelectQuestion.Length}");
        }

        return questionIndex;
    }

    private int SelectThemeIndex(bool[] canSelectTheme, int previousIndex)
    {
        // Theme selection
        var themeCount = canSelectTheme.Count(can => can);
        var themeIndex = -1;

        var pSelectPreviousTheme = _account.V2;
        var pSelectByThemePriority = _account.V3;
        var themeIndiciesPriority = _account.P1;

        if (themeCount == 1)
        {
            // Single theme is available
            themeIndex = Array.FindIndex(canSelectTheme, can => can);
        }
        else
        {
            var canSelectPreviousTheme = false; // Can the previous theme be selected

            if (previousIndex > -1 && previousIndex < canSelectTheme.Length && canSelectTheme[previousIndex])
            {
                canSelectPreviousTheme = true;
            }

            var r = canSelectPreviousTheme ? Random.Shared.Next(100) : pSelectPreviousTheme + Random.Shared.Next(100 - pSelectPreviousTheme);

            if (r < pSelectPreviousTheme)
            {
                themeIndex = previousIndex;
            }
            else if (r < pSelectPreviousTheme + pSelectByThemePriority)
            {
                // Selecting a theme according to the priority
                for (var k = 0; k < themeIndiciesPriority.Length; k++)
                {
                    var index = themeIndiciesPriority[k] - '0' - 1;

                    if (index > -1 && index < canSelectTheme.Length && canSelectTheme[index])
                    {
                        themeIndex = index;
                        break;
                    }
                }
            }

            if (themeIndex == -1)
            {
                var k = Random.Shared.Next(themeCount);
                themeIndex = -1;

                do
                {
                    themeIndex++;

                    if (themeIndex < canSelectTheme.Length && canSelectTheme[themeIndex])
                    {
                        k--;
                    }
                } while (k >= 0);
            }
        }

        if (themeIndex < 0 || themeIndex >= canSelectTheme.Length || !canSelectTheme[themeIndex])
        {
            throw new InvalidOperationException($"Theme index was not defined correctly: {themeIndex} of {canSelectTheme.Length}");
        }

        return themeIndex;
    }

    /// <summary>
    /// Checks if situation is critical.
    /// </summary>
    private bool IsCritical(List<ThemeInfo> roundTable, int currentScore, int bestOpponentScore, int roundPassedTimePercentage)
    {
        var leftQuestionCount = roundTable.Sum(theme => theme.Questions.Count(QuestionHelper.IsActive));

        return (leftQuestionCount <= _account.Nq || roundPassedTimePercentage > 100 - 10 * _account.Nq / 3)
            && currentScore < bestOpponentScore * _account.Part / 100;
    }
}
