using Notions;
using System.Collections.Generic;

namespace QTxtConverter
{
    /// <summary>
    /// Строка-комбинация других строк (задаваемых номерами)
    /// </summary>
    public sealed class CombinedString
    {
        private string _content;

        /// <summary>
        /// Получить список номеров строк-источников
        /// </summary>
        public List<int> Sources { get; } = new List<int>();

        /// <summary>
        /// Создание комбинированной строки
        /// </summary>
        /// <param name="s">Содержание</param>
        /// <param name="num">Список номеров строк-источников</param>
        public CombinedString(string s, params int[] num)
        {
            _content = s;
            foreach (var n in num)
            {
                Sources.Add(n);
            }
        }

        /// <summary>
        /// Создание комбинированной строки
        /// </summary>
        /// <param name="s">Список строк-источников</param>
        public CombinedString(params CombinedString[] s)
        {
            var index = 0;
            foreach (CombinedString str in s)
            {
                if (index == 0)
                {
                    _content = str.ToString();
                    foreach (int n in str.Sources)
                    {
                        Sources.Add(n);
                    }
                }
                else
                {
                    CombineWith(str);
                }

                index++;
            }
        }

        private void CombineWith(CombinedString str)
        {
            int len = _content.Length;
            _content = len > 0
                ? StringManager.BestCommonSubString(
                    _content,
                    str._content,
                    new StringManager.StringNorm(StringManager.TemplateSearchingNorm),
                    true)
                : "";

            foreach (int n in str.Sources)
            {
                if (!Sources.Contains(n))
                {
                    Sources.Add(n);
                }
            }
        }

        /// <summary>
        /// Содержание строки
        /// </summary>
        /// <returns>Содержание строки</returns>
        override public string ToString() => _content;
    }
}
