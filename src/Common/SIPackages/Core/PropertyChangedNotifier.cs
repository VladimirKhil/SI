using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace SIPackages.Core
{
    /// <summary>
    /// Класс, извещающий об изменениях своих свойств
    /// </summary>
    public abstract class PropertyChangedNotifier : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Сообщить об изменении свойства
        /// </summary>
        /// <param name="propertyName">Имя изменяемого свойства</param>
        protected void OnPropertyChanged<T>(T oldValue, [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new ExtendedPropertyChangedEventArgs<T>(propertyName, oldValue));
        }
    }
}
