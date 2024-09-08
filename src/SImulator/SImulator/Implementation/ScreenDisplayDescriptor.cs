using SImulator.Properties;
using SImulator.ViewModel.Contracts;
using System.Windows.Forms;

namespace SImulator.Implementation;

/// <inheritdoc cref="IDisplayDescriptor" />.
public sealed class ScreenDisplayDescriptor : IDisplayDescriptor
{
    /// <summary>
    /// Screen information.
    /// </summary>
    public Screen Screen { get; set; }

    public string Name => $"{(Screen == Screen.PrimaryScreen ? Resources.MainScreen : Resources.SecondaryScreen)} ({Resources.OldVersion})";

    public bool IsFullScreen => true;

    /// <summary>
    /// Initializes a new instance if <see cref="ScreenDisplayDescriptor" />.
    /// </summary>
    /// <param name="screen">Screen information.</param>
    public ScreenDisplayDescriptor(Screen screen) => Screen = screen;
}
