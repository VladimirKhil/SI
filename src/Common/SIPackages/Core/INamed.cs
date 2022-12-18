using System.ComponentModel;

namespace SIPackages.Core;

/// <summary>
/// Defines an object with name.
/// </summary>
public interface INamed : INotifyPropertyChanged
{
    /// <summary>
    /// Object name.
    /// </summary>
    string Name { get; set; }
}
