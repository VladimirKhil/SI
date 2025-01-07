using SICore.Contracts;
using SICore.Extensions;
using SICore.Models;
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

    public bool ValidateAnswer(string answer, string[] rightAnswers, string[] wrongAnswers) =>
        AnswerChecker.IsAnswerRight(answer, rightAnswers);

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

        var hasActiveQuestions = table.Any(theme => theme.Questions.Any(question => question.IsActive()));

        if (!hasActiveQuestions)
        {
            throw new InvalidOperationException("No active questions on the game table");
        }

        var maxQuestionCount = table.Max(theme => theme.Questions.Count);
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

    public int DeleteTheme(List<ThemeInfo> roundTable) => roundTable.SelectRandom(theme => theme.Name != null, Random.Shared);

    public int SelectPlayer(
        List<PlayerAccount> players,
        int myIndex,
        List<ThemeInfo> roundTable,
        int roundPassedTimePercentage)
    {
        var playerIndex = -1;
        var me = players[myIndex];
        var currentScore = me.Sum;
        var bestOpponentScore = players.Where(player => player != me).Max(player => player.Sum);
        var lowersOpponentScore = players.Where(player => player != me).Min(player => player.Sum);
        var isCritical = IsCritical(roundTable, currentScore, bestOpponentScore, roundPassedTimePercentage);

        if (isCritical || Random.Shared.Next(100) >= _account.V)
        {
            if (isCritical)
            {
                for (var i = 0; i < players.Count; i++)
                {
                    if (players[i] == me && players[i].CanBeSelected)
                    {
                        playerIndex = i;
                        break;
                    }
                }
            }

            // To the strongest one
            if (playerIndex == -1)
            {
                for (var i = 0; i < players.Count; i++)
                {
                    if (players[i].Sum == bestOpponentScore && players[i].CanBeSelected)
                    {
                        playerIndex = i;
                        break;
                    }
                }
            }
        }
        else
        {
            // To the weakest one
            for (var i = 0; i < players.Count; i++)
            {
                if (players[i].Sum == lowersOpponentScore && players[i].CanBeSelected)
                {
                    playerIndex = i;
                    break;
                }
            }
        }

        if (playerIndex == -1)
        {
            playerIndex = players.SelectRandomIndex();
        }

        return playerIndex;
    }

    public (StakeModes mode, int sum) MakeStake(
        List<PlayerAccount> players,
        int myIndex,
        List<ThemeInfo> roundTable,
        StakeInfo stakeInfo,
        int questionIndex,
        int previousStakerIndex,
        bool[] vars,
        int roundPassedTimePercentage)
    {
        var me = players[myIndex];
        var currentScore = me.Sum;
        var bestOpponentScore = players.Where(player => player != me).Max(player => player.Sum);
        var lowersOpponentScore = players.Where(player => player != me).Min(player => player.Sum);
        var isCritical = IsCritical(roundTable, currentScore, bestOpponentScore, roundPassedTimePercentage);

        var stakeSum = -1;
        var stakeDecision = StakeModes.Stake;

        switch (stakeInfo.Reason)
        {
            case StakeReason.HighestPlays:
                {
                    var stakeMode = MakeStake(
                        questionIndex,
                        players.Select(p => p.Sum).ToArray(),
                        myIndex,
                        previousStakerIndex,
                        _account.Style,
                        vars,
                        _account.N1,
                        _account.N5,
                        _account.B1,
                        _account.B5,
                        isCritical,
                        stakeInfo.Minimum,
                        stakeInfo.Step,
                        out stakeSum);

                    if (stakeMode == StakeMode.Nominal)
                    {
                        stakeMode = StakeMode.Sum;
                        stakeSum = stakeInfo.Minimum;
                    }

                    stakeDecision = FromStakeMode(stakeMode);
                    break;
                }

            case StakeReason.Hidden:
                {
                    var sums = players.Select(p => p.Sum).ToArray();
                    stakeSum = MakeFinalStake(sums, myIndex, _account.Style);
                    break;
                }

            default:
                {
                    var optionCount = stakeInfo.Step == 0 ? 2 : (stakeInfo.Maximum - stakeInfo.Minimum) / stakeInfo.Step + 1;
                    var stepIndex = 0;

                    switch (_account.Style)
                    {
                        case PlayerStyle.Careful:
                            stepIndex = 0;
                            break;

                        case PlayerStyle.Normal:
                            stepIndex = Random.Shared.Next(optionCount);
                            break;

                        case PlayerStyle.Agressive:
                            stepIndex = optionCount - 1;
                            break;
                    }

                    stakeSum = stakeInfo.Minimum + stepIndex * stakeInfo.Step;
                    break;
                }
        }

        return (stakeDecision, stakeSum);
    }

    private static StakeModes FromStakeMode(StakeMode stakeMode) =>
        stakeMode switch
        {
            StakeMode.Sum => StakeModes.Stake,
            StakeMode.AllIn => StakeModes.AllIn,
            _ => StakeModes.Pass,
        };

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

        try
        {
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
        }
        catch (IndexOutOfRangeException exc)
        {
            throw new IndexOutOfRangeException($"Input values: {string.Join(',', canSelectQuestion)}, {previousIndex}, {currentScore}, {bestOpponentScore}, {isCritical}, {questionIndiciesPriority}", exc);
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

        try
        {
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
        }
        catch (IndexOutOfRangeException exc)
        {
            throw new IndexOutOfRangeException($"Input values: {string.Join(',', canSelectTheme)}, {previousIndex}, {themeIndiciesPriority}", exc);
        }

        return themeIndex;
    }

    /// <summary>
    /// Makes a stake.
    /// </summary>
    internal static StakeMode MakeStake(
        int questNum,
        int[] sums,
        int myIndex,
        int lastStakerIndex,
        PlayerStyle style,
        bool[] vars,
        int N1,
        int N5,
        int B1,
        int B5,
        bool isCritical,
        int minCost,
        int stakeStep,
        out int stakeSum)
    {
        int i = 0;
        int ran = 0, stake = 0;

        StakeMode stakeMode;
        stakeSum = -1;

        // Вероятность ответить на вопрос
        double p = 0.75 - questNum / 8.0;

        int bestSum = sums.Where((sum, index) => index != myIndex).Max();
        var mySum = sums[myIndex];
        int bestNum = -1;

        for (i = 0; i < sums.Length; i++)
        {
            if (i != myIndex && sums[i] == bestSum)
            {
                bestNum = i;
            }
        }

        var pass = new List<Interval>();
        var min = new List<Interval>();
        var max = new List<Interval>();
        var result = new List<IntervalProbability>();

        // Сначала просчитаем оптимальную реакцию нашего противника на все возможные наши ставки
        StakeDecisions(
            style,
            bestSum,
            mySum,
            vars[1] ? (minCost - (vars[0] ? stakeStep : 0)) : mySum,
            mySum,
            p,
            ref pass,
            ref min,
            ref max,
            true /*т.к. он второй, то он всегда может спасовать*/,
            vars[2] ? sums[lastStakerIndex] : 0,
            stakeStep,
            ref result);

        StakeDecisions(
            style,
            mySum,
            bestSum,
            vars[1] ? (minCost - (vars[0] ? stakeStep : 0)) : mySum,
            mySum,
            p,
            ref pass,
            ref min,
            ref max,
            vars[2],
            vars[2] ? sums[lastStakerIndex] : 0,
            stakeStep,
            ref result);

        int maxL = result[0].Probabilities.Count;
        int li;

        for (int ind = 0; ind < maxL; ind++)
        {
            li = style switch
            {
                PlayerStyle.Agressive => maxL - ind - 1,
                PlayerStyle.Careful => ind,
                _ => 0,
            };

            i = 0;

            while (i < result.Count)
            {
                var prob = result[i];
                int k = 0;
                bool worse = false;

                while (k < result.Count)
                {
                    if (i != k)
                    {
                        if (style == PlayerStyle.Normal)
                        {
                            if (prob.ProbSum < result[k].ProbSum)
                            {
                                worse = true;
                                break;
                            }
                        }
                        else
                        {
                            if (prob.Probabilities[li] < result[k].Probabilities[li])
                            {
                                worse = true;
                                break;
                            }
                        }
                    }
                    k++;
                }

                if (worse)
                {
                    result.RemoveAt(i--);
                }

                i++;
            }

            if (style == PlayerStyle.Normal)
            {
                break;
            }
        }

        if (result.Count == 1 && result[0].Probabilities[0] == 0)
        {
            result[0].Min = result[0].Max = mySum;
        }

        // Проверка на критическую ситуацию
        if (isCritical)
        {
            switch (style)
            {
                case PlayerStyle.Agressive:

                    N1 -= 30;
                    N5 -= 30;
                    B1 += 30;
                    B5 += 30;

                    break;

                case PlayerStyle.Normal:

                    N1 -= 20;
                    N5 -= 20;
                    B1 += 20;
                    B5 += 20;

                    break;

                default:

                    N1 -= 10;
                    N5 -= 10;
                    B1 += 10;
                    B5 += 10;

                    break;
            }
        }

        // Вероятность сделать минимальную ставку
        int pMin = (int)(N1 + questNum / 4.0 * (N5 - N1));
        // Вероятность сделать максимальную ставку
        int pMax = (int)(B1 + questNum / 4.0 * (B5 - B1));

        int totalL = 0;

        if (pMin < 0)
        {
            totalL = 0;

            foreach (IntervalProbability interval in result)
            {
                totalL += interval.Length;
            }

            int newVal = -pMin * totalL / (-pMin + pMax);
            newVal = IntervalProbability.Locale(ref result, newVal);
            i = 0;

            while (i < result.Count)
            {
                if (result[i].Max < newVal)
                {
                    result.RemoveAt(i--);
                }
                else if (result[i].Min < newVal)
                {
                    result[i].Min = newVal;
                }

                i++;
            }

            pMin = 0;

            if (pMax <= 0)
            {
                pMax = 50;
            }
        }
        else if (pMax < 0)
        {
            totalL = 0;

            foreach (IntervalProbability interval in result)
            {
                totalL += interval.Length;
            }

            int newVal = -pMax * totalL / (pMin + -pMax);
            newVal = IntervalProbability.Locale(ref result, newVal);
            i = 0;

            while (i < result.Count)
            {
                if (result[i].Min > newVal)
                {
                    result.RemoveAt(i--);
                }
                else if (result[i].Max > newVal)
                {
                    result[i].Max = newVal;
                }

                i++;
            }

            pMax = 0;

            if (pMin <= 0)
            {
                pMin = 50;
            }
        }

        totalL = 0;

        foreach (var interval in result)
        {
            totalL += interval.Length;
        }

        int square = (pMin + pMax) * totalL / 2;
        ran = Random.Shared.Next(square);
        int c = Math.Min(pMin, pMax);
        int d = Math.Abs(pMin - pMax);
        long bb = (long)((pMin + c) / 2.0 * totalL);
        int x = pMin != 0 ? (ran / pMin) : totalL / 2;

        if (d > 0)
        {
            x = (int)((Math.Sqrt(bb * bb + 2.0 * ran * totalL * d) - bb) / d);
        }

        stake = IntervalProbability.Locale(ref result, x);

        int round = 0;
        i = 0;
        while (stake % stakeStep != 0 && i < 2)
        {
            round = style == PlayerStyle.Careful && i == 1
                || style == PlayerStyle.Agressive && i == 0
                || style == PlayerStyle.Normal && (i == 0 && stake % stakeStep >= 50 || i == 1 && stake % stakeStep < 50)
                ? (int)Math.Ceiling(stake / (double)stakeStep) * stakeStep
                : (int)Math.Floor(stake / (double)stakeStep) * stakeStep;

            foreach (Interval interval in result)
            {
                if (round >= interval.Min && round <= interval.Max)
                {
                    stake = round;
                    break;
                }
            }

            i++;
        }

        if (stake == 0)
            stakeMode = StakeMode.Pass;
        else if (stake == minCost - stakeStep && vars[0])
            stakeMode = StakeMode.Nominal;
        else if (stake == mySum)
            stakeMode = StakeMode.AllIn;
        else
        {
            stakeMode = StakeMode.Sum;
            stakeSum = stake;
        }

        return stakeMode;
    }

    /// <summary>
    /// Makes a decision about stake.
    /// Работает в двух режимах:
    /// 1. Для каждой своей ставки предсказывается оптимальная реакция для оппонента (пас, увеличение ставки или Ва-Банк).
    /// 2. После определения наиболее ожидаемой реакции вычисляется оптимальный по Парето интервал ставок
    /// </summary>
    /// <param name="style">Стиль игрока</param>
    /// <param name="mySum">Сумма</param>
    /// <param name="bestSum">Сумма лучшего из оппонентов</param>
    /// <param name="minStake">Минимальная возникающая ставка</param>
    /// <param name="maxStake">Максимальная возникающая ставка</param>
    /// <param name="p">Вероятность ответа на вопрос</param>
    /// <param name="pass">Интервалы ставок, на которых игрок спасует</param>
    /// <param name="min">Интервалы ставок, на которых игрок перекупит за минммальную сумму ставки</param>
    /// <param name="max">Интервалы ставок, на которых игрок сделает максимальную разумную ставку</param>
    /// <param name="canPass">Возможен ли пас в качестве варианта ставок</param>
    /// <param name="stakerSum">Сумма на счёте ставящего при возможности паса (иначе 0)</param>
    /// <param name="stakeStep">Minimum stake step value.</param>
    /// <param name="result">Паретооптимальное множество ставок</param>
    internal static void StakeDecisions(
        PlayerStyle style,
        int mySum,
        int bestSum,
        int minStake,
        int maxStake,
        double p,
        ref List<Interval> pass,
        ref List<Interval> min,
        ref List<Interval> max,
        bool canPass,
        int stakerSum,
        int stakeStep,
        ref List<IntervalProbability> result)
    {
        // Для всех возможных ставок определим наиболее вероятные ответы соперника

        // Набор опорных точек, охватывающий интервал всех своих возможных ставок
        // Каждая опорная точка разделяет качественно отличающиеся ставки (дающие разные вероятности исходов)
        // Ставки между опорными точками приводят к одинаковому исходу
        // Обязательными опорными точками являются минимальная и максимальная ставки
        var points = new PosUnicList(minStake, maxStake);

        // lambda - отношение своей суммы к сумме лидера среди оппонентов
        // Цель Аукциона - максиммизировать lambda
        var lambdas = new List<double>();

        // Является ли игрок лидером
        bool leader = mySum > bestSum;
        // Вторая итерация анализа, более умный просчёт
        bool smart = pass.Count + min.Count + max.Count > 0;

        if (!leader)
        {
            // Отстаёт в счёте
            // Цель - лидерство
            lambdas.Add(0.5);
            lambdas.Add(0.75);
            lambdas.Add(1);
        }
        else
        {
            // Лидирует в счёте
            // Цель - сохранение лидерства и отрыв
            lambdas.Add(1);
            lambdas.Add(2);

            switch (style)
            {
                case PlayerStyle.Agressive:
                    lambdas.Add(4);
                    break;

                case PlayerStyle.Normal:
                    lambdas.Add(3);
                    break;

                default:
                    break;
            }
        }

        points.Add(new DirPoint(minStake, true));

        for (int i = 0; i < lambdas.Count; i++)
        {
            var l = Math.Pow(lambdas[i], smart ? -1 : 1);
            int sum1 = mySum, sum2 = bestSum;

            if (smart)
            {
                sum1 = bestSum;
                sum2 = mySum;
            }

            points.Add(new DirPoint(sum1 / l - sum2 - (leader ? 1 : 0) * (smart ? -1 : 1), smart));
            points.Add(new DirPoint(sum2 - sum1 / l + (leader ? 1 : 0) * (smart ? -1 : 1), !smart));
            points.Add(new DirPoint(l * sum2 - sum1 - stakeStep + (leader ? 1 : 0) * (smart ? -1 : 1), !smart));
            points.Add(new DirPoint(sum1 - l * sum2 - stakeStep - (leader ? 1 : 0) * (smart ? -1 : 1), smart));
        }

        points.Add(new DirPoint(maxStake, false));
        points.Add(new DirPoint(maxStake, true));

        if (maxStake > mySum)
        {
            points.Add(new DirPoint(mySum, false));
            points.Add(new DirPoint(mySum, true));
        }

        points.Sort(new IntervalComparer());
        int pCount = points.Count;

        var intervals = new List<Interval>();

        if (canPass)
        {
            intervals.Add(new Interval(0, 0));
        }

        for (int i = 0; i < pCount - 1; i++)
        {
            int minI = i == 0 ? points[0].Value : intervals[^1].Max + stakeStep;

            if (minI > maxStake)
            {
                break;
            }

            int maxI = points[i + 1].Direction || i < points.Count - 2
                && points[i + 2].Value == points[i + 1].Value
                ? Math.Max(points[i + 1].Value - stakeStep, minI)
                : points[i + 1].Value;

            intervals.Add(new Interval(minI, maxI));
        }

        if ((smart ? mySum : bestSum) == maxStake && maxStake % stakeStep != 0)
        {
            intervals.Add(new Interval(maxStake, maxStake));
        }

        if (smart)
        {
            #region smartThinking

            // Дробим интервалы в соответствии с предполагаемой стратегией противника
            Interval.SplitBy(ref intervals, pass);
            Interval.SplitBy(ref intervals, min);
            Interval.SplitBy(ref intervals, max);

            result.Clear();

            foreach (var interval in intervals)
            {
                int val = interval.Min;
                int type = 2;

                foreach (Interval part in pass)
                {
                    if (part.Min <= interval.Min && part.Max >= interval.Max)
                    {
                        type = 0;
                        break;
                    }
                }

                if (type == 2)
                {
                    foreach (var part in min)
                    {
                        if (part.Min <= interval.Min && part.Max >= interval.Max)
                        {
                            type = 1;
                            break;
                        }
                    }
                }

                var prob = new IntervalProbability(interval.Min, interval.Max);
                result.Add(prob);

                for (int j = 0; j < lambdas.Count; j++)
                {
                    double l = lambdas[j];

                    switch (type)
                    {
                        case 0:
                            if (val == 0)
                                prob.Probabilities.Add(((mySum / l >= Math.Max(stakerSum + Math.Min(minStake - stakeStep, stakerSum), bestSum) + (leader ? 1 : 0)) ? p : 0) + ((mySum / l >= Math.Max(stakerSum - Math.Min(minStake - stakeStep, stakerSum), bestSum) + (leader ? 1 : 0)) ? 1 - p : 0));
                            else
                                prob.Probabilities.Add(((val >= l * bestSum - mySum + (leader ? 1 : 0)) ? 1 : 0) * p + ((val <= mySum - l * bestSum - (leader ? 1 : 0)) ? 1 : 0) * (1 - p));
                            break;

                        case 1:
                            if (val == 0)
                                prob.Probabilities.Add(((mySum / l >= bestSum + Math.Min(minStake, bestSum) + (leader ? 1 : 0)) ? p : 0) + ((mySum / l >= bestSum - Math.Min(minStake, bestSum) + (leader ? 1 : 0)) ? 1 - p : 0));
                            else
                                prob.Probabilities.Add(((mySum / l >= bestSum + Math.Min(val + stakeStep, bestSum) + (leader ? 1 : 0)) ? 1 : 0) * p + ((mySum / l >= bestSum - Math.Min(val + stakeStep, bestSum) + (leader ? 1 : 0)) ? 1 : 0) * (1 - p));
                            break;

                        default:
                            if (!leader && val < mySum)
                                prob.Probabilities.Add(((mySum / l >= bestSum + mySum + stakeStep) ? 1 : 0) * p + ((mySum / l >= bestSum - mySum - stakeStep) ? 1 : 0) * (1 - p));
                            else
                                prob.Probabilities.Add(((mySum / l > 2 * bestSum) ? 1 : 0) * p + ((mySum / l > 0) ? 1 : 0) * (1 - p));

                            break;
                    }
                }
            }

            // Склеиваем соседние участки с равными весами
            IntervalProbability.Join(ref result, stakeStep);

            // Выделяем паретооптимальные решения
            int i = 0;

            while (i < result.Count)
            {
                IntervalProbability prob = result[i];
                int k = 0;

                while (k < result.Count)
                {
                    if (i != k)
                    {
                        bool getout = true;
                        bool worse = false;

                        for (int l = 0; l < prob.Probabilities.Count; l++)
                        {
                            if (!worse && prob.Probabilities[l] < result[k].Probabilities[l])
                                worse = true;
                            if (prob.Probabilities[l] > result[k].Probabilities[l])
                                getout = false;
                        }
                        if (getout && worse)
                        {
                            result.RemoveAt(i--);
                            break;
                        }
                    }
                    k++;
                }
                i++;
            }

            #endregion
        }
        else
        {
            #region notSmartThinking

            foreach (Interval interval in intervals)
            {
                var val = interval.Min;
                var a = new double[lambdas.Count][];

                for (var j = 0; j < lambdas.Count; j++)
                {
                    var l = lambdas[j];
                    a[j] = new double[3];

                    if (val == 0)
                    {
                        a[j][0] = ((mySum / l >= Math.Max(bestSum, stakerSum + Math.Min(minStake - stakeStep, stakerSum)) + (leader ? 1 : 0)) ? p : 0) + ((mySum / l >= Math.Max(bestSum, stakerSum - Math.Min(minStake - stakeStep, stakerSum)) + (leader ? 1 : 0)) ? 1 - p : 0);
                        a[j][1] = (((mySum + Math.Min(minStake, mySum)) / l >= bestSum + (leader ? 1 : 0)) ? p : 0) + (((mySum - Math.Min(minStake, mySum)) / l >= bestSum + (leader ? 1 : 0)) ? 1 - p : 0);
                        if (!leader || val == maxStake)
                            a[j][2] = ((2 * mySum / l >= bestSum) ? p : 0) + (((mySum - Math.Min(minStake, mySum)) / l >= bestSum) ? 1 - p : 0);
                        else
                            a[j][2] = (((mySum + bestSum + stakeStep) / l > bestSum) ? p : 0) + (((mySum - bestSum - stakeStep) / l > bestSum) ? 1 - p : 0);
                    }
                    else
                    {
                        a[j][0] = ((val <= mySum / l - bestSum - (leader ? 1 : 0)) ? 1 : 0) * p + ((val >= bestSum - mySum / l + (leader ? 1 : 0)) ? 1 : 0) * (1 - p);
                        a[j][1] = (((mySum + Math.Min(val + stakeStep, mySum)) / l >= bestSum + (leader ? 1 : 0)) ? 1 : 0) * p + (((mySum - Math.Min(val + stakeStep, mySum)) / l >= bestSum + (leader ? 1 : 0)) ? 1 : 0) * (1 - p);
                        if (!leader || val == maxStake)
                            a[j][2] = ((2 * mySum / l >= bestSum) ? 1 : 0) * p;
                        else
                            a[j][2] = (((mySum + bestSum + stakeStep) / l > bestSum) ? 1 : 0) * p + (((mySum - bestSum - stakeStep) / l > bestSum) ? 1 : 0) * (1 - p);
                    }
                }

                var candidates = new List<int> { 0 };

                if (val != 0 && val < mySum && val != maxStake || val == 0 && stakerSum != minStake - stakeStep && minStake - stakeStep < mySum)
                    candidates.Add(1);
                if (val != 0 && val <= mySum || val == 0 && minStake - stakeStep <= mySum && mySum > 0)
                    candidates.Add(2);

                int i = -1;

                while (candidates.Count > 1 && ++i < lambdas.Count)
                {
                    for (var j = 0; j < candidates.Count; j++)
                    {
                        var getOut = false;

                        for (var k = 0; k < candidates.Count; k++)
                        {
                            if (k == j)
                            {
                                continue;
                            }

                            if (a[i][candidates[j]] < a[i][candidates[k]])
                            {
                                getOut = true;
                                break;
                            }
                        }

                        if (getOut)
                        {
                            candidates.RemoveAt(j--);
                        }
                    }
                }

                int decision = 0;

                if (candidates.Count > 1)
                {
                    int j;

                    switch (style)
                    {
                        case PlayerStyle.Agressive:
                            for (j = 2; j >= 0; j--)
                            {
                                if (candidates.Contains(j))
                                {
                                    decision = j;
                                    break;
                                }
                            }
                            break;

                        case PlayerStyle.Careful:
                            for (j = 0; j <= 2; j++)
                            {
                                if (candidates.Contains(j))
                                {
                                    decision = j;
                                    break;
                                }
                            }
                            break;

                        default:
                            var index = Random.Shared.Next(candidates.Count);
                            j = candidates[index];

                            decision = j;
                            break;
                    }
                }
                else
                {
                    decision = candidates[0];
                }

                switch (decision)
                {
                    case 0:
                        pass.Add(interval);
                        break;

                    case 1:
                        min.Add(interval);
                        break;

                    default:
                        max.Add(interval);
                        break;
                }
            }

            Interval.Join(ref pass, stakeStep);
            Interval.Join(ref min, stakeStep);
            Interval.Join(ref max, stakeStep);

            #endregion
        }
    }

    internal static int MakeFinalStake(int[] sums, int myIndex, PlayerStyle style)
    {
        var res = new List<Interval>();
        var ops = new List<int>();

        var otherSums = sums.Where((sum, index) => index != myIndex).ToArray();
        var otherBestSum = otherSums.Max();
        var mySum = sums[myIndex];

        if (otherBestSum > 0)
        {
            ops.Add(otherBestSum);

            if (sums.Length > 2)
            {
                var bestIndex = Array.IndexOf(otherSums, otherBestSum);
                otherSums = otherSums.Where((sum, index) => index != bestIndex).ToArray();
                var secondSum = otherSums.Max();

                if (secondSum > 0)
                {
                    ops.Add(secondSum);
                }
            }

            // Определяем множество паретооптимальных ставок
            ParetoStakes(mySum, ops.ToArray(), ref res);
        }
        else
        {
            res.Add(new Interval(1, Math.Max(mySum - 1, 1)));
        }

        var stake = 1;
        var numVars = 0;

        foreach (var inter in res)
        {
            numVars += inter.Length;
        }

        if (style != PlayerStyle.Normal && Random.Shared.Next(100) >= 20)
        {
            stake = style == PlayerStyle.Agressive ? res[^1].Max : res[0].Min;
        }
        else
        {
            int variant = Random.Shared.Next(numVars);
            stake = Interval.Locale(ref res, variant);
        }

        return stake;
    }

    /// <summary>
    /// Определение ставок в финальном раунде, оптимальных по Парето
    /// </summary>
    /// <param name="mySum">Моя сумма</param>
    /// <param name="opponentsSums">Суммы соперников</param>
    /// <param name="paretoStakes">Результирующее множество паретооптимальных ставок</param>
    internal static void ParetoStakes(int mySum, int[] opponentsSums, ref List<Interval> paretoStakes)
    {
        #region Comments

        // Вводится понятие двух критериев. Оба мы пытаемся максимизировать (отсюда паретооптимальность)
        // Оптимистичный критерий O(x) - вероятность выиграть при своей ставке x и своём верном ответе
        // (Неопределённость заключается в ставках и ответах соперников)
        // Пессимистичный критерий P(x) - вероятность выиграть при своей ставке x и своём неверном ответе
        // Вероятность ответа любого игрока считается равной 0.5
        // Возможные ставки оппонентов не равновероятны, выбираются самые вероятные ставки
        // Для лидера: 1, перекрывающая ставка (ва-банк при нескольких лидерах), доставка до ближайшего преследователя
        // Для преследователя: 1, ва-банк, доставка до лидера (если возможна)
        // Поэтому O(x) и P(x) представляют собой линейно постоянные функции
        // Решение задачи всегда существует

        #endregion

        // Определяем ставки соперников
        // Определяем возможные исходы для соперников

        // Исходы, меньшие суммы
        List<int> less = new List<int>();
        // Исходы, большие суммы
        List<int> more = new List<int>();
        // Исходы, равные сумме, нас не интересуют

        // Ищем сумму лидера
        int bestSum = mySum;
        foreach (int sum in opponentsSums)
        {
            if (sum > bestSum)
            {
                bestSum = sum;
            }
        }

        // Минимальный результат, который возможен у лидера
        int leaderMinRes = 0;

        // Заполняем множества исходов
        #region Sets

        for (int i = 0; i < opponentsSums.Length; i++)
        {
            // Ищем сумму лучшего соперника
            int bestOpponentSum = mySum;
            for (int j = 0; j < opponentsSums.Length; j++)
                if (i != j && opponentsSums[j] > bestOpponentSum)
                    bestOpponentSum = opponentsSums[j];

            // Возможные ставки
            List<int> stakes = new List<int>();

            if (opponentsSums[i] == bestSum)
            {
                // Если лидер
                stakes.Add(1);
                // Перекрытие
                if (bestOpponentSum < opponentsSums[i])
                {
                    stakes.Add(2 * bestOpponentSum - opponentsSums[i] + 1);
                    // Доставка
                    stakes.Add(opponentsSums[i] - bestOpponentSum);
                }
                else
                    stakes.Add(opponentsSums[i]);
            }
            else
            {
                // Иначе
                stakes.Add(1);
                stakes.Add(opponentsSums[i]);
                // Доставка
                if (opponentsSums[i] * 2 > bestOpponentSum)
                    stakes.Add(bestOpponentSum - opponentsSums[i]);
            }

            // Пишем во множества
            List<int> results = new List<int>();
            foreach (int stake in stakes)
            {
                results.Add(opponentsSums[i] + stake);
                results.Add(opponentsSums[i] - stake);
            }

            if (opponentsSums[i] == bestSum)
            {
                leaderMinRes = opponentsSums[i];
                foreach (int res in results)
                    if (res < leaderMinRes)
                        leaderMinRes = res;
            }

            foreach (int res in results)
                if (res < mySum && res >= leaderMinRes && !less.Contains(res))
                    less.Add(res);
                else if (res > mySum && res <= mySum * 2 && !more.Contains(res))
                    more.Add(res);
        }

        #endregion

        // Строим уровни эквивалентных ставок по оптимистичному и пессимистичному критериям

        #region Levels

        List<Interval> opt = new List<Interval>();
        List<Interval> pess = new List<Interval>();

        if (more.Count == 0)
            opt.Add(new Interval(1, mySum));
        else
        {
            more.Sort();
            for (int i = 0; i < more.Count; i++)
            {
                if (i == 0)
                {
                    if (more[i] > mySum + 1)
                        opt.Add(new Interval(1, more[i] - mySum - 1));
                }
                else
                {
                    if (more[i] > more[i - 1] + 1)
                        opt.Add(new Interval(more[i - 1] - mySum + 1, more[i] - mySum - 1));
                }
                opt.Add(new Interval(more[i] - mySum, more[i] - mySum));
                if (i == more.Count - 1)
                {
                    if (more[i] < 2 * mySum)
                        opt.Add(new Interval(more[i] - mySum + 1, mySum));
                }
            }
        }

        if (less.Count == 0)
            pess.Add(new Interval(1, mySum));
        else
        {
            less.Sort();
            less.Reverse();
            for (int i = 0; i < less.Count; i++)
            {
                if (i == 0)
                {
                    if (less[i] < mySum - 1)
                        pess.Add(new Interval(1, mySum - less[i] - 1));
                }
                else
                {
                    if (less[i] < less[i - 1] - 1)
                        pess.Add(new Interval(mySum - less[i - 1] + 1, mySum - less[i] - 1));
                }
                pess.Add(new Interval(mySum - less[i], mySum - less[i]));
                if (i == less.Count - 1)
                {
                    if (less[i] > 0)
                        pess.Add(new Interval(mySum - less[i] + 1, mySum));
                }
            }
        }

        #endregion

        // Определяем паретооптимальное множество

        #region ParetoStakes

        paretoStakes.Clear();
        int optI = 0, pessI = 0;

        while (optI < opt.Count || pessI < pess.Count)
        {
            if (opt[optI].Max >= pess[pessI].Max)
            {
                paretoStakes.Add(pess[pessI]);
                optI++;

                if (optI < opt.Count)
                {
                    int val = opt[optI].Min;

                    do
                    {
                        pessI++;
                    } while (!(pess[pessI].Min <= val && pess[pessI].Max >= val));
                }
                else
                {
                    pessI = pess.Count;

                    // Уберём слишком большие ставки для лидера
                    int bestOpponentSum = opponentsSums[0];

                    for (int j = 1; j < opponentsSums.Length; j++)
                    {
                        if (opponentsSums[j] > bestOpponentSum)
                            bestOpponentSum = opponentsSums[j];
                    }

                    if (mySum > bestOpponentSum)
                    {
                        if (mySum > bestOpponentSum + 200 && 2 * bestOpponentSum + 100 > mySum)
                            paretoStakes[^1].Max = 2 * bestOpponentSum - mySum + 100;
                        else if (2 * bestOpponentSum + 1 > mySum)
                            paretoStakes[^1].Max = 2 * bestOpponentSum - mySum + 1;
                    }
                }
            }
            else
            {
                pess[pessI].Min = opt[optI].Max + 1;
                optI++;
            }
        }

        Interval.Join(ref paretoStakes, 1);

        #endregion
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
