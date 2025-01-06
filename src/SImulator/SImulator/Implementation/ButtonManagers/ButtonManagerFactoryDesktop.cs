using SImulator.Implementation.ButtonManagers.WebNew;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel.Core;
using SImulator.ViewModel.Model;

namespace SImulator.Implementation.ButtonManagers;

/// <inheritdoc cref="ButtonManagerFactory" />
internal sealed class ButtonManagerFactoryDesktop : ButtonManagerFactory
{
    public override IButtonManager? Create(AppSettings settings, IButtonManagerListener buttonManagerListener) =>
        settings.UsePlayersKeys switch
        {
            PlayerKeysModes.Keyboard => new KeyboardHook(buttonManagerListener),
            PlayerKeysModes.Joystick => new JoystickListener(buttonManagerListener),
            PlayerKeysModes.Com => new ComButtonManager(settings.ComPort, buttonManagerListener),
            PlayerKeysModes.WebNew => new WebManagerNew(settings.WebPort, buttonManagerListener),
            _ => base.Create(settings, buttonManagerListener),
        };
}
