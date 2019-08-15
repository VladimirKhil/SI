namespace SIPackages.Core
{
    public sealed class SearchData
    {
        public string Item { get; set; }
        public int StartIndex { get; set; }
        public int ItemIndex { get; set; }
        public ResultKind Kind { get; set; }

        public SearchData(string item, int startIndex, int itemIndex, ResultKind kind)
        {
            Item = item;
            StartIndex = startIndex;
            ItemIndex = itemIndex;
            Kind = kind;
        }

        public SearchData(string item, int startIndex, ResultKind kind)
        {
            Item = item;
            StartIndex = startIndex;
            Kind = kind;
        }

        public SearchData(string item, int startIndex, int itemIndex)
        {
            Item = item;
            StartIndex = startIndex;
            ItemIndex = itemIndex;
        }
    }
}
