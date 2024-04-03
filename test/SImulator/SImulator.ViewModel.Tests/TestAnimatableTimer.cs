using Utils.Timers;

namespace SImulator.ViewModel.Tests;

internal sealed class TestAnimatableTimer : IAnimatableTimer
{
    public int MaxTime { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public double Time => throw new NotImplementedException();

    public TimerState State => throw new NotImplementedException();

    public event Action<IAnimatableTimer> TimeChanged;

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void Pause(int currentTime, bool byUser)
    {
        throw new NotImplementedException();
    }

    public void Run(int maxTime, bool byUser, double? fromValue = null)
    {
        throw new NotImplementedException();
    }

    public void Stop()
    {
        throw new NotImplementedException();
    }
}
