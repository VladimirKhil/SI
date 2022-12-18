namespace SIQuester.ViewModel;

/// <summary>
/// Defines an external link view model.
/// </summary>
public sealed class LinkViewModel
{
    /// <summary>
    /// Link title.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Link url.
    /// </summary>
    public string Uri { get; set; }

    /// <summary>
    /// Does the link support multiple lines.
    /// </summary>
    public bool IsMultiline { get; set; }
}
