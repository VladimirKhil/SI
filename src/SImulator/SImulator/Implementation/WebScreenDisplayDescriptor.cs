using SImulator.Implementation.WinAPI;
using SImulator.Properties;
using SImulator.ViewModel.Contracts;

namespace SImulator.Implementation;

/// <inheritdoc cref="IDisplayDescriptor" />.
internal sealed class WebScreenDisplayDescriptor : IDisplayDescriptor
{
    public int Left { get; }

    public int Top { get; }

    public int Width { get; }

    public int Height { get; }

    public bool IsPrimary { get; }

    public string Name => IsPrimary ? Resources.MainScreen : Resources.SecondaryScreen;

    public bool IsFullScreen => true;

    public bool IsWebView => true;

    public bool IsCustomizable => false;

    /// <summary>
    /// Initializes a new instance if <see cref="WebScreenDisplayDescriptor" />.
    /// </summary>
    /// <param name="screen">Screen information.</param>
    internal WebScreenDisplayDescriptor(DisplayInfo screen)
    {
        Left = screen.Left;
        Top = screen.Top;
        Width = screen.Width;
        Height = screen.Height;
        IsPrimary = screen.IsPrimary;
    }
}
