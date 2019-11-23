using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Threading.Tasks;
using System.ComponentModel;
using SIQuester.ViewModel.Core;
using System.Runtime.CompilerServices;

namespace SIQuester.ViewModel
{
    public sealed class DBNode: INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Key { get; set; }

        private DBNode[] _children;

        public DBNode[] Children
        {
            get
            {
                return _children;
            }
            set
            {
                _children = value;
                OnPropertyChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private bool isExpanded = false;

        public bool IsExpanded
        {
            get { return isExpanded; }
            set
            {
                if (isExpanded != value)
                {
                    isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
