namespace SIPackages.Core
{
    /// <summary>
    /// Provides helper methods for working with document links.
    /// </summary>
    internal static class LinkExtensions
    {
        /// <summary>
        /// Выделить текст ссылки из строки
        /// </summary>
        /// <param name="s">Строка текста в формате @link#tail</param>
        /// <param name="useTail">Should link tail (part after #) be used.</param>
        /// <returns>Ссылка link</returns>
        internal static string ExtractLink(this string s, bool useTail = false)
        {
            if (s.Length < 2 || s[0] != '@')
                return "";

            if (!useTail)
                return s.Substring(1);

            int ind = s.IndexOf('#');
            if (ind == 1)
                return "";

            return ind == -1 ? s.Substring(1) : s.Substring(1, ind - 1);
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
                return "";

            int ind = s.IndexOf('#');
            if (ind == 1)
                return "";

            if (ind > -1)
                tail = s.Substring(ind + 1);

            return ind == -1 ? s.Substring(1) : s.Substring(1, ind - 1);
        }
    }
}
