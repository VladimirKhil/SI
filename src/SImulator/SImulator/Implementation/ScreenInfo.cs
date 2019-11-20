using SImulator.ViewModel.PlatformSpecific;
using System.Windows.Forms;

namespace SImulator.Implementation
{
    public sealed class ScreenInfo: IScreen
    {
        public Screen Screen { get; set; }
        public bool IsRemote { get; set; }

        public string Name
        {
            get
            {
                if (IsRemote)
                    return "Другой компьютер";

                if (Screen == null)
                    return "Окно";
                
                return Screen == Screen.PrimaryScreen ? "Основной" : "Дополнительный";
            }
        }

        public ScreenInfo(Screen screen)
        {
            Screen = screen;
        }
    }
}
