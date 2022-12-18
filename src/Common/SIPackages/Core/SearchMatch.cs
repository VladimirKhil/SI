namespace SIPackages.Core;

/// <summary>
/// Respresent a string containing search result splitted by this result.
/// </summary>
public sealed class SearchMatch
{
    /// <summary>
    /// Search result kind.
    /// </summary>
    public ResultKind Kind { get; set; }

    /// <summary>
    /// String part before match.
    /// </summary>
    public string Begin { get; set; }

    /// <summary>
    /// Matched search result.
    /// </summary>
    public string Match { get; set; }

    /// <summary>
    /// String part after match.
    /// </summary>
    public string End { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="SearchMatch" /> class.
    /// </summary>
    /// <param name="begin">String part before match.</param>
    /// <param name="match">Matched search result.</param>
    /// <param name="end">String part after match.</param>
    public SearchMatch(string begin, string match, string end)
    {
        Begin = begin;
        Match = match;
        End = end;
    }
}
