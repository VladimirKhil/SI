using System.Diagnostics;

namespace SIPackages.Core;

/// <inheritdoc cref="INamed" />
public class Named : PropertyChangedNotifier, INamed
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _name = "";

    /// <inheritdoc />
    public virtual string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                var oldValue = _name;
                _name = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    /// <summary>
    /// Iitializes a new instance of <see cref="Named" /> class with an empty name.
    /// </summary>
    public Named() { }

    /// <summary>
    /// Iitializes a new instance of <see cref="Named" /> class.
    /// </summary>
    /// <param name="name">Object name.</param>
    public Named(string name) => _name = name;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Named named && Name == named.Name;

    /// <inheritdoc />
    public override int GetHashCode() => Name.GetHashCode();
}
