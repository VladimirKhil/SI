using SIPackages.Core;

namespace SIQuester.Model;

/// <summary>
/// Represents a search match inside a file.
/// </summary>
public sealed class SearchResult
{
    /// <summary>
    /// File name.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Found fragment with highlighted area.
    /// </summary>
    public SearchMatch Fragment { get; set; }
}
