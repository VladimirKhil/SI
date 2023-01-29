using SICore;
using SIData;
using SIGame.ViewModel.PlatformSpecific;
using System.Windows.Input;

namespace SIGame.ViewModel;

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

    private void SelectPicturePath_Executed(object? arg)
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
