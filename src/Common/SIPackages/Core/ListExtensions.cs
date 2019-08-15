using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIPackages.Core
{
    /// <summary>
    /// Функции для работы со строковыми списками
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Строковое представление списка
        /// </summary>
        /// <returns></returns>
        public static string ToCommonString(this List<string> list)
        {
            var text = new StringBuilder();
            text.Append('(');
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0)
                    text.Append(", ");

                text.Append(list[i]);
            }

            text.Append(')');
            return text.ToString();
        }

        public static bool ContainsQuery(this List<string> list, string pattern)
        {
            return list.Any(item => item.IndexOf(pattern, StringComparison.CurrentCultureIgnoreCase) > -1);
        }

        public static IEnumerable<SearchData> Search(this List<string> list, string pattern)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var index = list[i].IndexOf(pattern, StringComparison.CurrentCultureIgnoreCase);
                if (index > -1)
                    yield return new SearchData(list[i], index, i);
            }
        }
    }
}
