using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIQuester.ViewModel.Model;

/// <summary>
/// Represents a questions database node.
/// </summary>
public sealed class DBNode : INotifyPropertyChanged
{
    public string Name { get; set; }

    public string Key { get; set; }

    private DBNode[] _children;

    public DBNode[] Children
    {
        get => _children;
        set
        {
            _children = value;
            OnPropertyChanged();
        }
    }

    private bool _isExpanded = false;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnPropertyChanged();
            }
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;
}
