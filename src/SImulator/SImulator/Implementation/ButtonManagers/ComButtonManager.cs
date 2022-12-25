using SImulator.ViewModel;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel.Core;
using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace SImulator.Implementation.ButtonManagers;

/// <summary>
/// Provides COM-based player buttons.
/// </summary>
internal sealed class ComButtonManager : ButtonManagerBase
{
    private readonly string _portName;
    private readonly SerialPort _comPort = new();

    public ComButtonManager(string portName, IButtonManagerListener buttonManagerListener) : base(buttonManagerListener)
    {
        _portName = portName;
        _comPort.DataReceived += ComPort_DataReceived;
    }

    public override bool Start()
    {
        try
        {
            _comPort.PortName = _portName;
            _comPort.Open();
            return true;
        }
        catch (Exception exc)
        {
            ShowError(exc.Message);
            return false;
        }
    }

    public override void Stop()
    {
        _comPort.Close();
    }

    public override ValueTask DisposeAsync()
    {
        _comPort.Dispose();

        return new ValueTask();
    }

    private void ComPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        int bytes = _comPort.BytesToRead;
        if (bytes <= 0)
        {
            return;
        }

        var buffer = new byte[bytes];
        _comPort.Read(buffer, 0, bytes);

        Listener.OnKeyPressed((GameKey)buffer[0]);
    }

    private static void ShowError(string error) => System.Windows.MessageBox.Show(
        error,
        MainViewModel.ProductName,
        System.Windows.MessageBoxButton.OK,
        System.Windows.MessageBoxImage.Error);
}
