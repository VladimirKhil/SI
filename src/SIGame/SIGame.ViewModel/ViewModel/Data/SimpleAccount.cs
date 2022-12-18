using SIData;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace SIGame.ViewModel;

public class SimpleAccount<T> : INotifyPropertyChanged
    where T: Account
{
    private T? _selectedAccount = null;

    public T? SelectedAccount
    {
        get => _selectedAccount;
        set { _selectedAccount = value; OnPropertyChanged(); }
    }

    private IEnumerable<T>? _selectionList = null;

    [XmlIgnore]
    public IEnumerable<T>? SelectionList
    {
        get => _selectionList;
        set { _selectionList = value; OnPropertyChanged(); }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;
}
