using SICore;
using System.Windows.Input;
using Utils.Commands;

namespace SIGame.ViewModel;

public abstract class ViewModelWithNewAccount<TModel> : ViewModel<TModel>
    where TModel: IHumanPlayerOwner, new()
{
    private HumanAccount _human;

    public HumanAccount Human
    {
        get => _human;
        set
        {
            _human = value;
            OnHumanChanged();
        }
    }

    protected virtual void OnHumanChanged()
    {
        
    }

    protected ICommand _closeContent;

    private NavigatorViewModel? _content;

    public NavigatorViewModel? Content
    {
        get => _content;
        set { if (_content != value) { _content = value; OnPropertyChanged(); } }
    }

    private string? _fullError;

    public string? FullError
    {
        get => _fullError;
        set { if (_fullError != value) { _fullError = value; OnPropertyChanged(); } }
    }

    public ICommand ShowFullError { get; private set; }

    public ICommand ChangeSettings { get; internal set; }

    public event Action<GameViewModel, ViewerHumanLogic> StartGame;

    protected virtual void OnStartGame(GameViewModel gameViewModel, ViewerHumanLogic logic) => StartGame?.Invoke(gameViewModel, logic);

    protected ViewModelWithNewAccount()
    {
        ShowFullError = new SimpleCommand(ShowFullError_Executed);
    }

    protected ViewModelWithNewAccount(TModel model)
        : base(model)
    {
        ShowFullError = new SimpleCommand(ShowFullError_Executed);
    }

    private void ShowFullError_Executed(object? arg)
    {
        PlatformSpecific.PlatformManager.Instance.ShowMessage(FullError, PlatformSpecific.MessageType.Warning, true);
    }

    protected override void Initialize()
    {
        base.Initialize();
        
        _closeContent = new SimpleCommand(CloseContent_Executed);
    }

    protected virtual void CloseContent_Executed(object? arg)
    {
        Content = null;
    }
}
