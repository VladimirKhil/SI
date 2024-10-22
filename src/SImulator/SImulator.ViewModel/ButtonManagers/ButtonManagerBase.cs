using SImulator.ViewModel.Contracts;

namespace SImulator.ViewModel.ButtonManagers;

/// <inheritdoc cref="IButtonManager" />
public abstract class ButtonManagerBase : IButtonManager
{
    protected IButtonManagerListener Listener { get; }

    public ButtonManagerBase(IButtonManagerListener buttonManagerListener) => Listener = buttonManagerListener;

    public virtual bool ArePlayersManaged() => false;

    public virtual void RemovePlayerById(string id, string name) { }

    public abstract bool Start();

    public abstract void Stop();

    public virtual ICommandExecutor? TryGetCommandExecutor() => null;

    public virtual ValueTask DisposeAsync() => new();
}
