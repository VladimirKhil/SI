using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SIUI.ViewModel;

namespace SImulator.ViewModel
{
    public sealed class SimpleUICommand: SimpleCommand, INotifyPropertyChanged
    {
        private string _name = "";

        /// <summary>
        /// Имя команды
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(); }
        }

        public SimpleUICommand(Action<object> action) : base(action)
        {
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
