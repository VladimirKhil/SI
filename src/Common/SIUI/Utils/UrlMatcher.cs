using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SIUI.Utils
{
    public static class UrlMatcher
    {
        private static readonly Regex UrlRegex = new Regex(@"https?:\/\/[_a-z0-9.\/-]+", RegexOptions.Compiled);

        public static IEnumerable<Match> MatchText(string text)
        {
            return UrlRegex.Matches(text).Cast<Match>();
        }
    }
}
