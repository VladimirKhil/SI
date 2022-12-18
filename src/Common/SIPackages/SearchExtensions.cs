using SIPackages.Core;

namespace SIPackages;

/// <summary>
/// Provides search helper methods.
/// </summary>
public static class SearchExtensions
{
    /// <summary>
    /// Searches a value inside the object.
    /// </summary>
    /// <param name="item">Object to search within.</param>
    /// <param name="value">Value to search.</param>
    /// <returns>Search result or null.</returns>
    public static SearchMatch? SearchFragment(this InfoOwner item, string value)
    {
        var result = item.Search(value).FirstOrDefault();

        if (result == null)
        {
            return null;
        }

        var match = result.Item;
        var diff = match.Length - result.StartIndex - value.Length;

        return new SearchMatch(
            result.StartIndex > 0 ? match[..result.StartIndex] : "",
            match.Substring(result.StartIndex, value.Length),
            diff > 0 ? match.Substring(result.StartIndex + value.Length, diff) : "");
    }

    internal static IEnumerable<SearchData> Search(ResultKind kind, string str, string text)
    {
        if (str == null)
        {
            yield break;
        }

        var index = str.IndexOf(text, StringComparison.CurrentCultureIgnoreCase);

        if (index == -1)
        {
            yield break;
        }

        yield return new SearchData(str, index, kind);
    }

    /// <summary>
    /// Searches a value inside <see cref="Atom" />.
    /// </summary>
    /// <param name="atom">Atom to search within.</param>
    /// <param name="value">Value to search.</param>
    /// <returns>Search results.</returns>
    public static IEnumerable<SearchData> Search(this Atom atom, string value) => Search(ResultKind.Text, atom.Text, value);
}
