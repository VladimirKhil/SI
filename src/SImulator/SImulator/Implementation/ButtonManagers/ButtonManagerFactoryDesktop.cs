using SImulator.Implementation.ButtonManagers.Web;
using SImulator.ViewModel.Model;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel.Core;

namespace SImulator.Implementation.ButtonManagers
{
    internal sealed class ButtonManagerFactoryDesktop : ButtonManagerFactory
    {
        public override IButtonManager Create(AppSettings settings)
        {
            switch (settings.UsePlayersKeys)
            {
                case PlayerKeysModes.Keyboard:
                    return new KeyboardHook();

                case PlayerKeysModes.Joystick:
                    return new JoystickListener();

                case PlayerKeysModes.Com:
                    return new ComButtonManager(settings.ComPort);

#if LEGACY
                case PlayerKeysModes.Web:
                    return new WebManager2(settings.WebPort);
#endif
            }

            return base.Create(settings);
        }
    }
}
