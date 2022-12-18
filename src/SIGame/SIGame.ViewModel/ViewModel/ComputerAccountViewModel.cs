using SICore;
using SIData;
using SIGame.ViewModel.Properties;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace SIGame.ViewModel;

/// <summary>
/// Информация о компьютерном игроке
/// </summary>
public sealed class ComputerAccountViewModel : AccountViewModel<ComputerAccount>, INavigationNode
{
    public ObservableCollection<char> P1 { get; private set; }

    public ObservableCollection<char> P2 { get; private set; }

    private string _caErrorMessage = string.Empty;

    public string CAErrorMessage
    {
        get => _caErrorMessage;
        set { _caErrorMessage = value; OnPropertyChanged(); }
    }

    public ComputerAccount Origin { get; }

    public string AddName => Origin == null ? Resources.Create : Resources.Save;

    public string Name
    {
        get => _model.Name;
        set
        {
            _model.Name = value;
            CheckComputerAccount();
        }
    }

    private void CheckComputerAccount()
    {
        CAErrorMessage = string.IsNullOrWhiteSpace(Model.Name)
            ? Resources.PlayerNameRequired
            : Origin == null && CommonSettings.Default.CompPlayers2.Any(acc => acc.Name == Model.Name)
                ? Resources.PlayerExists
                : "";

        _addNewComputerAccount.CanBeExecuted = CAErrorMessage.Length == 0;
    }

    private CustomCommand _addNewComputerAccount;

    public ICommand AddNewComputerAccount => _addNewComputerAccount;

    public event Action<ComputerAccount, ComputerAccount> Add;
    public event Action Close;

    public ComputerAccountViewModel()
    {

    }

    public ComputerAccountViewModel(ComputerAccount account, ComputerAccount origin)
        : base(account)
    {
        Origin = origin;

        if (Origin != null)
        {
            CheckComputerAccount();
        }
    }

    protected override void Initialize()
    {
        base.Initialize();

        P1 = new ObservableCollection<char>(_model.P1);
        P2 = new ObservableCollection<char>(_model.P2);

        P1.CollectionChanged += P1_CollectionChanged;
        P2.CollectionChanged += P2_CollectionChanged;

        _addNewComputerAccount = new CustomCommand(AddNewComputerAccount_Executed);

        CheckComputerAccount();
    }

    private void AddNewComputerAccount_Executed(object arg)
    {
        Add?.Invoke(Origin, _model);
        Close?.Invoke();
    }

    private void P1_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => _model.P1 = P1.ToArray();

    private void P2_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => _model.P2 = P2.ToArray();
}
