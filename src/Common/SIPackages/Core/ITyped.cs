using System.ComponentModel;

namespace SIPackages.Core;

/// <summary>
/// Defines an object with type.
/// </summary>
public interface ITyped : INotifyPropertyChanged
{
    /// <summary>
    /// Object type.
    /// </summary>
    string Type { get; set; }
}
