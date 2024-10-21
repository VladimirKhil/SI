using System.Windows.Input;

namespace Utils.Commands;

/// <summary>
/// Describes a command that can be executed only in specified context.
/// </summary>
public sealed class ContextCommand : ICommand
{
    private readonly Action<object?> _action;

    /// <summary>
    /// Valid execution context.
    /// </summary>
    public HashSet<object> ExecutionContext { get; } = new();

    public event EventHandler? CanExecuteChanged;

    public ContextCommand(Action<object?> action) => _action = action;

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

    public void Execute(object? parameter) => _action(parameter);
}
