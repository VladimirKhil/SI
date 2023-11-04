namespace Utils.Commands;

/// <inheritdoc cref="IAsyncCommand" />
public sealed class AsyncCommand : IAsyncCommand
{
    private readonly Func<object?, Task> _execute;

    private bool _canBeExecuted = true;

    public event EventHandler? CanExecuteChanged;

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

    public AsyncCommand(Func<object?, Task> execute) => _execute = execute ?? throw new ArgumentNullException(nameof(execute));

    public bool CanExecute(object? parameter) => _canBeExecuted;

    public void Execute(object? parameter) => _execute(parameter);

    public Task ExecuteAsync(object? parameter) => _execute(parameter);
}
