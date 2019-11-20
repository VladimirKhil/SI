using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace SImulator
{
    public sealed class ExtendedRelayCommand: RelayCommand, INotifyPropertyChanged
    {
        private string name = string.Empty;

        /// <summary>
        /// Имя команды
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; OnPropertyChanged("Name"); }
        }

        public ExtendedRelayCommand(Action<object> execute)
            : base(execute)
        {
        }

        public ExtendedRelayCommand(Action<object> execute, Predicate<object> canExecute)
            : base(execute, canExecute)
        {
            
        }

        void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
