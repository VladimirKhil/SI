namespace Utils.Wpf.ViewModels;

public sealed class InputTextViewModel
{
    /// <summary>
    /// Link title.
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// Link url.
    /// </summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// Does the link support multiple lines.
    /// </summary>
    public bool IsMultiline { get; set; }
}
