using System.ComponentModel;

namespace SIPackages.Core;

/// <summary>
/// Extends <see cref="PropertyChangedEventArgs" /> class with information about old property value.
/// </summary>
/// <typeparam name="T">Old value type.</typeparam>
public sealed class ExtendedPropertyChangedEventArgs<T> : PropertyChangedEventArgs
{
    /// <summary>
    /// Old property value.
    /// </summary>
    public T OldValue { get; private set; }

    /// <summary>
    /// Initializes a new instance of <see cref="ExtendedPropertyChangedEventArgs{T}" /> class.
    /// </summary>
    /// <param name="propertyName">Property name.</param>
    /// <param name="oldValue">Old property value.</param>
    public ExtendedPropertyChangedEventArgs(string? propertyName, T oldValue)
        : base(propertyName) => OldValue = oldValue;
}
