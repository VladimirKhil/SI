using SICore.Contracts;
using SICore.Helpers;
using SICore.Models;
using SICore.Utils;
using SIPackages.Core;
using R = SICore.Properties.Resources;

namespace SICore;

/// <summary>
/// Defines a player computer logic.
/// </summary>
internal sealed class PlayerComputerController
{
    private const int DefaultThemeQuestionCount = 5;

    private readonly IPlayerIntelligence _intelligence;
    private readonly ViewerActions _viewerActions;
    private readonly ViewerData _data;

    private readonly TimerInfo[] _timersInfo;

    private int _themeQuestionCount = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerComputerController"/> class.
    /// </summary>
    public PlayerComputerController(ViewerData data, IPlayerIntelligence intelligence, ViewerActions viewerActions, TimerInfo[] timerInfos)
    {
        _intelligence = intelligence;
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
                var (knows, isSure) = (ValueTuple<bool, bool>)arg!;
                OnAnswer(knows, isSure);
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

    private void OnAnswer(bool knows, bool isSure)
    {
        try
        {
            var ans = new MessageBuilder(Messages.Answer, knows ? MessageParams.Answer_Right : MessageParams.Answer_Wrong);

            // TODO: remove localization
            if (isSure)
            {
                ans.Add(
                    string.Format(
                        Random.Shared.GetRandomString(_viewerActions.LO[nameof(R.Sure)]),
                        _data.Me.IsMale ? "" : _viewerActions.LO[nameof(R.SureFemaleEnding)]));
            }
            else
            {
                ans.Add(Random.Shared.GetRandomString(_viewerActions.LO[nameof(R.NotSure)]));
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
                    MyScore(),
                    BestOpponentScore(),
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

    private void OnPressButton() => _viewerActions.PressButton(_data.TryStartTime);

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
    /// Reacts on some player answer outcome.
    /// </summary>
    public void PersonAnswered(int playerIndex, bool isRight)
    {
        if (_data.Me == null)
        {
            return;
        }

        if (isRight)
        {
            EndThink();
        }

        _intelligence.OnPlayerOutcome(
            _data.Players,
            _data.Players.IndexOf((PlayerAccount)_data.Me),
            playerIndex,
            _data.TInfo.RoundInfo,
            isRight,
            GetTimePercentage(0));
    }

    public void SelectQuestion() => ScheduleExecution(PlayerTasks.SelectQuestion, 20 + Random.Shared.Next(10));

    internal void OnQuestionStart() => _intelligence.OnQuestionStart(
        _data.QuestionType == QuestionTypes.Simple,
        1.0 + 4.0 * DifficultyHelper.GetDifficulty(_data.QuestionIndex, _themeQuestionCount));

    public void StartThink()
    {
        // TODO: switch to async later
        var pressTime = _intelligence.OnStartCanPressButton();

        if (pressTime > -1)
        {
            ScheduleExecution(PlayerTasks.PressButton, pressTime);
        }
    }

    public void EndThink() => _intelligence.OnEndCanPressButton();

    /// <summary>
    /// Answers the question.
    /// </summary>
    public void Answer()
    {
        // TODO: switch to async later
        var (knows, isSure, answerTime) = _intelligence.OnAnswer();

        ScheduleExecution(
            PlayerTasks.Answer,
            _data.QuestionType == QuestionTypes.Simple ? 10 + Random.Shared.Next(10) : answerTime,
            (knows, isSure));
    }

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

    public void OnInitialized() => ScheduleExecution(PlayerTasks.Ready, 10);

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

    /// <summary>
    /// Returns the maximum opponent score.
    /// </summary>
    private int BestOpponentScore() => _data.Players.Where(player => player.Name != _viewerActions.Client.Name).Max(player => player.Sum);

    /// <summary>
    /// Returns the current player score.
    /// </summary>
    private int MyScore() => ((PlayerAccount)_data.Me).Sum;

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
