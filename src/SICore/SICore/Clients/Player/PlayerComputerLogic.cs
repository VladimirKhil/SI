using EnsureThat;
using SICore.Clients.Player;
using SICore.Contracts;
using SICore.Helpers;
using SICore.Models;
using SICore.Utils;
using SIData;
using SIPackages.Core;
using System.Text;
using R = SICore.Properties.Resources;

namespace SICore;

/// <summary>
/// Defines a player computer logic.
/// </summary>
internal sealed class PlayerComputerLogic : IPersonLogic
{
    private const int DefaultThemeQuestionCount = 5;

    private readonly IIntelligence _intelligence;
    private readonly ComputerAccount _account;
    private readonly ViewerActions _viewerActions;
    private readonly ViewerData _data;

    private readonly TimerInfo[] _timersInfo;

    private int _themeQuestionCount = -1;

    /// <summary>
    /// Does the player know the answer.
    /// </summary>
    internal bool KnowsAnswer { get; set; } = false;

    /// <summary>
    /// Is the player sure in the answer.
    /// </summary>
    internal bool IsSure { get; set; } = false;

    /// <summary>
    /// Is the player ready to press the button.
    /// </summary>
    internal bool ReadyToPress { get; set; } = false;

    private int _realBrave = 0;

    /// <summary>
    /// Current brave value.
    /// </summary>
    internal int RealBrave { get => _realBrave; set { _realBrave = Math.Max(0, value); } }

    /// <summary>
    /// Brave change speed.
    /// </summary>
    internal int DeltaBrave { get; set; } = 0;

    /// <summary>
    /// Current reaction speed.
    /// </summary>
    internal int RealSpeed { get; set; } = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerComputerLogic"/> class.
    /// </summary>
    public PlayerComputerLogic(ViewerData data, ComputerAccount account, IIntelligence intelligence, ViewerActions viewerActions, TimerInfo[] timerInfos)
    {
        Ensure.That(account).IsNotNull();

        _intelligence = intelligence;
        _account = account;
        _viewerActions = viewerActions;
        _data = data;
        _timersInfo = timerInfos;
    }

    // TODO: Switch to TaskRunner and support task cancellation
    private async void ScheduleExecution(PlayerTasks task, double taskTime)
    {
        await Task.Delay((int)taskTime * 100);

        try
        {
            ExecuteTask(task);
        }
        catch (Exception exc)
        {
            _data.SystemLog.AppendFormat("Execution error: {0}", exc.ToString()).AppendLine();
        }
    }

    private void ExecuteTask(PlayerTasks task)
    {
        switch (task)
        {
            case PlayerTasks.Ready:
                Ready();
                break;

            case PlayerTasks.Answer:
                AnswerTask();
                break;

            case PlayerTasks.Choose:
                Choose();
                break;

            case PlayerTasks.AnswerRight:
                AnswerRight();
                break;

            case PlayerTasks.AnswerWrong:
                AnswerWrong();
                break;

            case PlayerTasks.Cat:
                OnSecretQuestionAnswererSelect(Messages.Cat);
                break;

            case PlayerTasks.SelectPlayer:
                OnSelectPlayer();
                break;

            case PlayerTasks.CatCost:
                CatCostTask();
                break;

            case PlayerTasks.ChooseFinal:
                ChooseFinal();
                break;

            case PlayerTasks.FinalStake:
                FinalStakeTask();
                break;

            case PlayerTasks.Stake:
                StakeTask();
                break;

            case PlayerTasks.StakeNew:
                StakeTaskNew();
                break;

            case PlayerTasks.PressButton:
                PressButton();
                break;

            default:
                break;
        }
    }

    private int GetTimePercentage(int timerIndex)
    {
        var now = DateTime.UtcNow;
        var timer = _timersInfo[timerIndex];

        if (!timer.IsEnabled)
        {
            return timer.PauseTime > -1 ? 100 * timer.PauseTime / timer.MaxTime : 0;
        }

        return (int)(100 * (now - timer.StartTime).TotalMilliseconds / (timer.EndTime - timer.StartTime).TotalMilliseconds);
    }

    private void PressButton() => _viewerActions.PressButton(_data.PlayerDataExtensions.TryStartTime);

    private void AnswerRight() => _viewerActions.SendMessage(Messages.IsRight, "+");

    private void AnswerWrong() => _viewerActions.SendMessage(Messages.IsRight, "-");

    private void FinalStakeTask()
    {
        var myIndex2 = _data.Players.IndexOf((PlayerAccount)_data.Me);
        var sums = _data.Players.Select(p => p.Sum).ToArray();

        try
        {
            var stake = MakeFinalStake(sums, myIndex2, _account.Style);
            _viewerActions.SendMessageWithArgs(Messages.FinalStake, stake);
        }
        catch (Exception exc)
        {
            _data.SystemLog.AppendFormat(
                "Final stake calculation error: {0}\r\nParameter values:\r\n" +
                "sums = {1}. myIndex = {2}. Style = {3}",
                exc,
                string.Join(", ", sums),
                myIndex2,
                _account.Style)
                .AppendLine();
        }
    }

    private void ChooseFinal()
    {
        try
        {
            lock (_data.TInfoLock)
            {
                var choice = _data.TInfo.RoundInfo.SelectRandom(theme => theme.Name != null, Random.Shared);
                _viewerActions.SendMessageWithArgs(Messages.Delete, choice);
            }
        }
        catch (Exception exc)
        {
            _data.SystemLog.AppendFormat("Ошибка при убирании финальной темы. Описание ошибки: {0}", exc).AppendLine();
        }
    }

    private void StakeTaskNew()
    {
        var myIndex = _data.Players.IndexOf((PlayerAccount)_data.Me);
        var isCritical = IsCritical();

        try
        {
            var stakeSum = -1;
            var stakeDecision = StakeModes.Stake;

            switch (_data.PersonDataExtensions.StakeInfo.Reason)
            {
                case StakeReason.HighestPlays:
                    {
                        var stakeMode = MakeStake(
                            _data.QuestionIndex,
                            _data.Players.Select(p => p.Sum).ToArray(),
                            myIndex,
                            _data.LastStakerIndex,
                            _account.Style,
                            _data.PersonDataExtensions.Var,
                            _account.N1,
                            _account.N5,
                            _account.B1,
                            _account.B5,
                            isCritical,
                            _data.PersonDataExtensions.StakeInfo.Minimum,
                            _data.PersonDataExtensions.StakeInfo.Step,
                            out stakeSum);

                        if (stakeMode == StakeMode.Nominal)
                        {
                            stakeMode = StakeMode.Sum;
                            stakeSum = _data.PersonDataExtensions.StakeInfo.Minimum;
                        }

                        stakeDecision = FromStakeMode(stakeMode);
                        break;
                    }

                case StakeReason.Hidden:
                    {
                        var myIndex2 = _data.Players.IndexOf((PlayerAccount)_data.Me);
                        var sums = _data.Players.Select(p => p.Sum).ToArray();
                        stakeSum = MakeFinalStake(sums, myIndex2, _account.Style);
                        break;
                    }

                default:
                    {
                        var stakeInfo = _data.PersonDataExtensions.StakeInfo;
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

            var msg = new MessageBuilder(Messages.SetStake).Add(stakeDecision);

            if (stakeDecision == StakeModes.Stake)
            {
                msg.Add(stakeSum);
            }

            _viewerActions.SendMessage(msg.ToString());
        }
        catch (Exception exc)
        {
            _data.SystemLog.AppendFormat(
                "Stake task error: {0}\r\n" +
                "this.data.choiceQuest = {1}. Sums = {2}. " +
                "MyIndex = {3}.lastStakerNum = {4}. Style = {5} Vars = {6}:{7}:{8}:{9}. " +
                "N1 = {10}, N5 = {11}, B1 = {12}, B5 = {13}, Critical = {14}. MinCost = {15}",
                exc,
                _data.QuestionIndex,
                string.Join(", ", _data.Players.Select(p => p.Sum)),
                myIndex,
                _data.LastStakerIndex,
                _account.Style,
                _data.PersonDataExtensions.Var[0],
                _data.PersonDataExtensions.Var[1],
                _data.PersonDataExtensions.Var[2],
                _data.PersonDataExtensions.Var[3],
                _account.N1,
                _account.N5,
                _account.B1,
                _account.B5,
                isCritical,
                _data.PersonDataExtensions.StakeInfo.Minimum)
                .AppendLine();
        }
    }

    private static StakeModes FromStakeMode(StakeMode stakeMode) =>
        stakeMode switch
        {
            StakeMode.Sum => StakeModes.Stake,
            StakeMode.AllIn => StakeModes.AllIn,
            _ => StakeModes.Pass,
        };

    [Obsolete]
    private void StakeTask()
    {
        var myIndex = _data.Players.IndexOf((PlayerAccount)_data.Me);
        var isCritical = IsCritical();

        try
        {
            int stakeSum = -1;

            var stakeMode = MakeStake(
                _data.QuestionIndex,
                _data.Players.Select(p => p.Sum).ToArray(),
                myIndex,
                _data.LastStakerIndex,
                _account.Style,
                _data.PersonDataExtensions.Var,
                _account.N1,
                _account.N5,
                _account.B1,
                _account.B5,
                isCritical,
                _data.PersonDataExtensions.StakeInfo.Minimum,
                _data.PersonDataExtensions.StakeInfo.Step,
                out stakeSum);

            var msg = new StringBuilder(Messages.Stake).Append(Message.ArgsSeparatorChar).Append((int)stakeMode);

            if (stakeMode == StakeMode.Sum)
            {
                msg.Append(Message.ArgsSeparatorChar).Append(stakeSum);
            }

            _viewerActions.SendMessage(msg.ToString());
        }
        catch (Exception exc)
        {
            _data.SystemLog.AppendFormat(
                "Ошибка при расчёте ставки компьютерного игрока. Описание ошибки: {0}\r\nЗначения параметров.\r\n" +
                "this.data.choiceQuest = {1}. Sums = {2}. " +
                "MyIndex = {3}.lastStakerNum = {4}. Style = {5} Vars = {6}:{7}:{8}:{9}. " +
                "N1 = {10}, N5 = {11}, B1 = {12}, B5 = {13}, Critical = {14}. MinCost = {15}",
                exc,
                _data.QuestionIndex,
                string.Join(", ", _data.Players.Select(p => p.Sum)),
                myIndex,
                _data.LastStakerIndex,
                _account.Style,
                _data.PersonDataExtensions.Var[0],
                _data.PersonDataExtensions.Var[1],
                _data.PersonDataExtensions.Var[2],
                _data.PersonDataExtensions.Var[3],
                _account.N1,
                _account.N5,
                _account.B1,
                _account.B5,
                isCritical,
                _data.PersonDataExtensions.StakeInfo.Minimum)
                .AppendLine();
        }
    }

    private void CatCostTask()
    {
        try
        {
            var stakeInfo = _data.PersonDataExtensions.StakeInfo;
            var optionCount = stakeInfo.Step == 0 ? 2 : (stakeInfo.Maximum - stakeInfo.Minimum) / stakeInfo.Step + 1;
            
            var optionIndex = 0;

            switch (_account.Style)
            {
                case PlayerStyle.Careful:
                    optionIndex = 0;
                    break;

                case PlayerStyle.Normal:
                    optionIndex = Random.Shared.Next(optionCount);
                    break;

                case PlayerStyle.Agressive:
                    optionIndex = optionCount - 1;
                    break;
            }

            var price = stakeInfo.Minimum + optionIndex * stakeInfo.Step;
            _viewerActions.SendMessage(Messages.CatCost, price.ToString());
        }
        catch (Exception exc)
        {
            _data.SystemLog.AppendFormat("Secret question price selection error: {0}", exc).AppendLine();
        }
    }

    private void OnSelectPlayer() => OnSecretQuestionAnswererSelect(Messages.SelectPlayer);

    private void OnSecretQuestionAnswererSelect(string messageType)
    {
        try
        {
            var choice = -1;
            var isCritical = IsCritical();

            if (isCritical || Random.Shared.Next(100) >= _account.V)
            {
                if (isCritical)
                {
                    for (var i = 0; i < _data.Players.Count; i++)
                    {
                        if (_data.Players[i].Name == _viewerActions.Client.Name && _data.Players[i].CanBeSelected)
                        {
                            choice = i;
                            break;
                        }
                    }
                }

                // Вопрос с секретом - сильнейшему
                if (choice == -1)
                {
                    for (var i = 0; i < _data.Players.Count; i++)
                    {
                        if (_data.Players[i].Sum == _data.BigSum(_viewerActions.Client) && _data.Players[i].CanBeSelected)
                        {
                            choice = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                for (var i = 0; i < _data.Players.Count; i++)
                {
                    if (_data.Players[i].Sum == _data.SmallSum(_viewerActions.Client) && _data.Players[i].CanBeSelected)
                    {
                        choice = i;
                        break;
                    }
                }
            }

            _viewerActions.SendMessageWithArgs(messageType, choice);
        }
        catch (Exception exc)
        {
            _data.SystemLog.AppendFormat("Ошибка при выборе Вопроса с секретом. Описание ошибки: {0}", exc).AppendLine();
        }
    }

    private void AnswerTask()
    {
        try
        {
            var ans = new StringBuilder(Messages.Answer)
                .Append(Message.ArgsSeparatorChar)
                .Append(KnowsAnswer ? MessageParams.Answer_Right : MessageParams.Answer_Wrong)
                .Append(Message.ArgsSeparatorChar);

            if (IsSure)
            {
                ans.Append(
                    string.Format(
                        Random.Shared.GetRandomString(_viewerActions.LO[nameof(R.Sure)]),
                        _data.Me.IsMale ? "" : _viewerActions.LO[nameof(R.SureFemaleEnding)]));
            }
            else
            {
                ans.Append(Random.Shared.GetRandomString(_viewerActions.LO[nameof(R.NotSure)]));
            }

            _viewerActions.SendMessage(ans.ToString());
        }
        catch (Exception exc)
        {
            _data.SystemLog.AppendFormat("Ошибка при ответе на вопрос. Описание ошибки: {0}", exc).AppendLine();
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
    /// Рассчитать ставку на Аукционе
    /// </summary>
    /// <param name="questNum">Номер выбранного вопроса (от 0 и выше)</param>
    /// <param name="sums">Суммы на счетах участников</param>
    /// <param name="myIndex">Номер участника</param>
    /// <param name="lastStakerIndex">Индекс последнего ставящего</param>
    /// <param name="style">Стиль участника</param>
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
    /// Checks if situation is critical.
    /// </summary>
    private bool IsCritical()
    {
        int leftQuestionCount;

        lock (_data.TInfoLock)
        {
            leftQuestionCount = _data.TInfo.RoundInfo.Sum(theme => theme.Questions.Count(QuestionHelper.IsActive));
        }

        return (leftQuestionCount <= _account.Nq || GetTimePercentage(0) > 100 - 10 * _account.Nq / 3)
            && _data.MySum() < _account.Part * _data.BigSum(_viewerActions.Client) / 100;
    }

    /// <summary>
    /// Реакция на изменение чьего-то ответа / неответа
    /// </summary>
    public void PersonAnswered(int playerIndex, bool isRight)
    {
        if (_data.Me == null)
        {
            return;
        }

        var playerData = _data.PlayerDataExtensions;

        if (_data.MySum() < -2000) // Отрицательная сумма -> смелость падает
        {
            if (RealBrave >= _account.F + 80)
            {
                RealBrave -= 80;
            }
            else
            {
                RealBrave = _account.F;
            }
        }
        else if (_data.MySum() < 0)
        {
            if (RealBrave >= _account.F + 10)
            {
                RealBrave -= 10;
            }
            else
            {
                RealBrave = _account.F;
            }
        }

        if (isRight)
        {
            EndThink();
        }

        var me = _data.Players[playerIndex].Name == _viewerActions.Client.Name;

        if (me && isRight) // Ответил верно
        {
            switch (_account.Style)
            {
                case PlayerStyle.Agressive:
                    RealBrave += 7;
                    DeltaBrave = 3;
                    break;

                case PlayerStyle.Normal:
                    RealBrave += 5;
                    DeltaBrave = 2;
                    break;

                default:
                    RealBrave += 3;
                    DeltaBrave = 1;
                    break;
            }
        }
        else if (me) // Ответил неверно
        {
            switch (_account.Style)
            {
                case PlayerStyle.Agressive:
                    RealBrave -= 60;
                    DeltaBrave = 3;
                    break;

                case PlayerStyle.Normal:
                    RealBrave -= 80;
                    DeltaBrave = 2;
                    break;

                default:
                    RealBrave -= 100;
                    DeltaBrave = 1;
                    break;
            }
        }
        else if (isRight) // Кто-то другой ответил правильно
        {
            RealBrave += DeltaBrave;

            if (DeltaBrave < 5)
            {
                DeltaBrave++;
            }
        }

        // Критическая ситуация
        if (IsCritical())
        {
            RealBrave += 10;
        }
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
    /// Интеллектуальное вычисление ставок на Аукционе
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

    private void Choose()
    {
        try
        {
            lock (_data.TInfoLock)
            {
                var (themeIndex, questionIndex) = _intelligence.SelectQuestion(
                    _data.TInfo.RoundInfo,
                    (_data.ThemeIndex, _data.QuestionIndex),
                    _data.MySum(),
                    _data.BigSum(_viewerActions.Client),
                    GetTimePercentage(0));
                
                _viewerActions.SendMessageWithArgs(Messages.Choice, themeIndex, questionIndex);
            }
        }
        catch (Exception exc)
        {
            _data.SystemLog.AppendFormat("Question selection error: {0}", exc.ToString()).AppendLine();
        }
    }

    #region PlayerInterface Members

    public void ChooseQuest() => ScheduleExecution(PlayerTasks.Choose, 20 + Random.Shared.Next(10));

    /// <summary>
    /// Получение части вопроса
    /// </summary>
    public void OnQuestionContent()
    {
        var playerData = _data.PlayerDataExtensions;

        if (playerData.IsQuestionInProgress)
        {
            playerData.IsQuestionInProgress = false;
            CalculateAnsweringStrategy(playerData);
        }
    }

    private void CalculateAnsweringStrategy(PlayerData playerData)
    {
        var shortThink = _data.QuestionType == QuestionTypes.Simple;

        // Difficulty: 3 for middle question, 1 and 5 for easiest and hardest questions
        var difficulty = 1.0 + 4.0 * DifficultyHelper.GetDifficulty(_data.QuestionIndex, _themeQuestionCount);
        var playerLag = _account.S * 10;

        var playerStrength = (double)_account.F;

        if (shortThink)
        {
            IsSure = Random.Shared.Next(100) < playerStrength / (difficulty + 1) * 0.75; // 37,5% for F = 200 and difficulty = 3

            var riskRateLimit = RealBrave > 0
                ? (int)(100 * Math.Max(0, Math.Min(1, playerStrength / RealBrave)))
                : 100;

            try
            {
                var riskRate = riskRateLimit < 100 ? 1 - Random.Shared.Next(100 - riskRateLimit) * 0.01 : 1; // Minimizes time to press and guess chances too

                KnowsAnswer = IsSure || Random.Shared.Next(100) < playerStrength * riskRate / (difficulty + 1);
                RealSpeed = Math.Max(1, (int)((playerLag + (int)Random.Shared.NextGaussian(25 - playerStrength / 20 + difficulty * 3, 15)) * riskRate));

                ReadyToPress = IsSure || Random.Shared.Next(100) > 100 - (100 - riskRateLimit) / difficulty;
            }
            catch (ArgumentOutOfRangeException exc)
            {
                throw new Exception($"CalculateAnsweringStrategy: riskRateLimit = {riskRateLimit}, playerStrength = {playerStrength}, playerData.RealBrave = {RealBrave}", exc);
            }
        }
        else
        {
            IsSure = Random.Shared.Next(100) < playerStrength / (difficulty + 1); // 50% for F = 200 and difficulty = 3
            KnowsAnswer = IsSure || Random.Shared.Next(100) < playerStrength / (difficulty + 1) * 0.5;

            RealSpeed = Math.Max(1, playerLag + (int)Random.Shared.NextGaussian(50 - playerStrength / 20, 15)); // 5s average, 4s for strong player
        }
    }

    /// <summary>
    /// Можно нажимать на кнопку
    /// </summary>
    public void StartThink()
    {
        if (!ReadyToPress)
        {
            return;
        }

        ScheduleExecution(PlayerTasks.PressButton, RealSpeed);
        RealSpeed /= 2; // Повторные попытки выполняются быстрее
    }

    /// <summary>
    /// Прекращение размышлений
    /// </summary>
    public void EndThink()
    {
        RealBrave++;
    }

    /// <summary>
    /// Необходимо отвечать
    /// </summary>
    public void Answer() => ScheduleExecution(PlayerTasks.Answer,
        _data.QuestionType == QuestionTypes.Simple ? 10 + Random.Shared.Next(10) : RealSpeed);

    /// <summary>
    /// Необходимо отдать Вопрос с секретом
    /// </summary>
    public void Cat() => ScheduleExecution(PlayerTasks.Cat, 10 + Random.Shared.Next(10));

    public void SelectPlayer() => ScheduleExecution(PlayerTasks.SelectPlayer, 10 + Random.Shared.Next(10));

    /// <summary>
    /// Необходимо сделать ставку
    /// </summary>
    public void Stake() => ScheduleExecution(PlayerTasks.Stake, 10 + Random.Shared.Next(10));

    public void StakeNew() => ScheduleExecution(PlayerTasks.StakeNew, 10 + Random.Shared.Next(10));

    /// <summary>
    /// Необходимо выбрать финальную тему
    /// </summary>
    public void ChooseFinalTheme() => ScheduleExecution(PlayerTasks.ChooseFinal, 10 + Random.Shared.Next(10));

    /// <summary>
    /// Необходимо сделать финальную ставку
    /// </summary>
    public void FinalStake() => ScheduleExecution(PlayerTasks.FinalStake, 10 + Random.Shared.Next(20));

    /// <summary>
    /// Необходимо выбрать стоимость Вопроса с секретом
    /// </summary>
    public void CatCost() => ScheduleExecution(PlayerTasks.CatCost, 15);

    public void IsRight(bool voteForRight) =>
        ScheduleExecution(voteForRight ? PlayerTasks.AnswerRight : PlayerTasks.AnswerWrong, 10 + Random.Shared.Next(10));

    #endregion

    public void Report()
    {
        if (_data.SystemLog.Length > 0)
        {
            _viewerActions.SendMessage(Messages.Report, MessageParams.Report_Log, _data.SystemLog.ToString());
        }
        else
        {
            _viewerActions.SendMessage(Messages.Report, "DECLINE");
        }
    }

    public void OnInitialized()
    {
        RealBrave = _account.B0;
        ScheduleExecution(PlayerTasks.Ready, 10);
    }

    public void OnTheme(string[] mparams)
    {
        if (mparams.Length < 2)
        {
            _themeQuestionCount = DefaultThemeQuestionCount;
            return;
        }

        if (!int.TryParse(mparams[2], out _themeQuestionCount))
        {
            _themeQuestionCount = -1;
        }
    }

    public void OnQuestionSelected() => _themeQuestionCount = _data.TInfo.RoundInfo[_data.ThemeIndex].Questions.Count;

    private void Ready() => _viewerActions.SendMessage(Messages.Ready);
}
