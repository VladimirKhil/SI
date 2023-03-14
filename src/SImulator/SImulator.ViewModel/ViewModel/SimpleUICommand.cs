using System.ComponentModel;
using System.Runtime.CompilerServices;
using Utils.Commands;

namespace SImulator.ViewModel;

/// <summary>
/// Provides a named command.
/// </summary>
public sealed class SimpleUICommand : SimpleCommand, INotifyPropertyChanged
{
    private string _name = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Command name.
    /// </summary>
    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; OnPropertyChanged(); } }
    }

    public SimpleUICommand(Action<object?> action) : base(action) { }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
