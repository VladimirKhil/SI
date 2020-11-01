using System.Windows.Input;
using SIData;
using SICore;

namespace SIGame.ViewModel
{
    /// <summary>
    /// Аккаунт участника
    /// </summary>
    public class AccountViewModel<TAccount>: ViewModel<TAccount>
        where TAccount: Account, new()
    {
        /// <summary>
        /// Выбрать адрес изображения
        /// </summary>
        public ICommand SelectPicturePath { get; private set; }

        public AccountViewModel()
        {
            
        }

        public AccountViewModel(TAccount account)
            : base(account)
        {
            
        }

        protected override void Initialize()
        {
            base.Initialize();

            SelectPicturePath = new CustomCommand(SelectPicturePath_Executed);
        }

        private void SelectPicturePath_Executed(object arg)
        {
            var avatar = PlatformSpecific.PlatformManager.Instance.SelectHumanAvatar();
            if (avatar != null)
                _model.Picture = avatar;
        }
    }
}
