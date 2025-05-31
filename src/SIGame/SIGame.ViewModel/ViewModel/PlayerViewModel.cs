using SICore;
using SIData;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIGame.ViewModel;

public sealed class PlayerViewModel : IPersonViewModel, INotifyPropertyChanged
{
    public PlayerAccount Model { get; }

    public bool IsPlayer => true;

    public bool IsHuman => Model.IsHuman;

    public bool IsConnected => Model.IsConnected;

    private Account[] _others = Array.Empty<Account>();

    public event PropertyChangedEventHandler? PropertyChanged;

    public Account[] Others
    {
        get => _others;
        set
        {
            if (_others != value)
            {
                _others = value;
                OnPropertyChanged();
            }
        }
    }

    public string Name => Model.Name;

    public PlayerViewModel(PlayerAccount model) => Model = model;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
