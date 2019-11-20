using System;
using System.Linq;
using System.Collections;
using System.Windows.Input;
using System.Threading;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel;
using SImulator.ViewModel.Core;

namespace SImulator.Implementation.ButtonManagers
{
    /// <summary>
    /// Прослушиватель джойстика
    /// </summary>
    internal sealed class JoystickListener: ButtonManagerBase
    {
        private const int Period = 100;

        private SharpDX.DirectInput.DirectInput _directInput = null;
        private SharpDX.DirectInput.Joystick _joystick = null;

        private readonly System.Windows.Threading.Dispatcher _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

        private readonly Timer _timer;
        private readonly System.Windows.Forms.Form _form;

        private bool _acquired = false;

        private readonly object _sync = new object();

        public JoystickListener()
        {
            _form = new System.Windows.Forms.Form();
            _timer = new Timer(Timer_Elapsed, null, Timeout.Infinite, Period);
        }

        private void Timer_Elapsed(object state)
        {
            if (!Monitor.TryEnter(_sync))
                return;

            try
            {
                _joystick.Poll();

                var data = _joystick.GetBufferedData();
                if (data.Length > 0)
                {
                    var buttonData = data.Where(upd => upd.Offset >= SharpDX.DirectInput.JoystickOffset.Buttons0 && upd.Offset <= SharpDX.DirectInput.JoystickOffset.Buttons127 && upd.Value == 128);
                    if (buttonData.Any())
                    {
                        var buttonDataFirstTimestamp = buttonData.First().Timestamp;
                        var states = buttonData.Where(upd => upd.Timestamp == buttonDataFirstTimestamp).ToArray();

                        var index = new Random().Next(states.Length);
                        Pressed(states[index].Offset - SharpDX.DirectInput.JoystickOffset.Buttons0);
                    }
                }
            }
            finally
            {
                Monitor.Exit(_sync);
            }
        }

        void Pressed(int button)
        {
            _dispatcher.BeginInvoke((Action)(() =>
                {
                    OnKeyPressed((GameKey)(Key.D1 + button));
                }));
        }

        public override bool Run()
        {
            lock (_sync)
            {
                try
                {
                    if (_directInput == null)
                    {
                        _directInput = new SharpDX.DirectInput.DirectInput();
                    }

                    if (_joystick == null)
                    {
                        var devices = _directInput.GetDevices(SharpDX.DirectInput.DeviceClass.GameControl, SharpDX.DirectInput.DeviceEnumerationFlags.AttachedOnly);
                        
                        if (devices.Count == 0)
                        {
                            ShowError("Джойстик не обнаружен!");
                            return false;
                        }

                        _joystick = new SharpDX.DirectInput.Joystick(_directInput, devices[0].InstanceGuid);
                        _joystick.SetCooperativeLevel(_form, SharpDX.DirectInput.CooperativeLevel.Background | SharpDX.DirectInput.CooperativeLevel.NonExclusive);
                        _joystick.Properties.BufferSize = 128;
                    }

                    _joystick.Acquire();
                    _timer.Change(Period, Period);

                    _acquired = true;

                    return true;
                }
                catch (Exception exc)
                {
                    ShowError(exc.Message);
                    return false;
                }
            }
        }

        private void ShowError(string error)
        {
            System.Windows.MessageBox.Show(error, MainViewModel.ProductName, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }

        public override void Stop()
        {
            lock (_sync)
            {
                if (_acquired)
                {
                    _timer.Change(Timeout.Infinite, Period);
                    _joystick.Unacquire();

                    _acquired = false;
                }
            }
        }

        public override void Dispose()
        {
            lock (_sync)
            {
                if (_joystick != null)
                {
                    _joystick.Dispose();
                    _joystick = null;
                }

                if (_directInput != null)
                {
                    _directInput.Dispose();
                    _directInput = null;
                }
            }

            _form.Dispose();
            _timer.Dispose();
        }
    }
}
