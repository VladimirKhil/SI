namespace Utils.Timers;

/// <summary>
/// Defines a game timer.
/// </summary>
public interface IAnimatableTimer : IDisposable
{
    int MaxTime { get; set; }

    double Time { get; }

    bool KeepFinalValue { get; set; }

    TimerState State { get; }

    event Action<IAnimatableTimer> TimeChanged;

    void Run(int maxTime, bool byUser, double? fromValue = null);

    void Stop();

    void Pause(int currentTime, bool byUser);
}
