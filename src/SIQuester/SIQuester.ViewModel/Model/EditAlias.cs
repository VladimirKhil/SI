namespace SIQuester.Model;

/// <summary>
/// Defines a localized and colored alias for a template.
/// </summary>
public sealed class EditAlias
{
    /// <summary>
    /// Localized template name.
    /// </summary>
    public string VisibleName { get; private set; }

    /// <summary>
    /// Template color.
    /// </summary>
    public string Color { get; private set; }

    public EditAlias(string visibleName, string color)
    {
        VisibleName = visibleName;
        Color = color;
    }
}
