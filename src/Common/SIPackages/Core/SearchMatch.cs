namespace SIPackages.Core
{
    public sealed class SearchMatch
    {
        public ResultKind Kind { get; set; }

        public string Begin { get; set; }
        public string Match { get; set; }
        public string End { get; set; }

        public SearchMatch(string begin, string match, string end)
        {
            Begin = begin;
            Match = match;
            End = end;
        }
    }
}
