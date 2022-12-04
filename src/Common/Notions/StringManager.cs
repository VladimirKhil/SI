namespace Notions;

/// <summary>
/// Класс для сложных операций со строками
/// </summary>
public static class StringManager
{
    /// <summary>
    /// Норма общей подстроки двух строк
    /// </summary>
    /// <param name="string1">Первая строка</param>
    /// <param name="string2">Вторая строка</param>
    /// <param name="equal">Массив совпадающих символов в двух строках</param>
    /// <returns>Величина нормы: чем меньше - тем лучше</returns>
    public delegate int StringNorm(string string1, string string2, Point[] equal);

    /// <summary>
    /// Норма общей подстроки для вычисления подстроки максимальной длины
    /// </summary>
    /// <param name="string1">Первая строка</param>
    /// <param name="string2">Вторая строка</param>
    /// <param name="equal">Массив совпадающих символов в двух строках</param>
    /// <returns>Величина нормы: чем меньше - тем лучше</returns>
    public static int MaxLengthNorm(string string1, string string2, Point[] equal) => -equal.Length;

    /// <summary>
    /// Норма общей подстроки для вычисления шаблона темы и вопроса СИ
    /// </summary>
    /// <param name="string1">Первая строка</param>
    /// <param name="string2">Вторая строка</param>
    /// <param name="equal">Массив совпадающих символов в двух строках</param>
    /// <returns>Величина нормы: чем меньше - тем лучше</returns>
    public static int TemplateSearchingNorm(string string1, string string2, Point[] equal)
    {
        int result = 0;
        for (int i = 0; i < equal.Length; i++)
        {
            char c = string1[equal[i].X];
            if (c == '(' || c == ')' || c == ':' || c == '\t')
                result -= 100;
            else if (c == '\r' || c == '\n' || c == '\t' || c == '.' || c == '0')
                result -= 15;
            else if ((i > 1 && equal[i - 1].X == equal[i].X - 1 && equal[i - 2].X == equal[i].X - 2)
                || (i < equal.Length - 2 && equal[i + 1].X == equal[i].X + 1 && equal[i + 2].X == equal[i].X + 2))
                result -= 6;
            else if ((i > 0 && equal[i - 1].X == equal[i].X - 1)
                || (i < equal.Length - 1 && equal[i + 1].X == equal[i].X + 1))
                result -= 2;
            else
                result--;
        }

        return result;
    }

    internal static Equality BestCommonSubStringEquality(string string1, string string2, StringNorm norm, bool cutIntermediateResults)
    {
        var currentRow = new Equality[string2.Length + 1];
        for (int j = 0; j <= string2.Length; j++)
            currentRow[j] = new Equality();

        for (int i = 1; i <= string1.Length; i++)
        {
            Equality[] nextRow = new Equality[string2.Length + 1];
            nextRow[0] = new Equality();
            for (int j = 1; j <= string2.Length; j++)
            {
                nextRow[j] = new Equality();

                if (string1[i - 1] == string2[j - 1])
                {
                    nextRow[j].Append(currentRow[j - 1], i - 1, j - 1);
                }
                else
                {
                    nextRow[j].JoinTop(currentRow[j]);
                    nextRow[j].JoinLeft(nextRow[j - 1]);
                }

                if (cutIntermediateResults)
                    nextRow[j].LeaveOnlyBest(string1, string2, norm);
            }

            currentRow = nextRow;
        }

        return currentRow[string2.Length];
    }

    /// <summary>
    /// Получить общую подстроку двух строк, лучшую по некоторой функции расстояния между ними
    /// </summary>
    /// <param name="string1">Первая строка</param>
    /// <param name="string2">Вторая строка</param>
    /// <param name="norm">Норма подстроки: чем меньше её результат - тем лучше подстрока</param>
    /// <param name="cutIntermediateResults">Можно ли отбрасывать промежуточные неоптимальные результаты. Устанавливать в true, если для любых s1, s2, s: norm(s1) &lt; norm(s2) => norm(s1 + s) &lt; norm(s2 + s)</param>
    /// <returns>Лучшая общая подстрока максимально возможной длины</returns>
    public static string BestCommonSubString(string string1, string string2, StringNorm norm, bool cutIntermediateResults)
    {
        var equality = BestCommonSubStringEquality(string1, string2, norm, cutIntermediateResults);
        return equality.Best(string1, string2, norm);
    }

    /// <summary>
    /// Получить общую подстроку двух строк, лучшую по некоторой функции расстояния между ними
    /// </summary>
    /// <param name="string1">Первая строка</param>
    /// <param name="string2">Вторая строка</param>
    /// <param name="norm">Норма подстроки: чем меньше её результат - тем лучше подстрока</param>
    /// <param name="cutIntermediateResults">Можно ли отбрасывать промежуточные неоптимальные результаты. Устанавливать в true, если для любых s1, s2, s: norm(s1) &lt; norm(s2) => norm(s1 + s) &lt; norm(s2 + s)</param>
    /// <returns>Набор совпадений, определяющий лучшую общую подстроку максимально возможной длины</returns>
    public static Point[] BestCommonMatch(string string1, string string2, StringNorm norm, bool cutIntermediateResults)
    {
        var equality = BestCommonSubStringEquality(string1, string2, norm, cutIntermediateResults);
        return equality.BestEquality(string1, string2, norm);
    }
}
