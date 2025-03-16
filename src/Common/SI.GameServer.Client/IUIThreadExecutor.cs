namespace SI.GameServer.Client;

/// <summary>
/// Allows to execute an action in the UI thread.
/// </summary>
public interface IUIThreadExecutor
{
    void ExecuteOnUIThread(Action action);
}
