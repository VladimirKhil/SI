namespace SIUI.Model;

/// <summary>
/// Defines themes information.
/// </summary>
public sealed class ThemeInfo
{
    /// <summary>
    /// Theme name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Questions information.
    /// </summary>
    public List<QuestionInfo> Questions { get; } = new();
}
