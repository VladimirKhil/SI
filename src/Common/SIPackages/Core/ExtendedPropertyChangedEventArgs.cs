using System.ComponentModel;

namespace SIPackages.Core
{
    public sealed class ExtendedPropertyChangedEventArgs<T> : PropertyChangedEventArgs
    {
        public T OldValue { get; private set; }

        public ExtendedPropertyChangedEventArgs(string propertyName, T oldValue)
            : base(propertyName)
        {
            this.OldValue = oldValue;
        }
    }
}
