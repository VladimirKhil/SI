namespace Utils.Timers;

/// <summary>
/// Handles <see cref="TaskRunner" /> tasks.
/// </summary>
public interface ITaskRunHandler<T>
{
    /// <summary>
    /// Executes a task.
    /// </summary>
    /// <param name="taskId">Task code.</param>
    /// <param name="arg">Task argument.</param>
    void ExecuteTask(T taskId, int arg);
}
