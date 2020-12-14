using SIGame.ViewModel.PlatformSpecific;
using System;

namespace SIGame.Tests
{
	internal sealed class AnimatableTimerMock : IAnimatableTimer
	{
		public int MaxTime { get => 100; set { } }

		public double Time => throw new NotImplementedException();

		public TimerState State => throw new NotImplementedException();

		public event Action<IAnimatableTimer> TimeChanged
		{
			add { }
			remove { }
		}

		public void Pause(int currentTime, bool byUser)
		{
			throw new NotImplementedException();
		}

		public void Run(int maxTime, bool byUser)
		{
			throw new NotImplementedException();
		}

		public void Stop()
		{
			throw new NotImplementedException();
		}
	}
}
