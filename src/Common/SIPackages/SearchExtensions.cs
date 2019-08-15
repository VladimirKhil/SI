using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIPackages
{
    /// <summary>
    /// Класс, обеспечивающий поиск текста в пакете
    /// </summary>
    public static class SearchExtensions
    {
        public static SearchMatch SearchFragment(this InfoOwner item, string value)
        {
            var result = item.Search(value).FirstOrDefault();
            if (result == null)
                return null;

            var match = result.Item;
            var diff = match.Length - result.StartIndex - value.Length;
            return new SearchMatch(
                result.StartIndex > 0 ? match.Substring(0, result.StartIndex) : "",
                match.Substring(result.StartIndex, value.Length),
                diff > 0 ? match.Substring(result.StartIndex + value.Length, diff) : ""
                );
        }

        internal static IEnumerable<SearchData> Search(ResultKind kind, string str, string text)
        {
            if (str == null)
                yield break;

            var index = str.IndexOf(text, StringComparison.CurrentCultureIgnoreCase);
            if (index == -1)
                yield break;

            yield return new SearchData(str, index, kind);
        }

        public static IEnumerable<SearchData> Search(this Atom atom, string value)
        {
            return Search(ResultKind.Text, atom.Text, value);
        }
    }
}
