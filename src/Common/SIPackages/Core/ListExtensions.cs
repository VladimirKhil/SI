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
}
