using SImulator.Implementation.ButtonManagers.WebNew;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel.Core;
using SImulator.ViewModel.Model;
using System.Threading.Tasks;

namespace SImulator.Implementation.ButtonManagers;

/// <inheritdoc cref="ButtonManagerFactory" />
internal sealed class ButtonManagerFactoryDesktop : ButtonManagerFactory
{
    public override async Task<IButtonManager?> CreateAsync(AppSettings settings, IButtonManagerListener buttonManagerListener) =>
        settings.UsePlayersKeys switch
        {
            PlayerKeysModes.Keyboard => new KeyboardHook(buttonManagerListener),
            PlayerKeysModes.Joystick => new JoystickListener(buttonManagerListener),
            PlayerKeysModes.Com => new ComButtonManager(settings.ComPort, buttonManagerListener),
            PlayerKeysModes.WebNew => await WebManagerNew.CreateAsync(settings.WebPort, buttonManagerListener),
            _ => await base.CreateAsync(settings, buttonManagerListener),
        };
}
