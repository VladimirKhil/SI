using SIUI.ViewModel;

namespace SImulator.ViewModel;

/// <summary>
/// Provides a named command.
/// </summary>
public sealed class SimpleUICommand : SimpleCommand
{
    private string _name = "";

    /// <summary>
    /// Command name.
    /// </summary>
    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; OnPropertyChanged(); } }
    }

    public SimpleUICommand(Action<object?> action) : base(action) { }
}
