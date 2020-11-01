using System;
using System.Linq;
using System.Windows.Input;
using SICore;
using SIGame.ViewModel.Properties;

namespace SIGame.ViewModel
{
    public sealed class HumanAccountViewModel: AccountViewModel<HumanAccount>
    {
        private string haErrorMessage = "";

        public string HAErrorMessage
        {
            get { return this.haErrorMessage; }
            set
            {
                if (this.haErrorMessage != value)
                {
                    this.haErrorMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool isEdit;

        public bool IsEdit
        {
            get { return this.isEdit; }
            set
            {
                if (this.isEdit != value)
                {
                    this.isEdit = value;
                    OnPropertyChanged();
                    CheckHumanAccount();
                }
            }
        }

        public string CommitHeader
        {
            get { return IsEdit ? Resources.Save : Resources.Create; }
        }

        private CustomCommand addNewAccount;

        public ICommand AddNewAccount { get { return this.addNewAccount; } }

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

            this.addNewAccount = new CustomCommand(AddNewAccount_Executed);

            this._model.PropertyChanged += model_PropertyChanged;
            CheckHumanAccount();
        }

        private void AddNewAccount_Executed(object arg)
        {
            if (IsEdit)
                Edit?.Invoke();
            else
                Add?.Invoke();
        }

        void model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HumanAccount.Name) || e.PropertyName == nameof(HumanAccount.BirthDate))
            {
                CheckHumanAccount();
            }
        }

        private void CheckHumanAccount()
        {
            if (string.IsNullOrWhiteSpace(this._model.Name))
                this.HAErrorMessage = Resources.NameRequired;
            else if (!IsEdit && CommonSettings.Default.Humans2.Any(acc => acc.Name == this._model.Name))
                this.HAErrorMessage = Resources.AlreadyExists;
            else if (!this._model.BirthDate.HasValue)
                this.HAErrorMessage = Resources.BirthDateRequired;
            else
                this.HAErrorMessage = "";

            this.addNewAccount.CanBeExecuted = this.HAErrorMessage.Length == 0;
        }
    }
}
