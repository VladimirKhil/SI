using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIUI.ViewModel;

/// <summary>
/// Defines player info for showing on game table.
/// </summary>
public class SimplePlayerInfo : INotifyPropertyChanged
{
    private string _name = "";

    /// <summary>
    /// Player name.
    /// </summary>
    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; OnPropertyChanged(); } }
    }

    private int _sum = 0;

    /// <summary>
    /// Player score.
    /// </summary>
    public int Sum
    {
        get => _sum;
        set { if (_sum != value) { _sum = value; OnPropertyChanged(); } }
    }

    private PlayerState _state = PlayerState.None;

    /// <summary>
    /// Player state.
    /// </summary>
    public PlayerState State
    {
        get => _state;
        set { if (_state != value) { _state = value; OnPropertyChanged(); } }
    }

    private int _lostButtonIndex = -1;

    /// <summary>
    /// Player index in fighting for button press to answer.
    /// It starts from 2 (index of 1 got the player who wins the button).
    /// </summary>
    public int LostButtonIndex
    {
        get => _lostButtonIndex;
        set { if (_lostButtonIndex != value) { _lostButtonIndex = value; OnPropertyChanged(); } }
    }

    public override string ToString() => $"{_name}: {_sum}";

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;
}
