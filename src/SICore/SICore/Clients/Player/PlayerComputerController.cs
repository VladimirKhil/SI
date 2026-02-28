using SICore.Clients.Game;
using SICore.Contracts;
using SICore.Helpers;
using SICore.Models;
using SICore.Utils;
using SIPackages.Core;
using Utils.Timers;

namespace SICore;

/// <summary>
/// Defines a player computer logic.
/// </summary>
internal sealed class PlayerComputerController : ITaskRunHandler<PlayerComputerController.PlayerTasks>
{
    private const int DefaultThemeQuestionCount = 5;

    private readonly IPlayerIntelligence _intelligence;
    private readonly ViewerActions _viewerActions;
    private readonly ViewerData _state;

    private readonly TimerInfo[] _timersInfo;

    private int _themeQuestionCount = -1;

    private readonly TaskRunner<PlayerTasks> _taskRunner;

    private object? _taskArg; // Currently TaskRunner does not support object task arguments

    private readonly HistoryLog _historyLog = new();

    private int _lastStakerIndex = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerComputerController"/> class.
    /// </summary>
    public PlayerComputerController(ViewerData state, IPlayerIntelligence intelligence, ViewerActions viewerActions, TimerInfo[] timerInfos)
    {
        _intelligence = intelligence;
        _viewerActions = viewerActions;
        _state = state;
        _timersInfo = timerInfos;
        _taskRunner = new TaskRunner<PlayerTasks>(this);
    }

    private void ScheduleExecution(PlayerTasks task, double taskTime, object? arg = null)
    {
        _historyLog.AddLogEntry($"Scheduled {task}:{arg}");
        _taskArg = arg;
        _taskRunner.ScheduleExecution(task, taskTime);
    }

    public void AddLog(string message) => _historyLog.AddLogEntry(message);

    public void ExecuteTask(PlayerTasks taskId, int arg)
    {
        try
        {
            _state.TaskLock.WithLock(() => ExecuteTaskCore(taskId, _taskArg), ViewerData.LockTimeoutMs);
        }
        catch (Exception exc)
        {
            _state.SystemLog.AppendFormat("Execution error: {0}", exc.ToString()).AppendLine();
        }
    }

    private void ExecuteTaskCore(PlayerTasks task, object? arg)
    {
        _historyLog.AddLogEntry($"Executing {task}:{arg}");

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

            case PlayerTasks.ValidateAnswerNew:
                var (answer, voteForRight) = (ValueTuple<string, bool>)arg!;
                OnValidateAnswerNew(answer, voteForRight);
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
        var me = (PlayerAccount?)_state.Me;

        if (me == null)
        {
            return;
        }

        try
        {
            var ans = new MessageBuilder(Messages.Answer, knows ? MessageParams.Answer_Right : MessageParams.Answer_Wrong, isSure ? '+' : '-');
            _viewerActions.SendMessage(ans.ToString());
        }
        catch (Exception exc)
        {
            _state.SystemLog.AppendFormat("Answering error: {0} {1}", exc, _historyLog).AppendLine();
        }
    }

    private void OnSelectQuestion()
    {
        var me = (PlayerAccount?)_state.Me;

        if (me == null)
        {
            return;
        }

        try
        {
            var (themeIndex, questionIndex) = _intelligence.SelectQuestion(
                _state.TInfo.RoundInfo,
                (_state.ThemeIndex, _state.QuestionIndex),
                me.Sum,
                BestOpponentScore(),
                GetTimePercentage(0));

            _viewerActions.SendMessageWithArgs(Messages.Choice, themeIndex, questionIndex);
        }
        catch (Exception exc)
        {
            _state.SystemLog.AppendFormat("Question selection error: {0} {1}", exc.ToString(), _historyLog).AppendLine();
        }
    }

    // As player is validating only during appellations, computer player always agrees with appellation
    private void OnValidateAnswer(bool? voteForRight) => _viewerActions.IsRight(voteForRight == true);

    // As player is validating only during appellations, computer player always agrees with appellation
    private void OnValidateAnswerNew(string answer, bool voteForRight) => _viewerActions.ValidateAnswer(answer, voteForRight);

    private void OnSelectPlayer()
    {
        var me = (PlayerAccount?)_state.Me;

        if (me == null)
        {
            return;
        }

        var myIndex = _state.Players.IndexOf(me);

        if (myIndex == -1)
        {
            return;
        }

        try
        {
            var playerIndex = _intelligence.SelectPlayer(
                _state.Players,
                myIndex,
                _state.TInfo.RoundInfo,
                GetTimePercentage(0));

            _viewerActions.SelectPlayer(playerIndex);
        }
        catch (Exception exc)
        {
            _state.SystemLog.AppendFormat("Select player error: {0} {1}", exc, _historyLog).AppendLine();
        }
    }

    private void OnDeleteTheme()
    {
        try
        {
            var themeIndex = _intelligence.DeleteTheme(_state.TInfo.RoundInfo);
            _viewerActions.SendMessageWithArgs(Messages.Delete, themeIndex);
        }
        catch (Exception exc)
        {
            _state.SystemLog.AppendFormat("Theme delete error: {0} {1}", exc, _historyLog).AppendLine();
        }
    }

    private void OnMakeStake()
    {
        var me = (PlayerAccount?)_state.Me;

        if (me == null || _state.StakeInfo == null)
        {
            return;
        }

        var myIndex = _state.Players.IndexOf(me);

        if (myIndex == -1)
        {
            return;
        }

        try
        {
            var (stakeDecision, stakeSum) = _intelligence.MakeStake(
                _state.Players,
                myIndex,
                _state.TInfo.RoundInfo,
                _state.StakeInfo,
                _state.QuestionIndex,
                _lastStakerIndex,
                _state.StakeInfo.Modes,
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
            _state.SystemLog.AppendFormat("Stake task error: {0} {1}", exc, _historyLog).AppendLine();
        }
    }

    private void OnPressButton() => _viewerActions.PressButton(_state.TryStartTime);

    public void CancelTask() => _taskRunner.ScheduleExecution(PlayerTasks.None, 0, runTimer: false);

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
        if (_state.Me == null)
        {
            return;
        }

        if (isRight)
        {
            EndThink();
        }

        _intelligence.OnPlayerOutcome(
            _state.Players,
            _state.Players.IndexOf((PlayerAccount)_state.Me),
            playerIndex,
            _state.TInfo.RoundInfo,
            isRight,
            GetTimePercentage(0));
    }

    public void SelectQuestion() => ScheduleExecution(PlayerTasks.SelectQuestion, 20 + Random.Shared.Next(10));

    internal void OnQuestionStart() => _intelligence.OnQuestionStart(
        _state.QuestionType == QuestionTypes.Simple,
        1.0 + 4.0 * DifficultyHelper.GetDifficulty(_state.QuestionIndex, _themeQuestionCount));

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
            _state.QuestionType == QuestionTypes.Simple ? 10 + Random.Shared.Next(10) : answerTime,
            (knows, isSure));
    }

    public void SelectPlayer() => ScheduleExecution(PlayerTasks.SelectPlayer, 10 + Random.Shared.Next(10));

    public void MakeStake() => ScheduleExecution(PlayerTasks.MakeStake, 10 + Random.Shared.Next(10));

    /// <summary>
    /// Deletes round theme.
    /// </summary>
    public void DeleteTheme() => ScheduleExecution(PlayerTasks.DeleteTheme, 10 + Random.Shared.Next(10));

    public void ValidateAnswer(bool voteForRight) => ScheduleExecution(PlayerTasks.ValidateAnswer, 10 + Random.Shared.Next(10), voteForRight);

    public void OnAskValidateAnswer(string answer, bool voteForRight) => ScheduleExecution(PlayerTasks.ValidateAnswerNew, 10 + Random.Shared.Next(10), (answer, voteForRight));

    public void OnInitialized() => ScheduleExecution(PlayerTasks.Ready, 10);

    public void OnTheme(int questionCount)
    {
        if (questionCount == -1)
        {
            _themeQuestionCount = DefaultThemeQuestionCount;
            return;
        }

        _themeQuestionCount = questionCount;
    }

    public void OnQuestionSelected() => _themeQuestionCount = _state.TInfo.RoundInfo[_state.ThemeIndex].Questions.Count;

    /// <summary>
    /// Returns the maximum opponent score.
    /// </summary>
    private int BestOpponentScore() => _state.Players.Where(player => player.Name != _viewerActions.Client.Name).Max(player => player.Sum);

    internal void OnPersonStake(int stakerIndex) => _lastStakerIndex = stakerIndex;

    internal void OnPersonsUpdated()
    {
        if (_lastStakerIndex >= _state.Players.Count)
        {
            _lastStakerIndex = -1; // Reset if index is out of bounds
        }
    }

    internal enum PlayerTasks
    {
        None,
        Ready, // Internal action
        Answer,
        SelectQuestion,
        MakeStake,
        DeleteTheme,
        ValidateAnswer,
        ValidateAnswerNew,
        PressButton, // Internal action
        SelectPlayer,
    }
}
