namespace SICore;

/// <summary>
/// Chooses which player who pressed the button gets the right to answer.
/// </summary>
/// <remarks>
/// Pure randomness often produces consecutive winning streaks for a single player,
/// making the game feel biased. To make the selection feel fair, a weighted roulette
/// is used: the longer a player hasn't won the button, the higher their chances.
/// At the same time, a player cannot win twice in a row (unless they pressed it alone).
/// </remarks>
public class AnswererRandomizer
{
    private readonly Random _random;

    /// <summary>
    /// Memory depth: how many recent attempts are taken into account (usually equal to the number of players).
    /// </summary>
    private int _window;

    /// <summary>
    /// Player's attempt history (1 — won the button, 0 — lost). It is not reset upon exit
    /// or roster change, so that accumulated "bad luck" is preserved.
    /// </summary>
    private readonly Dictionary<string, List<int>> _history = new();

    private string? _prevWinner;

    /// <summary>
    /// Creates a randomizer for the game. The seed is needed for reproducibility in tests.
    /// </summary>
    public AnswererRandomizer(int initialPlayersCount, int? seed = null)
    {
        _window = Math.Max(1, initialPlayersCount);
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>
    /// Adjusts the memory window to the changed number of players.
    /// </summary>
    public void SetPlayersCount(int playersCount)
    {
        _window = Math.Max(1, playersCount);
    }

    /// <summary>
    /// Selects the answerer among those who managed to press the button.
    /// </summary>
    public string ChooseAnswerer(List<string> candidates)
    {
        if (candidates is null || candidates.Count == 0)
        {
            throw new ArgumentException("Candidates cannot be null or empty.", nameof(candidates));
        }

        foreach (var p in candidates)
        {
            if (!_history.ContainsKey(p))
            {
                _history[p] = new List<int>();
            }
        }

        var weights = candidates.ToDictionary(p => p, p => Weight(_history[p], _window));

        // The previous winner cannot win twice in a row if someone else pressed the button
        if (_prevWinner != null && weights.ContainsKey(_prevWinner) && candidates.Count > 1)
        {
            weights[_prevWinner] = 0;
        }

        var winner = Draw(candidates, weights);

        // Record history only for those who actually pressed the button — passive players should not accumulate weight
        foreach (var p in candidates)
        {
            var h = _history[p];
            h.Add(p == winner ? 1 : 0);
        }

        _prevWinner = winner;

        return winner;
    }

    /// <summary>
    /// Player's weight: base value of 1 plus the length of the current streak without wins (within the memory window).
    /// </summary>
    private static int Weight(List<int> playerHistory, int window)
    {
        int streak = 0;

        // From the most recent attempts to older ones
        for (int i = playerHistory.Count - 1; i >= 0 && streak < window; i--)
        {
            if (playerHistory[i] == 1)
            {
                break;
            }

            streak++;
        }

        return 1 + streak;
    }

    /// <summary>
    /// Roulette: selects a candidate with probability proportional to their weight.
    /// </summary>
    private string Draw(List<string> candidates, Dictionary<string, int> weights)
    {
        int total = candidates.Sum(c => weights[c]);

        // If all candidates have a weight of 0 (edge case), pick the last one
        if (total == 0)
        {
            return candidates[^1];
        }

        double r = _random.NextDouble() * total;
        double upto = 0.0;

        foreach (var c in candidates)
        {
            upto += weights[c];

            if (r < upto)
            {
                return c;
            }
        }

        // Fallback for double floating-point inaccuracies
        return candidates.Last(c => weights[c] > 0);
    }
}
