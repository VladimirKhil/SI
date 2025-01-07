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
internal sealed class PlayerComputerLogic
{
    private const int DefaultThemeQuestionCount = 5;

    private readonly IPlayerIntelligence _intelligence;
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
    public PlayerComputerLogic(ViewerData data, ComputerAccount account, IPlayerIntelligence intelligence, ViewerActions viewerActions, TimerInfo[] timerInfos)
    {
        _intelligence = intelligence;
        _account = account;
        _viewerActions = viewerActions;
        _data = data;
        _timersInfo = timerInfos;
    }

    // TODO: Switch to TaskRunner and support task cancellation
    private async void ScheduleExecution(PlayerTasks task, double taskTime, object? arg = null)
    {
        await Task.Delay((int)taskTime * 100);

        try
        {
            ExecuteTask(task, arg);
        }
        catch (Exception exc)
        {
            _data.SystemLog.AppendFormat("Execution error: {0}", exc.ToString()).AppendLine();
        }
    }

    private void ExecuteTask(PlayerTasks task, object? arg)
    {
        switch (task)
        {
            case PlayerTasks.Ready:
                OnReady();
                break;

            case PlayerTasks.Answer:
                OnAnswer();
                break;

            case PlayerTasks.SelectQuestion:
                OnSelectQuestion();
                break;

            case PlayerTasks.ValidateAnswer:
                OnValidateAnswer((bool?)arg);
                break;

            case PlayerTasks.SelectPlayer:
                OnSelectPlayer();
                break;

            case PlayerTasks.DeleteTheme:
                OnDeleteTheme();
                break;

            case PlayerTasks.MakeStake:
                OnMakeStake();
                break;

            case PlayerTasks.PressButton:
                OnPressButton();
                break;

            default:
                break;
        }
    }

    private void OnReady() => _viewerActions.SendMessage(Messages.Ready);

    private void OnAnswer()
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
            _data.SystemLog.AppendFormat("Answering error: {0}", exc).AppendLine();
        }
    }

    private void OnSelectQuestion()
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

    private void OnValidateAnswer(bool? voteForRight) => _viewerActions.SendMessage(Messages.IsRight, voteForRight == true ? "+" : "-");

    private void OnSelectPlayer()
    {
        try
        {
            var playerIndex = _intelligence.SelectPlayer(
                _data.Players,
                _data.Players.IndexOf((PlayerAccount)_data.Me),
                _data.TInfo.RoundInfo,
                GetTimePercentage(0));

            _viewerActions.SendMessageWithArgs(Messages.SelectPlayer, playerIndex);
        }
        catch (Exception exc)
        {
            _data.SystemLog.AppendFormat("Select player error: {0}", exc).AppendLine();
        }
    }

    private void OnDeleteTheme()
    {
        try
        {
            lock (_data.TInfoLock)
            {
                var themeIndex = _intelligence.DeleteTheme(_data.TInfo.RoundInfo);
                _viewerActions.SendMessageWithArgs(Messages.Delete, themeIndex);
            }
        }
        catch (Exception exc)
        {
            _data.SystemLog.AppendFormat("Theme delete error: {0}", exc).AppendLine();
        }
    }

    private void OnMakeStake()
    {
        try
        {
            var (stakeDecision, stakeSum) = _intelligence.MakeStake(
                _data.Players,
                _data.Players.IndexOf((PlayerAccount)_data.Me),
                _data.TInfo.RoundInfo,
                _data.PersonDataExtensions.StakeInfo,
                _data.QuestionIndex,
                _data.LastStakerIndex,
                _data.PersonDataExtensions.Var,
                GetTimePercentage(0));

            var msg = new MessageBuilder(Messages.SetStake).Add(stakeDecision);

            if (stakeDecision == StakeModes.Stake)
            {
                msg.Add(stakeSum);
            }

            _viewerActions.SendMessage(msg.ToString());
        }
        catch (Exception exc)
        {
            _data.SystemLog.AppendFormat("Stake task error: {0}", exc).AppendLine();
        }
    }

    private void OnPressButton() => _viewerActions.PressButton(_data.PlayerDataExtensions.TryStartTime);

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

    public void SelectQuestion() => ScheduleExecution(PlayerTasks.SelectQuestion, 20 + Random.Shared.Next(10));

    /// <summary>
    /// Получение части вопроса
    /// </summary>
    public void OnQuestionContent()
    {
        var playerData = _data.PlayerDataExtensions;

        if (playerData.IsQuestionInProgress)
        {
            playerData.IsQuestionInProgress = false;
            CalculateAnsweringStrategy();
        }
    }

    private void CalculateAnsweringStrategy()
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
    /// Answers the question.
    /// </summary>
    public void Answer() => ScheduleExecution(
        PlayerTasks.Answer,
        _data.QuestionType == QuestionTypes.Simple ? 10 + Random.Shared.Next(10) : RealSpeed);

    public void SelectPlayer() => ScheduleExecution(PlayerTasks.SelectPlayer, 10 + Random.Shared.Next(10));

    public void MakeStake() => ScheduleExecution(PlayerTasks.MakeStake, 10 + Random.Shared.Next(10));

    /// <summary>
    /// Deletes round theme.
    /// </summary>
    public void DeleteTheme() => ScheduleExecution(PlayerTasks.DeleteTheme, 10 + Random.Shared.Next(10));

    public void ValidateAnswer(bool voteForRight) => ScheduleExecution(PlayerTasks.ValidateAnswer, 10 + Random.Shared.Next(10), voteForRight);

    public void SendReport()
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

    private enum PlayerTasks
    {
        Ready, // Internal action
        Answer,
        SelectQuestion,
        MakeStake,
        DeleteTheme,
        ValidateAnswer,
        PressButton, // Internal action
        SelectPlayer,
    }
}
