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
            foreach (int n in num)
                Sources.Add(n);
        }

        /// <summary>
        /// Создание комбинированной строки
        /// </summary>
        /// <param name="s">Список строк-источников</param>
        public CombinedString(params CombinedString[] s)
        {
            int r = 0;
            foreach (CombinedString str in s)
            {
                if (r == 0)
                {
                    _content = str.ToString();
                    foreach (int n in str.Sources)
                        Sources.Add(n);
                }
                else
                    CombineWith(str);
                r++;
            }
        }

        private void CombineWith(CombinedString str)
        {
            int len = _content.Length;
            if (len > 0)
                _content = StringManager.BestCommonSubString(this._content, str._content, new StringManager.StringNorm(StringManager.TemplateSearchingNorm), true);
            else
                _content = "";

            foreach (int n in str.Sources)
                if (!Sources.Contains(n))
                    Sources.Add(n);
        }

        /// <summary>
        /// Содержание строки
        /// </summary>
        /// <returns>Содержание строки</returns>
        override public string ToString()
        {
            return _content;
        }
    }
}
