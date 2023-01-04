namespace SIUI.Model;

/// <summary>
/// Defines table information.
/// </summary>
public sealed class TableInfo
{
    /// <summary>
    /// Game themes.
    /// </summary>
    public List<string> GameThemes { get; } = new();

    /// <summary>
    /// Themes information.
    /// </summary>
    public List<ThemeInfo> RoundInfo { get; } = new();

    /// <summary>
    /// Is table paused.
    /// </summary>
    public bool Pause { get; set; }
}
