using SIEngine;
using SImulator.ViewModel.Contracts;
using SImulator.ViewModel.Model;
using Utils.Timers;

namespace SImulator.ViewModel.Services;

internal sealed class GameActions : IGameActions, ITaskRunHandler<Tasks>
{
    private readonly GameEngine _engine;
    private readonly TaskRunner<Tasks> _taskRunner;

    public GameActions(GameEngine engine)
    {
        _engine = engine;
        _taskRunner = new TaskRunner<Tasks>(this);
    }

    public void ExecuteTask(Tasks taskId, int arg)
    {
        _taskRunner.ScheduleExecution(Tasks.NoTask, 0, runTimer: false);

        switch (taskId)
        {
            case Tasks.MoveNext:
                _engine.MoveNext();
                break;
        }
    }

    public void MoveNext(int delayMs = 100) => _taskRunner.ScheduleExecution(Tasks.MoveNext, delayMs / 100);

    public void MoveBack()
    {
        _engine.MoveBack();
        _engine.MoveNext();
    }

    public void MoveNextRound() => _engine.MoveNextRound();

    public void MoveBackRound() => _engine.MoveBackRound();

    public void IsRightAnswer()
    {
        _engine.MoveToAnswer();
        _engine.MoveNext();
    }

    public void Dispose()
    {
        _taskRunner.Dispose();
        _engine.Dispose();
    }
}
