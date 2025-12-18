using Utils.Timers;

namespace SIGame.ViewModel.Tests.Mocks;

/// <summary>
/// Test implementation of IAnimatableTimer.
/// </summary>
internal sealed class TestAnimatableTimer : IAnimatableTimer
{
    public int MaxTime { get; set; } = 100;

    public double Time { get; private set; }

    public TimerState State { get; private set; } = TimerState.Stopped;

    public bool KeepFinalValue { get; set; }

    public event Action<IAnimatableTimer>? TimeChanged;

    public void Pause(int currentTime, bool byUser)
    {
        State = TimerState.Paused;
        Time = currentTime;
    }

    public void Run(int maxTime, bool byUser, double? fromValue = null)
    {
        MaxTime = maxTime;
        State = TimerState.Running;
        Time = fromValue ?? 0;
    }

    public void Stop()
    {
        State = TimerState.Stopped;
        Time = 0;
    }

    public void Dispose() { }
}
