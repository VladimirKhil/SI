namespace SImulator.ViewModel.ButtonManagers;

/// <inheritdoc cref="IButtonManager" />
public abstract class ButtonManagerBase : IButtonManager
{
    protected IButtonManagerListener Listener { get; }

    public ButtonManagerBase(IButtonManagerListener buttonManagerListener) => Listener = buttonManagerListener;

    public abstract bool Start();

    public abstract void Stop();

    public virtual ValueTask DisposeAsync() => new();
}
