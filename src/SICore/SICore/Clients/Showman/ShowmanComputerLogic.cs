using SICore.Clients.Showman;
using SIData;

namespace SICore;

/// <summary>
/// Логика ведущего-компьютера
/// </summary>
internal sealed class ShowmanComputerLogic : IShowmanLogic
{
    private readonly ViewerActions _viewerActions;
    private readonly ViewerData _data;

    public ShowmanComputerLogic(ViewerData data, ViewerActions viewerActions, ComputerAccount computerAccount)
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

            case ShowmanTasks.AnswerFirst:
                AnswerFirst();
                break;

            case ShowmanTasks.AnswerNextStake:
                AnswerNextStake();
                break;

            case ShowmanTasks.AnswerRight:
                AnswerRight((string?)arg);
                break;

            case ShowmanTasks.AnswerNextToDelete:
                AnswerNextToDelete();
                break;

            default:
                break;
        }
    }

    private void Ready() => ((PersonAccount)_data.Me).BeReadyCommand.Execute(null);

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

    private void AnswerNextToDelete() => SelectPlayer(Messages.NextDelete);

    private void AnswerNextStake() => SelectPlayer(Messages.Next);

    private void AnswerFirst() => SelectPlayer(Messages.First);

    private void OnSelectPlayer() => SelectPlayer(Messages.SelectPlayer);

    private void AnswerRight(string? answer)
    {
        var isRight = AnswerChecker.IsAnswerRight(answer ?? "", _data.PersonDataExtensions.Right);
        _viewerActions.SendMessage(Messages.IsRight, isRight ? "+" : "-");
    }

    public void SelectPlayer() => ScheduleExecution(ShowmanTasks.SelectPlayer, 10 + Random.Shared.Next(10));

    public void StarterChoose() => ScheduleExecution(ShowmanTasks.AnswerFirst, 10 + Random.Shared.Next(10));

    public void FirstStake() => ScheduleExecution(ShowmanTasks.AnswerNextStake, 10 + Random.Shared.Next(10));

    public void IsRight(string answer) => ScheduleExecution(ShowmanTasks.AnswerRight, 10 + Random.Shared.Next(10));

    public void FirstDelete() => ScheduleExecution(ShowmanTasks.AnswerNextToDelete, 10 + Random.Shared.Next(10));

    public void OnInitialized() => ScheduleExecution(ShowmanTasks.Ready, 10);


    public void ChooseQuest()
    {
        
    }

    public void Cat()
    {
        
    }

    public void Stake()
    {
        
    }

    public void ChooseFinalTheme()
    {
        
    }

    public void FinalStake()
    {
        
    }

    public void CatCost()
    {
        
    }
}
