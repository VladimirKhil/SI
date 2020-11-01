using System.Xml.Serialization;
using SIData;
using System.Runtime.CompilerServices;

namespace SIGame.ViewModel
{
    /// <summary>
    /// Игровой аккаунт
    /// </summary>
    public sealed class GameAccount: SimpleAccount<Account>
    {
        private AccountTypes accountType = AccountTypes.Human;

        public AccountTypes AccountType
        {
            get { return this.accountType; }
            set
            {
                if (this.accountType != value)
                {
                    this.accountType = value;
                    OnPropertyChanged();
                }
            }
        }

        [XmlIgnore]
        public bool IsCreator
        {
            get { return this.SelectedAccount == this.settings.Human; }
        }

        [XmlIgnore]
        public GameSettingsViewModel GameSettings
        {
            get { return this.settings; }
        }

        private GameSettingsViewModel settings;

        public GameAccount(GameSettingsViewModel settings)
        {
            this.settings = settings;
        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            if (propertyName == "SelectedAccount")
                OnPropertyChanged("IsCreator");
        }
    }
}
