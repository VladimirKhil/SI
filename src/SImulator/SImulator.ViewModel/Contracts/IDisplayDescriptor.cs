namespace SImulator.ViewModel.Contracts;

/// <summary>
/// Provides display info.
/// </summary>
public interface IDisplayDescriptor
{
    /// <summary>
    /// Display name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Full screen marker.
    /// </summary>
    bool IsFullScreen { get; }

    /// <summary>
    /// Should the screen display a web UI.
    /// </summary>
    bool IsWebView => false;
}
