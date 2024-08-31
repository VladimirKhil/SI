using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIGame.ViewModel.Models;

public sealed class PlayerSumInfo : INotifyPropertyChanged
{
    private int _playerIndex = 0;

    public int PlayerIndex
    {
        get => _playerIndex;
        set { _playerIndex = value; OnPropertyChanged(); }
    }

    private int _playerScore;

    public int PlayerScore
    {
        get => _playerScore;
        set { _playerScore = value; OnPropertyChanged(); }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public event PropertyChangedEventHandler? PropertyChanged;
}
