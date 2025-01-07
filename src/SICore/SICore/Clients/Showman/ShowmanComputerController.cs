using SICore.Contracts;
using SICore.Extensions;

namespace SICore;

/// <summary>
/// Defines a showman bot controller.
/// </summary>
internal sealed class ShowmanComputerController
{
    private readonly ViewerActions _viewerActions;
    private readonly ViewerData _data;
    private readonly IShowmanIntelligence _intelligence;

    public ShowmanComputerController(ViewerData data, ViewerActions viewerActions, IShowmanIntelligence intelligence)
    {
        _viewerActions = viewerActions;
        _data = data;
        _intelligence = intelligence;
    }

    private async void ScheduleExecution(ShowmanTasks task, double taskTime, object? arg = null)
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

            default:
                break;
        }
    }

    private void OnReady() => _viewerActions.SendMessage(Messages.Ready);

    private void OnSelectPlayer()
    {
        var playerIndex = _data.Players.SelectRandomIndex();
        _viewerActions.SendMessage(Messages.SelectPlayer, playerIndex.ToString());
    }

    private void OnValidateAnswer(string? answer)
    {
        var isRight = ValidateAnswerCore(answer);
        _viewerActions.SendMessage(Messages.IsRight, isRight ? "+" : "-");
    }

    private bool ValidateAnswerCore(string? answer)
    {
        if (string.IsNullOrEmpty(answer))
        {
            return false;
        }

        return _intelligence.ValidateAnswer(answer, _data.PersonDataExtensions.Right, _data.PersonDataExtensions.Wrong);
    }

    public void SelectPlayer() => ScheduleExecution(ShowmanTasks.SelectPlayer, 10 + Random.Shared.Next(10));

    public void IsRight(string answer) => ScheduleExecution(ShowmanTasks.ValidateAnswer, 10 + Random.Shared.Next(10), answer);

    public void OnInitialized() => ScheduleExecution(ShowmanTasks.Ready, 10);

    private enum ShowmanTasks
    {
        Ready, // Internal action
        ValidateAnswer,
        SelectPlayer,
    }
}
