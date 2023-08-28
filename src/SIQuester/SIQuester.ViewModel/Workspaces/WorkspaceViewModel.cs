using Utils.Commands;

namespace SIQuester.ViewModel;

/// <summary>
/// Defines an application workspace view model.
/// </summary>
public abstract class WorkspaceViewModel : ModelViewBase
{
    /// <summary>
    /// Close workspace.
    /// </summary>
    public IAsyncCommand Close { get; private set; }

    /// <summary>
    /// Workspace name.
    /// </summary>
    public abstract string Header { get; }

    private string? _errorMessage = null;

    /// <summary>
    /// Workspace error message.
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage != value)
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Workspace tooltip.
    /// </summary>
    public virtual string? ToolTip { get; } = null;

    /// <summary>
    /// Workspace operation error event.
    /// </summary>
    public event Action<Exception, string?>? Error;

    public event Action<WorkspaceViewModel>? Closed;

    public event Action<WorkspaceViewModel>? NewItem;

    protected WorkspaceViewModel()
    {
        Close = new AsyncCommand(Close_Executed);
    }

    protected virtual Task Close_Executed(object? arg)
    {
        OnClosed();

        return Task.CompletedTask;
    }

    protected internal void OnError(Exception exc, string? message = null) => Error?.Invoke(exc, message);

    protected void OnClosed() => Closed?.Invoke(this);

    protected void OnNewItem(WorkspaceViewModel viewModel) => NewItem?.Invoke(viewModel);

    protected internal virtual ValueTask SaveToTempAsync(CancellationToken cancellationToken = default) => new();

    protected internal virtual ValueTask SaveIfNeededAsync(CancellationToken cancellationToken = default) => new();
}
