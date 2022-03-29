using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIPackages.Core
{
    /// <summary>
    /// Defines a secret question info.
    /// </summary>
    public class BagCatInfo: INotifyPropertyChanged
    {
        private int _minimum = 0;

        /// <summary>
        /// Minimum stake value.
        /// </summary>
        public int Minimum
        {
            get { return _minimum; }
            set { _minimum = value; OnPropertyChanged(); }
        }

        private int _maximum = 0;

        /// <summary>
        /// Maximum stake value.
        /// </summary>
        public int Maximum
        {
            get { return _maximum; }
            set { _maximum = value; OnPropertyChanged(); }
        }

        private int _step = 0;

        /// <summary>
        /// Step (a minimum distance between two possible stakes) value.
        /// </summary>
        public int Step
        {
            get { return _step; }
            set { _step = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Property change notifier.
        /// </summary>
        /// <param name="name">Name of the property that has changed.</param>
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
