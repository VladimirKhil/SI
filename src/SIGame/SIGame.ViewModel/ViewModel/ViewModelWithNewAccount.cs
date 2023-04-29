using SICore;
using SICore.Contracts;
using SICore.Network.Servers;
using System.Windows.Input;

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

    private NavigatorViewModel _content;

    public NavigatorViewModel Content
    {
        get => _content;
        set { if (_content != value) { _content = value; OnPropertyChanged(); } }
    }

    private string _fullError;

    public string FullError
    {
        get => _fullError;
        set { if (_fullError != value) { _fullError = value; OnPropertyChanged(); } }
    }

    public ICommand ShowFullError { get; private set; }

    public ICommand ChangeSettings { get; internal set; }

    public event Action<Node, IViewerClient, bool, bool, string, IFileShare?, int> StartGame;

    protected virtual void OnStartGame(
        Node server,
        IViewerClient host,
        bool networkGame,
        bool isOnline,
        string tempDocFolder,
        IFileShare? fileShare,
        int networkGamePort) =>
        StartGame?.Invoke(server, host, networkGame, isOnline, tempDocFolder, fileShare, networkGamePort);

    protected ViewModelWithNewAccount()
    {
        ShowFullError = new CustomCommand(ShowFullError_Executed);
    }

    protected ViewModelWithNewAccount(TModel model)
        : base(model)
    {
        ShowFullError = new CustomCommand(ShowFullError_Executed);
    }

    private void ShowFullError_Executed(object? arg)
    {
        PlatformSpecific.PlatformManager.Instance.ShowMessage(FullError, PlatformSpecific.MessageType.Warning, true);
    }

    protected override void Initialize()
    {
        base.Initialize();
        
        _closeContent = new CustomCommand(CloseContent_Executed);
    }

    protected virtual void CloseContent_Executed(object? arg)
    {
        Content = null;
    }
}
