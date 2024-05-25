using Utils.Timers;

namespace SIGame.Tests;

internal sealed class AnimatableTimerMock : IAnimatableTimer
{
	public int MaxTime { get => 100; set { } }

	public double Time => throw new NotImplementedException();

	public TimerState State => throw new NotImplementedException();

    public bool KeepFinalValue { get; set; }

    public event Action<IAnimatableTimer> TimeChanged
	{
		add { }
		remove { }
	}

    public void Pause(int currentTime, bool byUser) => throw new NotImplementedException();

    public void Run(int maxTime, bool byUser, double? fromValue = null) { }

	public void Stop() { }

    public void Dispose() { }
}
