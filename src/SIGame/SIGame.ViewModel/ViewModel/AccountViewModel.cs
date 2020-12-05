using System.Windows.Input;
using SIData;
using SICore;
using System;
using SIGame.ViewModel.PlatformSpecific;

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
            try
            {
                var avatar = PlatformManager.Instance.SelectHumanAvatar();
                if (avatar != null)
                {
                    _model.Picture = avatar;
                }
            }
            catch (Exception exc)
            {
                PlatformManager.Instance.ShowMessage(exc.Message, MessageType.Warning, true);
            }
        }
    }
}
