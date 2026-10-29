using Avalonia.Threading;
using System.Diagnostics;
using Utils.Timers;

namespace SImulator.Implementation;

/// <summary>
/// Game timer animated from 0.0 to 100.0 (port of the WPF AnimatableTimer).
/// </summary>
internal sealed class AnimatableTimer : IAnimatableTimer
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(33);
    private static readonly TimeSpan PauseAnimationDuration = TimeSpan.FromMilliseconds(300);

    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _stopwatch = new();

    private double _from;
    private double _to;
    private TimeSpan _duration;
    private bool _isPauseAnimation;
    private bool _isUserPaused;
    private bool _isSystemPaused;
    private double _time;

    /// <summary>
    /// Maximum running time, 0.1 s.
    /// </summary>
    public int MaxTime { get; set; }

    /// <summary>
    /// Current timer time, from 0.0 to 100.0.
    /// </summary>
    public double Time
    {
        get => _time;
        private set
        {
            if (_time != value)
            {
                _time = value;
                TimeChanged?.Invoke(this);
            }
        }
    }

    public bool KeepFinalValue { get; set; }

    public TimerState State { get; private set; } = TimerState.Stopped;

    public event Action<IAnimatableTimer>? TimeChanged;

    public AnimatableTimer()
    {
        _timer = new DispatcherTimer(TickInterval, DispatcherPriority.Render, OnTick);
        _timer.Stop();
    }

    public void Run(int maxTime, bool byUser, double? fromValue = null)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Invoke(() => Run(maxTime, byUser, fromValue));
            return;
        }

        if (maxTime > -1)
        {
            MaxTime = maxTime;
        }

        if (byUser)
        {
            _isUserPaused = false;

            if (State != TimerState.Paused)
            {
                return;
            }
        }
        else
        {
            _isSystemPaused = false;
        }

        if (_isUserPaused || _isSystemPaused)
        {
            return;
        }

        var wasPaused = State == TimerState.Paused;
        var start = fromValue ?? (wasPaused ? Time : 0.0);
        var animationTime = wasPaused ? MaxTime * (1.0 - start / 100) : MaxTime;

        if (animationTime < double.Epsilon)
        {
            return;
        }

        State = TimerState.Running;
        Animate(start, 100.0, TimeSpan.FromMilliseconds(animationTime * 100), isPauseAnimation: false);
    }

    public void Stop()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Invoke(Stop);
            return;
        }

        State = TimerState.Stopped;
        _timer.Stop();
        Time = 0.0;
    }

    public void Pause(int currentTime, bool byUser)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Invoke(() => Pause(currentTime, byUser));
            return;
        }

        if (byUser)
        {
            _isUserPaused = true;
        }
        else
        {
            _isSystemPaused = true;
        }

        if (State != TimerState.Running)
        {
            return;
        }

        State = TimerState.Paused;
        _timer.Stop();

        if (MaxTime > 0)
        {
            Animate(Time, Math.Min(100.0, currentTime * 100.0 / MaxTime), PauseAnimationDuration, isPauseAnimation: true);
        }
    }

    private void Animate(double from, double to, TimeSpan duration, bool isPauseAnimation)
    {
        _from = from;
        _to = to;
        _duration = duration;
        _isPauseAnimation = isPauseAnimation;
        _stopwatch.Restart();
        Time = from;
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var progress = _duration > TimeSpan.Zero ? Math.Min(1.0, _stopwatch.Elapsed / _duration) : 1.0;
        Time = _from + (_to - _from) * progress;

        if (progress < 1.0)
        {
            return;
        }

        _timer.Stop();

        if (_isPauseAnimation)
        {
            return;
        }

        if (State == TimerState.Running)
        {
            State = TimerState.Completed;
        }

        if (!KeepFinalValue)
        {
            Time = 0.0;
        }
    }

    public void Dispose() => _timer.Stop();
}
