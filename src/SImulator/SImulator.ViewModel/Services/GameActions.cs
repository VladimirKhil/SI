using SIEngine;
using SImulator.ViewModel.Contracts;
using SImulator.ViewModel.Model;
using Utils.Timers;

namespace SImulator.ViewModel.Services;

internal sealed class GameActions : IGameActions, ITaskRunHandler<Tasks>
{
    private readonly GameEngine _engine;
    private readonly IPresentationController _presentationController;
    private readonly TaskRunner<Tasks> _taskRunner;

    private string[] _themeNames = Array.Empty<string>();

    public GameActions(GameEngine engine, IPresentationController presentationController)
    {
        _engine = engine;
        _presentationController = presentationController;
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

            case Tasks.ShowTheme:
                if (arg < 0 || arg >= _themeNames.Length)
                {
                    break;
                }

                _presentationController.SetTheme(_themeNames[arg], true);
                _taskRunner.ScheduleExecution(Tasks.ShowTheme, 20, arg + 1);
                break;
        }
    }

    public void MoveNext(int delayMs = 100) => _taskRunner.ScheduleExecution(Tasks.MoveNext, delayMs / 100);

    public void ShowThemes(string[] themeNames)
    {
        _themeNames = themeNames;
        _taskRunner.ScheduleExecution(Tasks.ShowTheme, 1, 0);
    }

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
