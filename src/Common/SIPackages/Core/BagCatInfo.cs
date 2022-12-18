using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIPackages.Core;

/// <summary>
/// Defines a secret question info.
/// </summary>
public class BagCatInfo : INotifyPropertyChanged
{
    private int _minimum = 0;

    /// <summary>
    /// Minimum stake value.
    /// </summary>
    public int Minimum
    {
        get => _minimum;
        set { if (_minimum != value) { _minimum = value; OnPropertyChanged(); } }
    }

    private int _maximum = 0;

    /// <summary>
    /// Maximum stake value.
    /// </summary>
    public int Maximum
    {
        get => _maximum;
        set { if (_maximum != value) { _maximum = value; OnPropertyChanged(); } }
    }

    private int _step = 0;

    /// <summary>
    /// Step (a minimum distance between two possible stakes) value.
    /// </summary>
    public int Step
    {
        get => _step;
        set { if (_step != value) { _step = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Raises object property change event.
    /// </summary>
    /// <param name="name">Name of the property wchich value has been changed.</param>
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;
}
