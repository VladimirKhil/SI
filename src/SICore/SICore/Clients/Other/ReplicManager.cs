using System;
using System.Text;

namespace SICore
{
    internal static class ReplicManager
    {
        private static readonly char[] _escapeChars = new char[] { '<', '>', '\"', '\'', '&' };
        private static readonly string[] _escapeStrings = new string[] { "&lt;", "&gt;", "&quot;", "&apos;", "&amp;" };

        internal static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "";
            }

            var result = new StringBuilder();

            var length = s.Length;
            for (int i = 0; i < length; i++)
            {
                var c = s[i];
                var ind = Array.IndexOf(_escapeChars, c);
                if (ind > -1)
                    result.Append(_escapeStrings[ind]);
                else
                    result.Append(c);
            }

            return result.ToString();
        }

        /// <summary>
        /// Реплика
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string Replic(string s)
        {
            return string.Format("<replic>{0}</replic>{1}", Escape(s), Line());
        }

        /// <summary>
        /// Реплика
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string OldReplic(string s)
        {
            return string.Format("<replic old=\"true\">{0}</replic>{1}", Escape(s), Line());
        }

        /// <summary>
        /// Системное сообщение
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string System(string s)
        {
            return string.Format("<system>{0}</system>{1}", Escape(s), Line());
        }

        /// <summary>
        /// Специальное сообщение
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string Special(string s)
        {
            return string.Format("<special>{0}</special>{1}", Escape(s), Line());
        }

        /// <summary>
        /// Переход на новую строку
        /// </summary>
        /// <returns></returns>
        public static string Line()
        {
            return "<line />";
        }
    }
}
