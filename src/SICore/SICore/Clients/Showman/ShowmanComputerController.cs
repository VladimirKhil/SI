using SICore.Contracts;
using SICore.Extensions;

namespace SICore;

/// <summary>
/// Defines a showman bot controller.
/// </summary>
internal sealed class ShowmanComputerController
{
    private readonly ViewerActions _viewerActions;
    private readonly ViewerData _state;
    private readonly IShowmanIntelligence _intelligence;

    public ShowmanComputerController(ViewerData state, ViewerActions viewerActions, IShowmanIntelligence intelligence)
    {
        _viewerActions = viewerActions;
        _state = state;
        _intelligence = intelligence;
    }

    private async void ScheduleExecution(ShowmanTasks task, double taskTime, object? arg = null)
    {
        await Task.Delay((int)taskTime * 100);

        try
        {
            await _state.TaskLock.WithLockAsync(() => ExecuteTask(task, arg), ViewerData.LockTimeoutMs);
        }
        catch (Exception exc)
        {
            _state.SystemLog.AppendFormat("Execution error: {0}", exc.ToString()).AppendLine();
        }
    }

    private void ExecuteTask(ShowmanTasks task, object? arg)
    {
        switch (task)
        {
            case ShowmanTasks.Ready:
                OnReady();
                break;

            case ShowmanTasks.SelectPlayer:
                OnSelectPlayer();
                break;

            case ShowmanTasks.ValidateAnswer:
                OnValidateAnswer((string?)arg);
                break;

            case ShowmanTasks.ValidateAnswerNew:
                var (answer, voteForRight) = ((string, bool))arg!;
                OnValidateAnswerNew(answer, voteForRight);
                break;

            default:
                break;
        }
    }

    private void OnReady() => _viewerActions.SendMessage(Messages.Ready);

    private void OnSelectPlayer()
    {
        var playerIndex = _state.Players.SelectRandomIndex();
        _viewerActions.SendMessage(Messages.SelectPlayer, playerIndex.ToString());
    }

    private void OnValidateAnswer(string? answer)
    {
        var isRight = ValidateAnswerCore(answer);
        _viewerActions.SendMessage(Messages.IsRight, isRight ? "+" : "-");
    }

    private void OnValidateAnswerNew(string answer, bool voteForRight)
    {
        var isRight = ValidateAnswerCore(answer);
        _viewerActions.ValidateAnswer(answer, isRight);
    }

    private bool ValidateAnswerCore(string? answer)
    {
        if (string.IsNullOrEmpty(answer))
        {
            return false;
        }

        return _intelligence.ValidateAnswer(answer, _state.Right, _state.Wrong);
    }

    public void SelectPlayer() => ScheduleExecution(ShowmanTasks.SelectPlayer, 10 + Random.Shared.Next(10));

    public void IsRight(string answer) => ScheduleExecution(ShowmanTasks.ValidateAnswer, 10 + Random.Shared.Next(10), answer);

    public void OnAskValidateAnswer(string answer, bool voteForRight) => ScheduleExecution(ShowmanTasks.ValidateAnswerNew, 10 + Random.Shared.Next(10), (answer, voteForRight));

    public void OnInitialized() => ScheduleExecution(ShowmanTasks.Ready, 10);

    private enum ShowmanTasks
    {
        Ready, // Internal action
        ValidateAnswer,
        ValidateAnswerNew,
        SelectPlayer,
    }
}
