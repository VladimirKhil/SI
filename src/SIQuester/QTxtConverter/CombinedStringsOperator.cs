using System.Collections.Generic;

namespace QTxtConverter
{
    /// <summary>
    /// Обработчик комбинируемых строк
    /// </summary>
    public sealed class CombinedStringsOperator: List<CombinedString>
    {
        /// <summary>
        /// Создание оператора
        /// </summary>
        public CombinedStringsOperator(): base()
        {
        }

        /// <summary>
        /// Создание комбинации строк по индексам
        /// </summary>
        /// <param name="ind">Индексы комбинируемых строк</param>
        /// <returns></returns>
        public CombinedString CreateCombination(params int[] ind)
        {
            int L = ind.Length;
            foreach (var str in this)
            {
                bool b = true;
                if (str.Sources.Count != L)
                    continue;
                foreach (int i in ind)
                    if (!str.Sources.Contains(i))
                    {
                        b = false;
                        break;
                    }
                if (!b) continue;
                return str;
            }

            int[] ind1 = new int[L/2];
            int[] ind2 = new int[L - L/2];
            for (int i = 0; i < L/2; i++)
                ind1[i] = ind[i];
            for (int i = 0; i < L - L/2; i++)
                ind2[i] = ind[i + L/2];

            var s1 = CreateCombination(ind1);
            var s2 = CreateCombination(ind2);
            var sres = new CombinedString(s1, s2);
            this.Add(sres);

            return sres;
        }

        /// <summary>
        /// Пересечение двух массивов индексов
        /// </summary>
        /// <param name="l1">Первый набор индексов</param>
        /// <param name="l2">Второй набор индексов</param>
        /// <returns></returns>
        public static int[] Intersect(int[] l1, int[] l2)
        {
            int num = 0;
            foreach (int i in l1)
                if ((new List<int>(l2)).Contains(i))
                    num++;

            int[] l = new int[num];
            num = 0;
            foreach (int i in l1)
                if ((new List<int>(l2)).Contains(i))
                    l[num++] = i;

            return l;
        }

        /// <summary>
        /// Объединение строк
        /// </summary>
        /// <param name="s">Массив индексов</param>
        /// <returns>Объединённая строка</returns>
        public string Union(List<int[]> s)
        {
            int L = s.Count;
            var ind = new int[L];
            var use = new bool[L];
            var cs = new CombinedString[L];
            string rez = "";
            for (int i = 0; i < L; i++)
            {
                ind[i] = 0;
                cs[i] = CreateCombination(s[i]);
            }

            bool b;
            do
            {
                for (int i = 0; i < L; i++)
                    use[i] = true;
                for (int i = 1; i < L; i++)
                {
                    if (ind[i] == cs[i].ToString().Length)
                    {
                        use[i] = false;
                        continue;
                    }
                    int k = 0;
                    while (!use[k]) k++;
                    if (k >= i) continue;
                    if (cs[k].ToString()[ind[k]] == cs[i].ToString()[ind[i]])
                        continue;
                    CombinedString str = CreateCombination(Intersect(s[k], s[i]));
                    int l = str.ToString().Length;
                    for (int j = 0; j < l; j++)
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

                bool t = true;
                for (int i = 0; i < L; i++)
                    if (use[i])
                    {
                        if (t)
                        {
                            rez += cs[i].ToString()[ind[i]];
                            t = false;
                        }
                        ind[i]++;
                    }


                b = false;
                for (int i = 0; i < L; i++)
                    if (ind[i] < cs[i].ToString().Length)
                        b = true;
            } while (b);

            return rez;
        }
    }
}
