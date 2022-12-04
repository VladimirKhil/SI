using SImulator.Properties;
using SImulator.ViewModel.PlatformSpecific;
using System.Windows.Forms;

namespace SImulator.Implementation;

/// <inheritdoc cref="IScreen" />.
public sealed class ScreenInfo : IScreen
{
    /// <summary>
    /// Screen information.
    /// </summary>
    public Screen Screen { get; set; }

    public string Name =>
        Screen == null
            ? Resources.Window
            : Screen == Screen.PrimaryScreen ? Resources.MainScreen : Resources.SecondaryScreen;

    /// <summary>
    /// Initializes a new instance if <see cref="ScreenInfo" />.
    /// </summary>
    /// <param name="screen">Screen information.</param>
    public ScreenInfo(Screen screen) => Screen = screen;
}
