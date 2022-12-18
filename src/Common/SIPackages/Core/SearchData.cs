namespace SIPackages.Core;

/// <summary>
/// Defines a search result data.
/// </summary>
public sealed class SearchData
{
    /// <summary>
    /// Item containing the search string.
    /// </summary>
    public string Item { get; set; }

    /// <summary>
    /// Start index of search string inside <see cref="Item" />.
    /// </summary>
    public int StartIndex { get; set; }

    /// <summary>
    /// Item index inside its owning collection.
    /// </summary>
    public int ItemIndex { get; set; }

    /// <summary>
    /// Result source kind.
    /// </summary>
    public ResultKind Kind { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="SearchData" /> class.
    /// </summary>
    /// <param name="item">Item containing the search string.</param>
    /// <param name="startIndex">Start index of search string inside <paramref name="item"/>.</param>
    /// <param name="itemIndex">Item index inside its owning collection.</param>
    /// <param name="kind">Result source kind.</param>
    public SearchData(string item, int startIndex, int itemIndex, ResultKind kind)
    {
        Item = item;
        StartIndex = startIndex;
        ItemIndex = itemIndex;
        Kind = kind;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SearchData" /> class.
    /// </summary>
    /// <param name="item">Item containing the search string.</param>
    /// <param name="startIndex">Start index of search string inside <paramref name="item"/>.</param>
    /// <param name="kind">Result source kind.</param>
    public SearchData(string item, int startIndex, ResultKind kind)
    {
        Item = item;
        StartIndex = startIndex;
        Kind = kind;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SearchData" /> class.
    /// </summary>
    /// <param name="item">Item containing the search string.</param>
    /// <param name="startIndex">Start index of search string inside <paramref name="item"/>.</param>
    /// <param name="itemIndex">Item index inside its owning collection.</param>
    public SearchData(string item, int startIndex, int itemIndex)
    {
        Item = item;
        StartIndex = startIndex;
        ItemIndex = itemIndex;
    }
}
