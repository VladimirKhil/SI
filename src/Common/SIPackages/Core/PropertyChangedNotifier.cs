using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIPackages.Core
{
    /// <summary>
    /// Defines an object that informs about its property value changes.
    /// </summary>
    public abstract class PropertyChangedNotifier : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Informs about object property value change.
        /// </summary>
        /// <param name="propertyName">Name of changed property.</param>
        protected void OnPropertyChanged<T>(T oldValue, [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new ExtendedPropertyChangedEventArgs<T>(propertyName, oldValue));
        }
    }
}
