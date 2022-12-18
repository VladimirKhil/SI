using System.Windows.Input;
using Utils;

namespace SIGame.ViewModel;

/// <summary>
/// Describes a command that can be executed only with a fixed set of valid arguments.
/// </summary>
/// <inheritdoc cref="ICommand" />
public sealed class ExtendedCommand : ICommand
{
    private readonly Action<object?> _execute;

    /// <summary>
    /// A set of valid command arguments.
    /// </summary>
    public HashSet<object> ExecutionArea { get; } = new();

    /// <summary>
    /// Raises <see cref="CanExecuteChanged" /> event in a synchronization context-bound thread.
    /// </summary>
    public void OnCanBeExecutedChanged()
    {
        if (CanExecuteChanged != null)
        {
            if (SynchronizationContext.Current == null)
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

    public ExtendedCommand(Action<object?> execute) => _execute = execute ?? throw new ArgumentNullException(nameof(execute));

    public bool CanExecute(object? parameter) => parameter != null && ExecutionArea.Contains(parameter);

    public event EventHandler? CanExecuteChanged;

    public void Execute(object? parameter) => _execute(parameter);
}
