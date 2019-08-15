using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIPackages.Core
{
    public sealed class BagCatInfo: INotifyPropertyChanged
    {
        private int _minimum = 0;

        public int Minimum
        {
            get { return _minimum; }
            set { _minimum = value; OnPropertyChanged(); }
        }

        private int _maximum = 0;

        public int Maximum
        {
            get { return _maximum; }
            set { _maximum = value; OnPropertyChanged(); }
        }

        private int _step = 0;

        public int Step
        {
            get { return _step; }
            set { _step = value; OnPropertyChanged(); }
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
