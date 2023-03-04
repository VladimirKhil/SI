using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIPackages.Core;

/// <summary>
/// Defines an object that informs about its property value changes.
/// </summary>
[Serializable]
public abstract class PropertyChangedNotifier : INotifyPropertyChanged
{
    /// <inheritdoc />
    [field: NonSerialized]
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises object property change event.
    /// </summary>
    /// <param name="oldValue">Old property value.</param>
    /// <param name="propertyName">Changed property name.</param>
    protected void OnPropertyChanged<T>(T oldValue, [CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new ExtendedPropertyChangedEventArgs<T>(propertyName, oldValue));
}
