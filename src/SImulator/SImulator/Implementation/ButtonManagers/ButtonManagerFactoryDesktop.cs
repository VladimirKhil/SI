using SImulator.Implementation.ButtonManagers.WebNew;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel.Contracts;
using SImulator.ViewModel.Core;
using SImulator.ViewModel.Model;

namespace SImulator.Implementation.ButtonManagers;

/// <inheritdoc cref="ButtonManagerFactory" />
internal sealed class ButtonManagerFactoryDesktop : ButtonManagerFactory
{
    public override async Task<IButtonManager?> CreateAsync(AppSettings settings, IButtonManagerListener buttonManagerListener, IPlatformService platformService) =>
        settings.UsePlayersKeys switch
        {
            PlayerKeysModes.Keyboard => new KeyboardHook(buttonManagerListener),
            PlayerKeysModes.Joystick => new JoystickListener(buttonManagerListener),
            PlayerKeysModes.Com => new ComButtonManager(settings.ComPort, buttonManagerListener),
            PlayerKeysModes.WebNew => await WebManagerNew.CreateAsync(settings.WebPort, buttonManagerListener, platformService),
            _ => await base.CreateAsync(settings, buttonManagerListener, platformService),
        };
}
