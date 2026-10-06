using SImulator.Properties;
using SImulator.ViewModel.Contracts;

namespace SImulator.Implementation;

/// <summary>
/// Describes the game board shown in an external web browser.
/// </summary>
/// <remarks>
/// Useful where the embedded WebView2 cannot be rendered (e.g. Wine/CrossOver on macOS).
/// The board is available on this computer only.
/// </remarks>
internal sealed class BrowserDisplayDescriptor : IDisplayDescriptor
{
    internal static readonly BrowserDisplayDescriptor Instance = new();

    public string Name => Resources.BrowserScreen;

    public bool IsFullScreen => false;

    public bool IsCustomizable => false;
}
