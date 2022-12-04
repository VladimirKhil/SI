namespace Notions;

/// <summary>
/// Смысловое понятие, обобщение строки
/// </summary>
public static class Notion
{
    /// <summary>
    /// Поиск наиболее общей подпоследовательности двух понятий при определении правильности ответа
    /// </summary>
    /// <param name="s1">Первое понятие</param>
    /// <param name="s2">Второе понятие</param>
    public static double AnswerValidatingCommon2(string s1, string s2)
    {
        if (s2.Length == 0)
            return 1.0;

        if (s1.Length == 0)
            return 0.0;

        var ss1 = StringExtensions.Simplify(s1);
        var ss2 = StringExtensions.Simplify(s2);

        var cost = new int[ss2.Length];

        int L = s2.Length;
        int l = ss2.Length;
        int c = 1;

        var maxscore = 0;
        for (int j = L - 1, i = l - 1; j >= 0; j--)
            if (char.IsLetterOrDigit(s2[j]))
            {
                c++;
                cost[i] = c;
                maxscore += c;
                i--;
            }
            else
                c = 1;

        var result = StringManager.BestCommonMatch(ss1, ss2, (str1, str2, points) => SearchNorm(points, cost), ss1.Length * ss2.Length > 1500);

        int L1 = ss1.Length;
        int score = 0;
        foreach (var item in result)
        {
            score += cost[item.Y];
        }

        if (maxscore == 0)
            if (score == 0)
                return 1.0;
            else
                return 0.0;
        return (double)score / maxscore;
    }

    private static int SearchNorm(Point[] points, int[] cost)
    {
        int distance = 0;

        for (int i = 0; i < points.Length; i++)
        {
            if (i > 0 && points[i - 1].X == points[i].X - 1 || i + 1 < points.Length && points[i + 1].X == points[i].X + 1)
                distance -= 100 * cost[points[i].Y];

            if (i > 0)
            {
                var dist = points[i].X - points[i - 1].X;
                if (dist > 1)
                    distance += dist - 1;
            }
        }

        return distance;
    }

    /// <summary>
    /// Returns random value from values collection.
    /// </summary>
    /// <param name="values">Values collection.</param>
    public static string RandomString(params string[] values)
    {
        var c = values.Length;

        if (c == 0)
        {
            return string.Empty;
        }

        var random = new Random(DateTime.UtcNow.Millisecond);
        return values[random.Next(c)];
    }

    public static string FormatNumber(int num) => StringExtensions.FormatNumber(num.ToString());
}
