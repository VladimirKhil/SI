using Lingware.Spard.Common;
using Lingware.Spard.Expressions;
using System;

namespace QTxtConverter
{
    public sealed class ReadErrorEventArgs: EventArgs
    {
        public bool Cancel { get; set; }
        public bool Skip { get; set; }
        public MatchInfo<char> BestTry { get; set; }
        public Tuple<int, int> Index { get; set; }
        public Expression NotReaded { get; set; }
        public Expression Missing { get; set; }

        public int Move { get; set; }

        public ReadErrorEventArgs() => Cancel = Skip = false;
    }
}
