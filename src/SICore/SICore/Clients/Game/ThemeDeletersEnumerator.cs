using System.Text;

namespace SICore.Clients.Game;

/// <summary>
/// Enumerates players that delete final themes.
/// </summary>
public sealed class ThemeDeletersEnumerator
{
    /// <summary>
    /// Contains index info of next player to delete theme.
    /// If next player is undefined, contains a set of possible player indicies.
    /// </summary>
    public sealed class IndexInfo
    {
        /// <summary>
        /// Current player index.
        /// </summary>
        public int PlayerIndex { get; set; }

        /// <summary>
        /// Contains possible values for <see cref="PlayerIndex" /> when it is not selected yet.
        /// Only one value should be selected after all.
        /// </summary>
        public HashSet<int> PossibleIndicies { get; private set; }

        public IndexInfo(int index)
        {
            if (index < 0)
            {
                throw new ArgumentException($"Invalid value: {index}", nameof(index));
            }

            PlayerIndex = index;
            PossibleIndicies = new HashSet<int>();
        }

        public IndexInfo(HashSet<int> indicies)
        {
            PlayerIndex = -1;
            PossibleIndicies = indicies; // IndexInfo instances share common PossibleIndicies
        }

        public void SetIndex(int index)
        {
            if (PlayerIndex != -1)
            {
                throw new InvalidOperationException($"Index is already set. PlayerIndex: {PlayerIndex}; index: {index}");
            }

            if (index == -1)
            {
                throw new ArgumentException("Wrong index value", nameof(index));
            }

            if (!PossibleIndicies.Contains(index))
            {
                throw new InvalidOperationException($"Invalid player index {index}. Valid indicies are ({string.Join(", ", PossibleIndicies)})");
            }

            PlayerIndex = index;
            PossibleIndicies.Remove(index);
        }

        public override string ToString() => PlayerIndex == -1 ? $"[{string.Join("|", PossibleIndicies)}]" : PlayerIndex.ToString();
    }

    /// <summary>
    /// Making stakes/theme deleting order.
    /// </summary>
    private IndexInfo[] _order;

    /// <summary>
    /// Current enumerator position in <see cref="_order" />.
    /// </summary>
    private int _orderIndex;

    /// <summary>
    /// Является ли перебор циклическим (с конца идём в начало)
    /// </summary>
    private bool _cycle;

    /// <summary>
    /// Current enumerator item.
    /// </summary>
    public IndexInfo Current => _order[_orderIndex];

    /// <summary>
    /// Initializes a new instance of <see cref="ThemeDeletersEnumerator" /> class.
    /// </summary>
    /// <param name="players">Game players.</param>
    /// <param name="themesCount">Numbers of themes in round.</param>
    /// <exception cref="InvalidOperationException">Invalid players collection has been provided.</exception>
    public ThemeDeletersEnumerator(IList<GamePlayerAccount> players, int themesCount)
    {
        if (themesCount <= 0)
        {
            throw new ArgumentException("Value must be positive", nameof(themesCount));
        }

        var playersCount = players.Count;

        // The player with the highest score should be the last to remove the theme
        var goodPlayers = players.Where(p => p.InGame).ToArray();
        var deletersCount = goodPlayers.Length;

        if (deletersCount == 0)
        {
            throw new InvalidOperationException("No InGame players found");
        }

        _order = new IndexInfo[deletersCount];

        var leftThemesCount = (themesCount - 1) % deletersCount;

        if (leftThemesCount == 0)
        {
            leftThemesCount = deletersCount;
        }

        // Split players to classes according to their sums
        var levels = goodPlayers.GroupBy(p => p.Sum).OrderByDescending(g => g.Key).ToArray();
        var k = leftThemesCount - 1;

        for (var levelIndex = 0; levelIndex < levels.Length; levelIndex++)
        {
            var level = levels[levelIndex];

            if (level.Count() == 1)
            {
                _order[k--] = new IndexInfo(players.IndexOf(level.First()));

                if (k == -1)
                {
                    k += deletersCount;
                }
            }
            else
            {
                var indicies = new HashSet<int>();

                foreach (var item in level)
                {
                    indicies.Add(players.IndexOf(item));
                    _order[k--] = new IndexInfo(indicies);

                    if (k == -1)
                    {
                        k += deletersCount;
                    }
                }
            }
        }
    }

    public ThemeDeletersEnumerator(IndexInfo[] order)
    {
        _order = order;
    }

    private readonly List<string> _removeLog = new(); // Temporary object to catch errors

    public string GetRemoveLog() => string.Join(Environment.NewLine, _removeLog);

    public void RemoveAt(int index)
    {
        var removeLog = new StringBuilder(ToString()).Append(' ').Append(index);

        _removeLog.Add("Before: " + removeLog.ToString());

        var processedPossibleIndicies = new HashSet<HashSet<int>>();

        void updatePossibleIndices(IndexInfo indexInfo)
        {
            var possibleIndices = indexInfo.PossibleIndicies;

            if (processedPossibleIndicies.Contains(possibleIndices))
            {
                return;
            }

            var allIndices = possibleIndices.ToArray();
            possibleIndices.Clear();

            foreach (var ind in allIndices)
            {
                if (ind == index)
                {
                    continue;
                }

                possibleIndices.Add(ind - (ind > index ? 1 : 0));
            }

            processedPossibleIndicies.Add(possibleIndices);
        }

        if (!_order.Any(o => o.PlayerIndex == index || o.PlayerIndex == -1 && o.PossibleIndicies.Contains(index)))
        {
            for (var i = 0; i < _order.Length; i++)
            {
                var playerIndex = _order[i].PlayerIndex;

                if (playerIndex > index)
                {
                    _order[i].PlayerIndex--;
                    continue;
                }

                if (playerIndex == -1)
                {
                    updatePossibleIndices(_order[i]);
                }
            }

            _removeLog.Add("After: " + ToString());
            return;
        }

        try
        {
            var newOrder = new IndexInfo[_order.Length - 1];
            var possibleVariantsCount = -1;
            HashSet<int>? variantWithIndex = null;

            for (int i = 0, j = 0; i < _order.Length; i++)
            {
                if (_order[i].PlayerIndex == index ||
                    _order[i].PlayerIndex == -1 &&
                    _order[i].PossibleIndicies.Count == 1 &&
                    _order[i].PossibleIndicies.Contains(index))
                {
                    continue;
                }

                newOrder[j] = _order[i];

                if (_order[i].PlayerIndex > index)
                {
                    newOrder[j].PlayerIndex--;
                }

                if (_order[i].PlayerIndex == -1)
                {
                    if (!processedPossibleIndicies.Contains(_order[i].PossibleIndicies) && _order[i].PossibleIndicies.Contains(index))
                    {
                        variantWithIndex = _order[i].PossibleIndicies;
                        possibleVariantsCount = variantWithIndex.Count;
                    }

                    if (_order[i].PossibleIndicies == variantWithIndex)
                    {
                        possibleVariantsCount--;

                        if (possibleVariantsCount == 0) // One variant should be deleted
                        {
                            continue;
                        }
                    }

                    updatePossibleIndices(_order[i]);
                }

                j++;

                if (j == newOrder.Length)
                {
                    break;
                }
            }

            if (_orderIndex == _order.Length - 1)
            {
                _orderIndex = _cycle ? 0 : newOrder.Length - 1;
            }

            _order = newOrder;

            _removeLog.Add("After: " + ToString());
        }
        catch (Exception exc)
        {
            throw new Exception("RemoveAt error: " + removeLog.ToString(), exc);
        }
    }

    public void Reset(bool cycle)
    {
        _orderIndex = -1;
        _cycle = cycle;
    }

    public bool MoveNext()
    {
        _orderIndex++;

        if (_orderIndex == _order.Length)
        {
            if (!_cycle || _order.Length == 0)
            {
                return false;
            }

            _orderIndex = 0;
        }

        return true;
    }

    public void MoveBack() => _orderIndex--;

    public bool IsEmpty() => _order.Length == 0;

    public override string ToString() => 
        new StringBuilder(string.Join(",", _order.Select(o => o.ToString())))
        .Append(' ')
        .Append(_orderIndex)
        .Append(' ')
        .Append(_cycle)
        .ToString();
}
