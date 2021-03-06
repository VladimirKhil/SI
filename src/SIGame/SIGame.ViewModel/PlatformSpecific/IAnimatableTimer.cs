﻿using System;

namespace SIGame.ViewModel.PlatformSpecific
{
    public interface IAnimatableTimer
    {
        int MaxTime { get; set; }
        double Time { get; }

        TimerState State { get; }

        event Action<IAnimatableTimer> TimeChanged;

        void Run(int maxTime, bool byUser);
        void Stop();
        void Pause(int currentTime, bool byUser);
    }
}
