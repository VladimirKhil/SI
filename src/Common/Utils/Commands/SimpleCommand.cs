using System.Windows.Input;

namespace Utils.Commands;

/// <summary>
/// Represents a simple command.
/// </summary>
public class SimpleCommand : ICommand
{
    private bool _canBeExecuted = true;

    /// <summary>
    /// Can the command be executed now.
    /// </summary>
    public bool CanBeExecuted
    {
        get => _canBeExecuted;
        set
        {
            if (_canBeExecuted != value)
            {
                _canBeExecuted = value;

                if (CanExecuteChanged != null)
                {
                    if (SynchronizationContext.Current == null && UI.Scheduler != null)
                    {
                        Task.Factory.StartNew(
                            () => CanExecuteChanged?.Invoke(this, EventArgs.Empty),
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
        }
    }

    private readonly Action<object?> _action;

    public bool CanExecute(object? parameter) => _canBeExecuted;

    public event EventHandler? CanExecuteChanged;

    public void Execute(object? parameter) => _action?.Invoke(parameter);

    public SimpleCommand(Action<object?> action) => _action = action;
}
