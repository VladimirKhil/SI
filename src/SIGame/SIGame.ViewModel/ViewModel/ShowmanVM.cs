using SICore;
using SIData;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIGame.ViewModel;

public sealed class ShowmanVM : IPersonViewModel, INotifyPropertyChanged
{
    public PersonAccount Model { get; }

    public bool IsPlayer => false;

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

    public ShowmanVM(PersonAccount model) => Model = model;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
