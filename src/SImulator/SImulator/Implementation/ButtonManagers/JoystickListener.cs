using SharpDX.DirectInput;
using SImulator.ViewModel;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel.Core;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SImulator.Implementation.ButtonManagers;

/// <summary>
/// Provides joystick-based player buttons.
/// </summary>
internal sealed class JoystickListener : ButtonManagerBase
{
    private const int Period = 100;

    private DirectInput? _directInput = null;
    private Joystick? _joystick = null;

    private readonly System.Windows.Threading.Dispatcher _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

    private readonly Timer _timer;
    private readonly System.Windows.Forms.Form _form; // TODO: replace with HwndHost

    private bool _acquired = false;

    private readonly object _sync = new();

    public JoystickListener(IButtonManagerListener buttonManagerListener) : base(buttonManagerListener)
    {
        _form = new System.Windows.Forms.Form();
        _timer = new Timer(Timer_Elapsed, null, Timeout.Infinite, Period);
    }

    private void Timer_Elapsed(object? state)
    {
        if (!Monitor.TryEnter(_sync))
        {
            return;
        }

        try
        {
            _joystick.Poll();

            var data = _joystick.GetBufferedData();
            if (data.Length == 0)
            {
                return;
            }

            var buttonData = data.Where(upd =>
                upd.Offset >= JoystickOffset.Buttons0
                && upd.Offset <= JoystickOffset.Buttons127
                && upd.Value == 128);

            if (!buttonData.Any())
            {
                return;
            }

            var buttonDataFirstTimestamp = buttonData.First().Timestamp;
            var states = buttonData.Where(upd => upd.Timestamp == buttonDataFirstTimestamp).ToArray();

            var index = new Random().Next(states.Length);
            Pressed(states[index].Offset - JoystickOffset.Buttons0);
        }
        finally
        {
            Monitor.Exit(_sync);
        }
    }

    private void Pressed(int button) => _dispatcher.BeginInvoke(() => Listener.OnKeyPressed((GameKey)(System.Windows.Input.Key.D1 + button)));

    public override bool Start()
    {
        lock (_sync)
        {
            try
            {
                _directInput ??= new DirectInput();

                if (_joystick == null)
                {
                    var devices = _directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
                    
                    if (devices.Count == 0)
                    {
                        ShowError("Джойстик не обнаружен!");
                        return false;
                    }

                    _joystick = new Joystick(_directInput, devices[0].InstanceGuid);
                    _joystick.SetCooperativeLevel(_form.Handle, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
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

    private static void ShowError(string error) => System.Windows.MessageBox.Show(
        error,
        MainViewModel.ProductName,
        System.Windows.MessageBoxButton.OK,
        System.Windows.MessageBoxImage.Error);

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

    public override ValueTask DisposeAsync()
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

        return new ValueTask();
    }
}
