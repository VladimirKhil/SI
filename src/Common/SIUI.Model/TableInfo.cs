namespace SIUI.Model;

/// <summary>
/// Defines table information.
/// </summary>
public sealed class TableInfo
{
    /// <summary>
    /// Game themes.
    /// </summary>
    public List<string> GameThemes { get; } = new List<string>();

    /// <summary>
    /// Themes information.
    /// </summary>
    public List<ThemeInfo> RoundInfo { get; } = new List<ThemeInfo>();

    /// <summary>
    /// Is table paused.
    /// </summary>
    public bool Pause { get; set; }
}
