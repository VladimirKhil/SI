using Lingware.Spard.Common;
using Lingware.Spard.Expressions;

namespace QTxtConverter;

/// <summary>
/// Represents read error event arguments.
/// </summary>
public sealed class ReadErrorEventArgs : EventArgs
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
