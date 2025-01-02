using SICore.Clients.Showman;

namespace SICore;

/// <summary>
/// Defines a showman bot logic.
/// </summary>
internal sealed class ShowmanComputerLogic
{
    private readonly ViewerActions _viewerActions;
    private readonly ViewerData _data;

    public ShowmanComputerLogic(ViewerData data, ViewerActions viewerActions)
    {
        _viewerActions = viewerActions;
        _data = data;
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
                Ready();
                break;

            case ShowmanTasks.SelectPlayer:
                OnSelectPlayer();
                break;

            case ShowmanTasks.AnswerRight:
                AnswerRight((string?)arg);
                break;

            default:
                break;
        }
    }

    private void Ready() => _viewerActions.SendMessage(Messages.Ready);

    private void SelectPlayer(string message)
    {
        int num = _data.Players.Count(p => p.CanBeSelected);
        int i = Random.Shared.Next(num);
        
        while (i < _data.Players.Count && !_data.Players[i].CanBeSelected)
        {
            i++;
        }

        _viewerActions.SendMessage(message, i.ToString());
    }

    private void OnSelectPlayer() => SelectPlayer(Messages.SelectPlayer);

    private void AnswerRight(string? answer)
    {
        var isRight = AnswerChecker.IsAnswerRight(answer ?? "", _data.PersonDataExtensions.Right);
        _viewerActions.SendMessage(Messages.IsRight, isRight ? "+" : "-");
    }

    public void SelectPlayer() => ScheduleExecution(ShowmanTasks.SelectPlayer, 10 + Random.Shared.Next(10));

    public void IsRight(string answer) => ScheduleExecution(ShowmanTasks.AnswerRight, 10 + Random.Shared.Next(10), answer);

    public void OnInitialized() => ScheduleExecution(ShowmanTasks.Ready, 10);
}
