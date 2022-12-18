using System.Text;

namespace SIPackages.Core;

/// <summary>
/// Provides extension methods for string lists.
/// </summary>
public static class ListExtensions
{
    /// <summary>
    /// Converts list to normalized string.
    /// </summary>
    public static string ToCommonString(this List<string> list)
    {
        var text = new StringBuilder();
        text.Append('(');

        for (var i = 0; i < list.Count; i++)
        {
            if (i > 0)
            {
                text.Append(", ");
            }

            text.Append(list[i]);
        }

        text.Append(')');

        return text.ToString();
    }

    /// <summary>
    /// Checks if any of list items contains the provided pattern.
    /// </summary>
    /// <param name="list">List to check.</param>
    /// <param name="pattern">Pattern to find.</param>
    public static bool ContainsQuery(this List<string> list, string pattern) =>
        list.Any(item => item.IndexOf(pattern, StringComparison.CurrentCultureIgnoreCase) > -1);

    /// <summary>
    /// Searches pattern in list values.
    /// </summary>
    /// <param name="list">List to search.</param>
    /// <param name="pattern">Searched pattern.</param>
    /// <returns>Search result.</returns>
    public static IEnumerable<SearchData> Search(this List<string> list, string pattern)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var index = list[i].IndexOf(pattern, StringComparison.CurrentCultureIgnoreCase);
            if (index > -1)
            {
                yield return new SearchData(list[i], index, i);
            }
        }
    }
}
