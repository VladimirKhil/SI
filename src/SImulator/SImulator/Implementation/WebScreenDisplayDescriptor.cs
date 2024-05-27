using SImulator.Properties;
using SImulator.ViewModel.Contracts;
using System.Windows.Forms;

namespace SImulator.Implementation;

/// <inheritdoc cref="IDisplayDescriptor" />.
public sealed class WebScreenDisplayDescriptor : IDisplayDescriptor
{
    /// <summary>
    /// Screen information.
    /// </summary>
    public Screen Screen { get; set; }

    public string Name => $"{(Screen == Screen.PrimaryScreen ? Resources.MainScreen : Resources.SecondaryScreen)} ({Resources.NewVersion})";

    public bool IsFullScreen => true;

    public bool IsWebView => true;

    /// <summary>
    /// Initializes a new instance if <see cref="WebScreenDisplayDescriptor" />.
    /// </summary>
    /// <param name="screen">Screen information.</param>
    public WebScreenDisplayDescriptor(Screen screen) => Screen = screen;
}
