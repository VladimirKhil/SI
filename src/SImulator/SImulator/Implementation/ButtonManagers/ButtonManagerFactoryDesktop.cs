using SImulator.Implementation.ButtonManagers.Web;
using SImulator.ViewModel.Model;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel.Core;

namespace SImulator.Implementation.ButtonManagers
{
    /// <inheritdoc cref="ButtonManagerFactory" />
    internal sealed class ButtonManagerFactoryDesktop : ButtonManagerFactory
    {
        public override IButtonManager Create(AppSettings settings) =>
            settings.UsePlayersKeys switch
            {
                PlayerKeysModes.Keyboard => new KeyboardHook(),
                PlayerKeysModes.Joystick => new JoystickListener(),
                PlayerKeysModes.Com => new ComButtonManager(settings.ComPort),
                PlayerKeysModes.Web => new WebManager(settings.WebPort),
                _ => base.Create(settings),
            };
    }
}
