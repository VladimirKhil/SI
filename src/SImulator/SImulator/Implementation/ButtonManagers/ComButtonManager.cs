using SImulator.ViewModel;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel.Core;
using System;
using System.IO.Ports;

namespace SImulator.Implementation.ButtonManagers
{
    internal sealed class ComButtonManager : ButtonManagerBase
    {
        private readonly string _portName;
        private readonly SerialPort _comPort = new SerialPort();

        public ComButtonManager(string portName)
        {
            _portName = portName;
            _comPort.DataReceived += ComPort_DataReceived;
        }

        private void ComPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int bytes = _comPort.BytesToRead;
            if (bytes > 0)
            {
                var buffer = new byte[bytes];
                _comPort.Read(buffer, 0, bytes);

                OnKeyPressed((GameKey)buffer[0]);
            }
        }

        public override bool Run()
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

        private void ShowError(string error)
        {
            System.Windows.MessageBox.Show(error, MainViewModel.ProductName, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }

        public override void Stop()
        {
            _comPort.Close();
        }

        public override void Dispose()
        {
            _comPort.Dispose();
        }
    }
}
