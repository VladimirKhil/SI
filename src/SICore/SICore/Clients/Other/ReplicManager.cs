using System;
using System.Text;

namespace SICore
{
    /// <summary>
    /// Contains methods for generating replic messages.
    /// </summary>
    internal static class ReplicManager
    {
        private static readonly char[] _escapeChars = new char[] { '<', '>', '\"', '\'', '&' };
        private static readonly string[] _escapeStrings = new string[] { "&lt;", "&gt;", "&quot;", "&apos;", "&amp;" };

        /// <summary>
        /// Line break.
        /// </summary>
        private const string _Line = "<line />";

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
        /// Системное сообщение
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string System(string s) => string.Format("<system>{0}</system>{1}", Escape(s), _Line);

        /// <summary>
        /// Специальное сообщение
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string Special(string s) => string.Format("<special>{0}</special>{1}", Escape(s), _Line);
    }
}
