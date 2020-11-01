using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using SIData;
using System.Runtime.CompilerServices;

namespace SIGame.ViewModel
{
    public class SimpleAccount<T>: INotifyPropertyChanged
        where T: Account
    {
        private T _selectedAccount = null;

        public T SelectedAccount
        {
            get { return _selectedAccount; }
            set { _selectedAccount = value; OnPropertyChanged(); }
        }

        private IEnumerable<T> _selectionList = null;

        [XmlIgnore]
        public IEnumerable<T> SelectionList
        {
            get { return _selectionList; }
            set { _selectionList = value; OnPropertyChanged(); }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
