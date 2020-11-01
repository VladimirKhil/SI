using SICore;
using SIData;
using SIGame.ViewModel.Properties;
using System;
using System.Windows.Input;

namespace SIGame.ViewModel
{
    public sealed class ShowmanViewModel : AccountViewModel<ComputerAccount>, INavigationNode
    {
        private string _haErrorMessage = "";

        public string HAErrorMessage
        {
            get { return _haErrorMessage; }
            set { _haErrorMessage = value; OnPropertyChanged(); }
        }

        public event Action<ComputerAccount> Add;
        public event Action Close;

        private CustomCommand _addNewAccount;

        public ICommand AddNewAccount => _addNewAccount;

        public string CommitHeader => Resources.Create;

        public ShowmanViewModel(ComputerAccount account)
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
            Add?.Invoke(_model);
            Close?.Invoke();
        }

        void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Account.Name))
            {
                CheckHumanAccount();
            }
        }

        private void CheckHumanAccount()
        {
            if (string.IsNullOrWhiteSpace(_model.Name))
                HAErrorMessage = Resources.ShomanNameRequired;
            else
                HAErrorMessage = "";

            _addNewAccount.CanBeExecuted = HAErrorMessage.Length == 0;
        }
    }
}
