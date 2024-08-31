using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SICore;

/// <summary>
/// Defines a showman data.
/// </summary>
public sealed class ShowmanData : INotifyPropertyChanged
{
    private CustomCommand? _manageTable;

    /// <summary>
    /// Manage game table command.
    /// </summary>
    public CustomCommand? ManageTable
    {
        get => _manageTable;
        set
        {
            if (_manageTable != value)
            {
                _manageTable = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
