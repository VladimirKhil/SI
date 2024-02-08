namespace SICore.Clients.Game;

/// <summary>
/// Handles <see cref="TaskRunner" /> tasks.
/// </summary>
internal interface ITaskRunHandler<T>
{
    /// <summary>
    /// Executes a task.
    /// </summary>
    /// <param name="taskId">Task code.</param>
    /// <param name="arg">Task argument.</param>
    void ExecuteTask(T taskId, int arg);
}
