namespace Utils.Timers;

/// <summary>
/// Runs tasks with specified intervals.
/// </summary>
/// <typeparam name="T">Task type.</typeparam>
public sealed class TaskRunner<T> : IDisposable where T : struct
{
    /// <summary>
    /// Maximum time to schedule a task, 0.1 s.
    /// </summary>
    private const int MaximumWaitTime = 1200;

    private readonly ITaskRunHandler<T> _taskRunHandler;

    private readonly Timer _taskTimer;

    private int _taskArgument = -1;

    private readonly Lock _taskTimerLock = new(nameof(_taskTimerLock));

    /// <summary>
    /// Estimated time for current task to fire.
    /// </summary>
    private DateTime _finishingTime;

    private bool _disposed;

    private readonly Stack<Tuple<T, int, int>> _oldTasks = new();

    public IEnumerable<Tuple<T, int, int>> OldTasks => _oldTasks;

    public T CurrentTask { get; private set; }

    public bool IsExecutionPaused => _oldTasks.Any() && EqualityComparer<T>.Default.Equals(CurrentTask, default);

    public T PendingTask => IsExecutionPaused ? _oldTasks.Peek().Item1 : CurrentTask;

    public bool IsRunning { get; set; }

    public TaskRunner(ITaskRunHandler<T> taskRunHandler)
    {
        _taskTimer = new Timer(TaskTimer_Elapsed, null, Timeout.Infinite, Timeout.Infinite);
        _taskRunHandler = taskRunHandler;
    }

    public string PrintOldTasks() => string.Join("|", OldTasks.Select(t => $"{t.Item1}:{t.Item2}"));

    internal void SetTask(T task, int taskArgument)
    {
        CurrentTask = task;
        _taskArgument = taskArgument;
    }

    public void ExecuteImmediate() =>
        _taskTimerLock.WithLock(() =>
        {
            if (!_disposed)
            {
                _taskTimer.Change(10, Timeout.Infinite);
            }
        });

    public void ScheduleExecution(T task, double taskTime, int taskArgument = 0, bool runTimer = true)
    {
        SetTask(task, taskArgument);

        if (!runTimer)
        {
            IsRunning = false;
            return;
        }

        IsRunning = true;
        RunTaskTimer(Math.Min(MaximumWaitTime, taskTime));
    }

    public void PauseExecution(T task, int taskArgument)
    {
        var now = DateTime.UtcNow;

        // Saving running task, its argument and left time
        var leftTime = (int)((_finishingTime - now).TotalMilliseconds / 100);
        _oldTasks.Push(Tuple.Create(task, taskArgument, leftTime));

        CurrentTask = default;
    }

    public int ResumeExecution(int resumeTime = 0, bool runTimer = true)
    {
        if (!_oldTasks.Any())
        {
            throw new Exception("Cannot resume execution: no saved task");
        }

        var oldTask = _oldTasks.Pop();
        var taskTime = resumeTime > 0 ? resumeTime : Math.Max(1, oldTask.Item3);

        ScheduleExecution(oldTask.Item1, taskTime, oldTask.Item2, runTimer);

        return taskTime;
    }

    public void UpdatePausedTask(T task, int taskArgument, int taskTime)
    {
        if (_oldTasks.Any())
        {
            _oldTasks.Pop();
        }

        _oldTasks.Push(Tuple.Create(task, taskArgument, taskTime));
    }

    public void ClearOldTasks() => _oldTasks.Clear();

    private void RunTaskTimer(double taskTime)
    {
        if (_disposed || taskTime <= 0)
        {
            return;
        }

        _taskTimerLock.WithLock(() =>
        {
            if (_disposed)
            {
                return;
            }

            _taskTimer.Change((int)taskTime * 100, Timeout.Infinite);
            _finishingTime = DateTime.UtcNow + TimeSpan.FromMilliseconds(taskTime * 100);
        });
    }

    private void TaskTimer_Elapsed(object? state)
    {
        if (EqualityComparer<T>.Default.Equals(CurrentTask, default))
        {
            return;
        }

        try
        {
            _taskRunHandler.ExecuteTask(CurrentTask, _taskArgument);
        }
        catch (ObjectDisposedException)
        {
            // Do nothing
        }
    }

    public void Dispose()
    {
        _taskTimerLock.WithLock(
             () =>
             {
                 if (_disposed)
                 {
                     return;
                 }

                 _taskTimer.Dispose();
                 _disposed = true;
             },
             5000);

        _taskTimerLock.Dispose();
    }
}
