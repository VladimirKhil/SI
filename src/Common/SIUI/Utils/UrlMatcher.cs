using System.Text.RegularExpressions;

namespace SIUI.Utils;

public static class UrlMatcher
{
    private static readonly Regex UrlRegex = new(@"https?:\/\/[_a-z0-9.\/-]+", RegexOptions.Compiled);

    public static IEnumerable<Match> MatchText(string text) => UrlRegex.Matches(text).Cast<Match>();
}
