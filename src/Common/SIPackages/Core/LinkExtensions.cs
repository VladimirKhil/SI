namespace SIPackages.Core;

/// <summary>
/// Provides helper methods for working with document links.
/// </summary>
public static class LinkExtensions
{
    /// <summary>
    /// Extracts link from value.
    /// </summary>
    /// <param name="value">Text with format of "@link#tail".</param>
    /// <param name="useTail">Should the link tail (part after #) be used.</param>
    /// <returns>Extracted link.</returns>
    public static string ExtractLink(this string value, bool useTail = false)
    {
        if (value.Length < 2 || value[0] != '@')
        {
            return "";
        }

        if (!useTail)
        {
            return value[1..];
        }

        var ind = value.IndexOf('#');

        if (ind == 1)
        {
            return "";
        }

        return ind == -1 ? value[1..] : value[1..ind];
    }

    /// <summary>
    /// Выделить текст ссылки из строки
    /// </summary>
    /// <param name="s">Строка текста в формате @link#tail</param>
    /// <param name="tail">"Хвост" ссылки tail</param>
    /// <returns>Ссылка link</returns>
    internal static string ExtractLink(this string s, out string tail)
    {
        tail = "";

        if (s.Length == 0 || s[0] != '@' || s.Length < 2)
        {
            return "";
        }

        var ind = s.IndexOf('#');

        if (ind == 1)
        {
            return "";
        }

        if (ind > -1)
        {
            tail = s[(ind + 1)..];
        }

        return ind == -1 ? s[1..] : s[1..ind];
    }
}
