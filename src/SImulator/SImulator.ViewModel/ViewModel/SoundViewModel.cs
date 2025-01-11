using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Utils.Commands;

namespace SImulator.ViewModel;

/// <summary>
/// Defines a game sound view model.
/// </summary>
public sealed class SoundViewModel : INotifyPropertyChanged
{
    private readonly Func<string> _getter;
    private readonly Action<string> _setter;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Sound name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Sound value.
    /// </summary>
    public string Value
    {
        get => _getter();
        set
        {
            _setter(value);
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    /// Clears the sound value.
    /// </summary>
    public ICommand Clear { get; }

    public SoundViewModel(string name, Func<string> getter, Action<string> setter)
    {
        Name = name;
        _getter = getter;
        _setter = setter;

        Clear = new SimpleCommand(arg => Value = "");
    }
}
