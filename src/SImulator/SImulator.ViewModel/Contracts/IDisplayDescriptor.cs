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
    /// Can the screen be customized.
    /// </summary>
    bool IsCustomizable => true;
}
