using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SICore;

public sealed class Pair : INotifyPropertyChanged
{
    private int _first = 0;

    public int First
    {
        get => _first;
        set { _first = value; OnPropertyChanged(); }
    }

    private int _second;

    public int Second
    {
        get => _second;
        set { _second = value; OnPropertyChanged(); }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public event PropertyChangedEventHandler? PropertyChanged;
}
