using SImulator.ViewModel.Model;
using SImulator.ViewModel.Core;

namespace SImulator.ViewModel.ButtonManagers
{
    /// <summary>
    /// Фабрика кнопочных менеджеров
    /// </summary>
    public class ButtonManagerFactory
    {
        public virtual IButtonManager Create(AppSettings settings)
        {
            switch (settings.UsePlayersKeys)
            {
                case PlayerKeysModes.External:
                    return new EmptyButtonManager();

                default:
                    return null;
            }
        }
    }
}
