using System;
using System.Windows.Input;
using System.IO;
using SICore;
using SICore.Network.Servers;

namespace SIGame.ViewModel
{
    public abstract class ViewModelWithNewAccount<TModel> : ViewModel<TModel>
        where TModel: IHumanPlayerOwner, new()
    {
        private HumanAccount _human;

        public HumanAccount Human
        {
            get { return _human; }
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
            get { return _content; }
            set { if (_content != value) { _content = value; OnPropertyChanged(); } }
        }

        private string _fullError;

        public string FullError
        {
            get { return _fullError; }
            set { if (_fullError != value) { _fullError = value; OnPropertyChanged(); } }
        }

        public ICommand ShowFullError { get; private set; }

        protected virtual void LoadNewSettings(UserSettings settings)
        {
            
        }

        public ICommand ChangeSettings { get; internal set; }

        public event Action<Node, IViewerClient, bool, bool, string, int> StartGame;

        protected virtual void OnStartGame(Node server, IViewerClient host, bool networkGame, bool isOnline, string tempDocFolder, int networkGamePort) =>
            StartGame?.Invoke(server, host, networkGame, isOnline, tempDocFolder, networkGamePort);

        protected ViewModelWithNewAccount()
        {
            ShowFullError = new CustomCommand(ShowFullError_Executed);
        }

        protected ViewModelWithNewAccount(TModel model)
            : base(model)
        {
            ShowFullError = new CustomCommand(ShowFullError_Executed);
        }

        private void ShowFullError_Executed(object arg)
        {
            PlatformSpecific.PlatformManager.Instance.ShowMessage(FullError, PlatformSpecific.MessageType.Warning, true);
        }

        protected override void Initialize()
        {
            base.Initialize();
            
            _closeContent = new CustomCommand(CloseContent_Executed);
        }

        protected virtual void CloseContent_Executed(object arg)
        {
            Content = null;
        }
    }
}
