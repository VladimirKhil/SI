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
    /// Screen working area in pixels (without the menu bar and the Dock), for full screen boards.
    /// </summary>
    internal PixelRect? Bounds { get; }

    /// <summary>
    /// Screen scaling (physical pixels per logical point).
    /// </summary>
    internal double Scaling { get; }

    private MacScreenDescriptor(string name, bool isFullScreen, PixelRect? bounds, double scaling = 1.0)
    {
        Name = name;
        IsFullScreen = isFullScreen;
        Bounds = bounds;
        Scaling = scaling;
    }

    public MacScreenDescriptor(Screen screen)
        : this(screen.IsPrimary ? Resources.MainScreen : Resources.SecondaryScreen, true, screen.WorkingArea, screen.Scaling)
    {
    }
}
