using SICore;
using SIGame.ViewModel.Properties;
using System.Windows.Input;

namespace SIGame.ViewModel;

public sealed class HumanAccountViewModel : AccountViewModel<HumanAccount>
{
    public bool IsProgress => false;

    private string _haErrorMessage = "";

    public string HAErrorMessage
    {
        get => _haErrorMessage;
        set
        {
            if (_haErrorMessage != value)
            {
                _haErrorMessage = value;
                OnPropertyChanged();
            }
        }
    }

    private bool isEdit;

    public bool IsEdit
    {
        get => isEdit;
        set
        {
            if (isEdit != value)
            {
                isEdit = value;
                OnPropertyChanged();
                CheckHumanAccount();
            }
        }
    }

    public string CommitHeader => IsEdit ? Resources.Save : Resources.Create;

    private CustomCommand _addNewAccount;

    public ICommand AddNewAccount => _addNewAccount;

    public HumanAccount CurrentAccount { get; internal set; }

    public event Action Add;
    public event Action Edit;

    public HumanAccountViewModel()
    {

    }

    public HumanAccountViewModel(HumanAccount account)
        : base(account)
    {

    }

    protected override void Initialize()
    {
        base.Initialize();

        _addNewAccount = new CustomCommand(AddNewAccount_Executed);

        _model.PropertyChanged += Model_PropertyChanged;
        CheckHumanAccount();
    }

    private void AddNewAccount_Executed(object arg)
    {
        if (IsEdit)
            Edit?.Invoke();
        else
            Add?.Invoke();
    }

    private void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HumanAccount.Name) || e.PropertyName == nameof(HumanAccount.BirthDate))
        {
            CheckHumanAccount();
        }
    }

    private void CheckHumanAccount()
    {
        if (string.IsNullOrWhiteSpace(_model.Name))
            HAErrorMessage = Resources.NameRequired;
        else if (!IsEdit && CommonSettings.Default.Humans2.Any(acc => acc.Name == _model.Name))
            HAErrorMessage = Resources.AlreadyExists;
        else if (!_model.BirthDate.HasValue)
            HAErrorMessage = Resources.BirthDateRequired;
        else
            HAErrorMessage = "";

        _addNewAccount.CanBeExecuted = HAErrorMessage.Length == 0;
    }
}
