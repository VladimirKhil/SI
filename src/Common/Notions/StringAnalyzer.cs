namespace Notions;

/// <summary>
/// Provides helper methods for working with strings.
/// </summary>
public static class StringAnalyzer
{
    private const int MaxQueueLength = 100;
    private const int MaxIterationsCount = 100_000;

    /// <summary>
    /// Searches for the longest common substring of provided source strings. Common substring could be sparse.
    /// </summary>
    /// <param name="values">Source strings.</param>
    /// <returns>The longest common substring of values and corresponding matched positions in source strings.</returns>
    public static SubstringMatch? LongestCommonSubstring(params string[] values)
    {
        if (values.Length == 0)
        {
            return null;
        }

        if (values.Length == 1)
        {
            return new SubstringMatch(new int[][] { Enumerable.Range(0, values[0].Length).ToArray() }, values[0]);
        }

        var matches = new Queue<SubstringSubmatch>(new[] { new SubstringSubmatch(null, 0, null) });

        SubstringSubmatch? bestHypotesis = null;
        var iterationsCounter = 0;

        while (matches.Count > 0 && iterationsCounter < MaxIterationsCount)
        {
            var match = matches.Dequeue();

            var originalPositions = match.Positions ?? Enumerable.Repeat(-1, values.Length).ToArray();

            var hypotesisStart = originalPositions;

            var currentHypotises = new List<int[]>();

            do
            {
                var nextPositions = TryMove(hypotesisStart, values);

                if (nextPositions == null)
                {
                    break;
                }

                var newHypotesis = new SubstringSubmatch(nextPositions, match.Length + 1, match);

                var isSuitable = true;

                for (int i = 0; i < currentHypotises.Count; i++)
                {
                    var accepatble = false;

                    for (int j = 0; j < currentHypotises[i].Length; j++)
                    {
                        if (newHypotesis.Positions![j] < currentHypotises[i][j])
                        {
                            accepatble = true;
                            break;
                        }
                    }

                    if (!accepatble)
                    {
                        isSuitable = false;
                        break;
                    }
                }

                if (isSuitable)
                {
                    currentHypotises.Add(newHypotesis.Positions!);

                    if (matches.Count < MaxQueueLength)
                    {
                        matches.Enqueue(newHypotesis);
                    }

                    bestHypotesis = bestHypotesis == null
                        ? newHypotesis
                        : (bestHypotesis.Length < newHypotesis.Length ? newHypotesis : bestHypotesis);
                }

                hypotesisStart = (int[])originalPositions.Clone();
                hypotesisStart[0] = nextPositions[0];
            } while (++iterationsCounter < MaxIterationsCount);
        }

        return BuildFinalMatch(values, bestHypotesis);
    }

    private static SubstringMatch? BuildFinalMatch(string[] values, SubstringSubmatch? bestHypotesis)
    {
        var resultPositions = new List<int[]>();
        var resultString = new List<char>();

        var currentHypotesis = bestHypotesis;

        while (currentHypotesis != null && currentHypotesis.Positions != null)
        {
            resultPositions.Add(currentHypotesis.Positions);
            resultString.Add(values[0][currentHypotesis.Positions[0]]);

            currentHypotesis = currentHypotesis.PreviousMatch;
        }

        resultPositions.Reverse();
        resultString.Reverse();

        return new SubstringMatch(resultPositions.ToArray(), new string(resultString.ToArray()));
    }

    private static int[]? TryMove(int[] positions, string[] values)
    {
        var firstPos = positions[0] + 1;
        var result = Enumerable.Repeat(0, values.Length).ToArray();

        while (firstPos < values[0].Length)
        {
            result[0] = firstPos;

            var matched = true;

            for (int i = 1; i < values.Length; i++)
            {
                var found = false;

                for (int j = positions[i] + 1; j < values[i].Length; j++)
                {
                    if (values[0][firstPos] == values[i][j])
                    {
                        found = true;
                        result[i] = j;
                        break;
                    }
                }

                if (!found)
                {
                    matched = false;
                    break;
                }
            }

            if (!matched)
            {
                firstPos++;
                continue;
            }

            return result;
        }

        return null;
    }

    public record SubstringSubmatch(int[]? Positions, int Length, SubstringSubmatch? PreviousMatch);

    public record struct SubstringMatch(int[][] PositionsHistory, string Substring);
}
