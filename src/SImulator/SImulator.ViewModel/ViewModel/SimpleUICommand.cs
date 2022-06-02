using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SIUI.ViewModel;

namespace SImulator.ViewModel
{
    /// <summary>
    /// Provides a named command.
    /// </summary>
    public sealed class SimpleUICommand : SimpleCommand, INotifyPropertyChanged
    {
        private string _name = "";

        /// <summary>
        /// Command name.
        /// </summary>
        public string Name
        {
            get => _name;
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
