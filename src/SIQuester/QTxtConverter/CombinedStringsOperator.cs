namespace QTxtConverter;

/// <summary>
/// Generates a common string for a set of strings.
/// </summary>
public sealed class CombinedStringsOperator : List<CombinedString>
{
    /// <summary>
    /// Создание оператора
    /// </summary>
    public CombinedStringsOperator(): base() { }

    // TODO: rewrite algorithm to combine all string at once instead of combining them in pairs

    /// <summary>
    /// Создание комбинации строк по индексам
    /// </summary>
    /// <param name="ind">Индексы комбинируемых строк</param>
    /// <returns></returns>
    public CombinedString CreateCombination(params int[] ind)
    {
        var indicesCount = ind.Length;

        foreach (var str in this)
        {
            var foundIndex = true;

            if (str.Sources.Count != indicesCount)
            {
                continue;
            }

            foreach (int i in ind)
            {
                if (!str.Sources.Contains(i))
                {
                    foundIndex = false;
                    break;
                }
            }

            if (!foundIndex)
            {
                continue;
            }

            return str;
        }

        var ind1 = new int[indicesCount / 2];
        var ind2 = new int[indicesCount - indicesCount / 2];

        for (int i = 0; i < indicesCount / 2; i++)
        {
            ind1[i] = ind[i];
        }

        for (int i = 0; i < indicesCount - indicesCount / 2; i++)
        {
            ind2[i] = ind[i + indicesCount / 2];
        }

        var s1 = CreateCombination(ind1);
        var s2 = CreateCombination(ind2);
        var sres = new CombinedString(s1, s2);

        Add(sres);

        return sres;
    }

    /// <summary>
    /// Пересечение двух массивов индексов
    /// </summary>
    /// <param name="l1">Первый набор индексов</param>
    /// <param name="l2">Второй набор индексов</param>
    /// <returns></returns>
    public static int[] Intersect(int[] l1, int[] l2) => l1.Intersect(l2).ToArray();

    /// <summary>
    /// Объединение строк
    /// </summary>
    /// <param name="s">Массив индексов</param>
    /// <returns>Объединённая строка</returns>
    public string Union(List<int[]> s)
    {
        int indicesCount = s.Count;
        var ind = new int[indicesCount];
        var use = new bool[indicesCount];
        var cs = new CombinedString[indicesCount];
        string rez = "";

        for (int i = 0; i < indicesCount; i++)
        {
            ind[i] = 0;
            cs[i] = CreateCombination(s[i]);
        }

        bool b;
        do
        {
            for (int i = 0; i < indicesCount; i++)
            {
                use[i] = true;
            }

            for (int i = 1; i < indicesCount; i++)
            {
                if (ind[i] == cs[i].ToString().Length)
                {
                    use[i] = false;
                    continue;
                }

                int k = 0;
                while (!use[k])
                {
                    k++;
                }

                if (k >= i)
                {
                    continue;
                }

                if (cs[k].ToString()[ind[k]] == cs[i].ToString()[ind[i]])
                {
                    continue;
                }

                var str = CreateCombination(Intersect(s[k], s[i]));
                int l = str.ToString().Length;
                for (int j = 0; j < l; j++)
                {
                    if (str.ToString()[j] == cs[k].ToString()[ind[k]])
                    {
                        use[i] = false;
                        break;
                    }
                    else if (str.ToString()[j] == cs[i].ToString()[ind[i]])
                    {
                        for (int f = 0; f < i; f++)
                            use[f] = false;
                        break;
                    }
                }
            }

            bool t = true;
            for (int i = 0; i < indicesCount; i++)
            {
                if (use[i])
                {
                    if (t)
                    {
                        rez += cs[i].ToString()[ind[i]];
                        t = false;
                    }
                    ind[i]++;
                }
            }

            b = false;
            for (int i = 0; i < indicesCount; i++)
            {
                if (ind[i] < cs[i].ToString().Length)
                {
                    b = true;
                }
            }
        } while (b);

        return rez;
    }
}
