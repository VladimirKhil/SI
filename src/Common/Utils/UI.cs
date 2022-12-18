namespace Utils;

/// <summary>
/// Provides helper method to execute the code in the UI thread.
/// </summary>
public static class UI
{
    public static TaskScheduler? Scheduler { get; private set; } // TODO: -> private

    public static void Initialize()
    {
        Scheduler = TaskScheduler.FromCurrentSynchronizationContext();
    }

    public static void Execute(Action action, Action<Exception> onError, CancellationToken cancellationToken = default)
    {
        void wrappedAction()
        {
            try
            {
                action();
            }
            catch (Exception exc)
            {
                onError(exc);
            }
        }

        if (TaskScheduler.Current != Scheduler && Scheduler != null)
        {
            Task.Factory.StartNew(wrappedAction, cancellationToken, TaskCreationOptions.DenyChildAttach, Scheduler);
            return;
        }

        wrappedAction();
    }
}
