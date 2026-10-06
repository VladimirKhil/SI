using Avalonia;
using Avalonia.Platform;
using SImulator.Properties;
using SImulator.ViewModel.Contracts;

namespace SImulator.Implementation;

/// <summary>
/// Describes a display (full screen board) or a windowed board.
/// </summary>
internal sealed class MacScreenDescriptor : IDisplayDescriptor
{
    internal static readonly MacScreenDescriptor WindowScreen = new(Resources.WebView, false, null);

    public string Name { get; }

    public bool IsFullScreen { get; }

    public bool IsCustomizable => false;

    /// <summary>
    /// Screen bounds in pixels (for full screen boards).
    /// </summary>
    internal PixelRect? Bounds { get; }

    private MacScreenDescriptor(string name, bool isFullScreen, PixelRect? bounds)
    {
        Name = name;
        IsFullScreen = isFullScreen;
        Bounds = bounds;
    }

    public MacScreenDescriptor(Screen screen)
        : this(screen.IsPrimary ? Resources.MainScreen : Resources.SecondaryScreen, true, screen.Bounds)
    {
    }
}
