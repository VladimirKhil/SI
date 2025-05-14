using Utils.Timers;

namespace SImulator.ViewModel.Tests;

internal sealed class TestAnimatableTimer : IAnimatableTimer
{
    public int MaxTime { get; set; }

    public double Time => throw new NotImplementedException();

    public TimerState State => throw new NotImplementedException();

    public bool KeepFinalValue { get; set; }

    public event Action<IAnimatableTimer>? TimeChanged;

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void Pause(int currentTime, bool byUser)
    {
        throw new NotImplementedException();
    }

    public void Run(int maxTime, bool byUser, double? fromValue = null) { }

    public void Stop()
    {
        throw new NotImplementedException();
    }
}
