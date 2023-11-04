using System.Windows.Input;

namespace Utils.Commands;

/// <summary>
/// Describes an asynchronous command that can be executed only in specified context.
/// </summary>
public sealed class AsyncContextCommand : ICommand
{
    private readonly Func<object?, Task> _execute;

    /// <summary>
    /// Valid execution context.
    /// </summary>
    public HashSet<object> ExecutionContext { get; } = new();

    public event EventHandler? CanExecuteChanged;

    public AsyncContextCommand(Func<object?, Task> execute) => _execute = execute ?? throw new ArgumentNullException(nameof(execute));

    /// <summary>
    /// Raises <see cref="CanExecuteChanged" /> event in a synchronization context-bound thread.
    /// </summary>
    public void OnCanBeExecutedChanged()
    {
        if (CanExecuteChanged != null)
        {
            if (SynchronizationContext.Current == null && UI.Scheduler != null)
            {
                Task.Factory.StartNew(
                    () => CanExecuteChanged(this, EventArgs.Empty),
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    UI.Scheduler);
            }
            else
            {
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }
    }

    public bool CanExecute(object? parameter) => parameter != null && ExecutionContext.Contains(parameter);

    public void Execute(object? parameter) => _execute(parameter);

    public Task ExecuteAsync(object? parameter) => _execute(parameter);
}
