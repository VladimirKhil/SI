using SICore;
using SIGame.ViewModel.PlatformSpecific;
using SIGame.ViewModel.Properties;
using System.Windows.Input;

namespace SIGame.ViewModel;

public sealed class HumanPlayerViewModel : ViewModel<GameSettings>
{
    public bool IsProgress => false;

    internal event Action HumanPlayerChanged;

    private HumanAccount _humanPlayer = null;

    private readonly CommonSettings _commonSettings;

    /// <summary>
    /// Живой игрок
    /// </summary>
    public HumanAccount HumanPlayer
    {
        get => _humanPlayer;
        set
        {
            _model.HumanPlayerName = value != null ? value.Name : "";

            SetHumanPlayer();
            OnPropertyChanged();

            OnHumanPlayerChanged();
        }
    }

    public event Action AccountEditing;

    public event Action NewAccountCreating;
    public event Action NewAccountCreated;

    private HumanAccount[] _humanPlayers;

    public HumanAccount[] HumanPlayers
    {
        get => _humanPlayers;
        set
        {
            _humanPlayers = value;
            OnPropertyChanged();
        }
    }

    private readonly HumanAccount _newHumanAccount = new HumanAccount { Name = Resources.New + "…", CanBeDeleted = false };

    public void UpdateHumanPlayers()
    {
        var result = new List<HumanAccount>(_commonSettings.Humans2)
        {
            _newHumanAccount
        };

        HumanPlayers = result.ToArray();
    }

    private void OnHumanPlayerChanged()
    {
        if (_humanPlayer == _newHumanAccount)
        {
            NewAccount = new HumanAccountViewModel(new HumanAccount { CanBeDeleted = true });
            NewAccountCreating?.Invoke();
        }

        HumanPlayerChanged?.Invoke();
    }

    private HumanAccountViewModel? _newAccount = null;

    public HumanAccountViewModel? NewAccount
    {
        get => _newAccount;
        set
        {
            if (_newAccount != null)
            {
                _newAccount.Add -= NewAccount_Add;
                _newAccount.Edit -= NewAccount_Edit;
            }

            _newAccount = value;

            if (_newAccount != null)
            {
                _newAccount.Add += NewAccount_Add;
                _newAccount.Edit += NewAccount_Edit;
            }

            OnPropertyChanged();
        }
    }

    public ICommand EditAccount { get; private set; }

    public ICommand RemoveAccount { get; private set; }

    private void NewAccount_Add()
    {
        _commonSettings.Humans2.Add(NewAccount.Model);

        UpdateHumanPlayers();
        HumanPlayer = NewAccount.Model;
        NewAccount = null;

        NewAccountCreated?.Invoke();
    }

    private void NewAccount_Edit()
    {
        var currentAccount = NewAccount.CurrentAccount;
        var newAccount = _newAccount.Model;

        currentAccount.Name = newAccount.Name;
        currentAccount.IsMale = newAccount.IsMale;
        currentAccount.BirthDate = newAccount.BirthDate;
        currentAccount.Picture = newAccount.Picture;

        NewAccount = null;

        NewAccountCreated?.Invoke();
    }

    private void SetHumanPlayer() => 
        _humanPlayer = HumanPlayers.FirstOrDefault(acc => acc.Name == _model.HumanPlayerName);

    public HumanPlayerViewModel(CommonSettings commonSettings)
    {
        _commonSettings = commonSettings;
        InitializeCore();
    }

    public HumanPlayerViewModel(GameSettings model, CommonSettings commonSettings)
        : base(model)
    {
        _commonSettings = commonSettings;
        InitializeCore();
    }

    private void InitializeCore()
    {
        EditAccount = new CustomCommand(EditAccount_Executed);
        RemoveAccount = new CustomCommand(RemoveAccount_Executed);

        UpdateHumanPlayers();

        SetHumanPlayer();

        if (_humanPlayer == null)
        {
            _humanPlayer = _commonSettings.Humans2.FirstOrDefault() ?? _newHumanAccount;
            _model.HumanPlayerName = _humanPlayer.Name;
        }

        OnHumanPlayerChanged();
    }

    private void EditAccount_Executed(object arg)
    {
        var humanAccount = arg as HumanAccount;
        var humanAccountCopy = new HumanAccount(humanAccount) { CanBeDeleted = true };

        NewAccount = new HumanAccountViewModel(humanAccountCopy)
        {
            IsEdit = true,
            CurrentAccount = humanAccount
        };

        AccountEditing?.Invoke();
    }

    private void RemoveAccount_Executed(object arg)
    {
        if (arg is HumanAccount humanAccount
            && humanAccount.CanBeDeleted
            && PlatformManager.Instance.Ask(string.Format(Resources.DeleteConfirm, humanAccount.Name)))
        {
            _commonSettings.Humans2.Remove(humanAccount);
            UpdateHumanPlayers();

            HumanPlayer = HumanPlayers.First();
        }
    }
}
