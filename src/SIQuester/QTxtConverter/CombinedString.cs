using System;
using System.Collections.Generic;
using System.Text;
using Notions;

namespace QTxtConverter
{
    /// <summary>
    /// Строка-комбинация других строк (задаваемых номерами)
    /// </summary>
    public sealed class CombinedString
    {
        string content;
        List<int> sources = new List<int>();

        /// <summary>
        /// Создание комбинированной строки
        /// </summary>
        /// <param name="s">Содержание</param>
        /// <param name="num">Список номеров строк-источников</param>
        public CombinedString(string s, params int[] num)
        {
            content = s;
            foreach (int n in num)
                sources.Add(n);
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
                    content = str.ToString();
                    foreach (int n in str.Sources)
                        sources.Add(n);
                }
                else
                    CombineWith(str);
                r++;
            }
        }

        private void CombineWith(CombinedString str)
        {
            int len = content.Length;
            if (len > 0)
                content = StringManager.BestCommonSubString(this.content, str.content, new StringManager.StringNorm(StringManager.TemplateSearchingNorm), true);
            else
                content = "";

            foreach (int n in str.Sources)
                if (!sources.Contains(n))
                    sources.Add(n);
        }

        /// <summary>
        /// Содержание строки
        /// </summary>
        /// <returns>Содержание строки</returns>
        override public string ToString()
        {
            return content;
        }

        /// <summary>
        /// Получить список номеров строк-источников
        /// </summary>
        public List<int> Sources
        {
            get
            {
                return sources;
            }
        }
    }
}
