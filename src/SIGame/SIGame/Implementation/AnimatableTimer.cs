using SIGame.ViewModel.PlatformSpecific;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;

namespace SIGame.Implementation
{
    internal sealed class AnimatableTimer : Animatable, IAnimatableTimer
    {
        public event Action<IAnimatableTimer> TimeChanged;

        public AnimatableTimer()
        {
            TimeDescriptor.AddValueChanged(this, (sender, r) =>
            {
                TimeChanged?.Invoke(this);
            });
        }

        public double Time
        {
            get { return (double)GetValue(TimeProperty); }
            set { SetValue(TimeProperty, value); }
        }

        public int MaxTime { get; set; }

        public TimerState State { get; private set; } = TimerState.Stopped;

        private bool _isUserPaused;
        private bool _isSystemPaused;

        // Using a DependencyProperty as the backing store for Time.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TimeProperty =
            DependencyProperty.Register(nameof(Time), typeof(double), typeof(AnimatableTimer), new PropertyMetadata(0.0));

        public static DependencyPropertyDescriptor TimeDescriptor = DependencyPropertyDescriptor.FromProperty(TimeProperty, typeof(AnimatableTimer));

        public void Run(int maxTime, bool byUser)
        {
            if (Dispatcher != System.Windows.Threading.Dispatcher.CurrentDispatcher)
            {
                Dispatcher.Invoke((Action<int, bool>)Run, maxTime, byUser);
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

            var animationTime = State == TimerState.Paused ? MaxTime * (1.0 - Time / 100) : MaxTime;

            if (animationTime < double.Epsilon)
            {
                return;
            }

            var duration = new Duration(TimeSpan.FromMilliseconds(animationTime * 100));

            State = TimerState.Running;

            var animation = new DoubleAnimation(100.0, duration) { FillBehavior = FillBehavior.Stop };

            BeginAnimation(TimeProperty, animation);
        }

        protected override Freezable CreateInstanceCore() => new AnimatableTimer();

        public void Stop()
        {
            if (Dispatcher != System.Windows.Threading.Dispatcher.CurrentDispatcher)
            {
                Dispatcher.Invoke(Stop);
                return;
            }

            State = TimerState.Stopped;
            
            BeginAnimation(TimeProperty, null);

            Time = 0.0;
        }

        public void Pause(int currentTime, bool byUser)
        {
            if (Dispatcher != System.Windows.Threading.Dispatcher.CurrentDispatcher)
            {
                Dispatcher.Invoke((Action<int, bool>)Pause, currentTime, byUser);
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
            
            if (State == TimerState.Running)
            {
                State = TimerState.Paused;

                if (MaxTime > 0)
                {
                    var animation = new DoubleAnimation(Math.Min(100.0, currentTime * 100 / MaxTime), new Duration(TimeSpan.FromMilliseconds(300))) { FillBehavior = FillBehavior.HoldEnd };

                    BeginAnimation(TimeProperty, animation);
                }
            }
        }
    }
}
