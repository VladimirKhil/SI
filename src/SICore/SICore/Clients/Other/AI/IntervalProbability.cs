using System.Collections.Generic;

namespace SICore
{
    /// <summary>
    /// Специализированный интервал для ставок
    /// Содержит вероятности достижения определённого лидерства при данных ставках
    /// </summary>
    internal sealed class IntervalProbability : Interval
    {
        public IntervalProbability(int min, int max) : base(min, max) { }

        /// <summary>
        /// Вероятности достижения лидерства
        /// </summary>
        public List<double> Probabilities { get; } = new List<double>();

        public new string ToString()
        {
            string s = base.ToString();
            string h = "";
            if (Probabilities.Count > 0)
            {
                s += " (";
                foreach (double d in Probabilities)
                {
                    if (h.Length > 0)
                        h += " ";
                    h += d;
                }
                s += h + ")";
            }

            return s;
        }

        public double ProbSum
        {
            get
            {
                double sum = 0;
                foreach (double prob in Probabilities)
                    sum += prob;

                return sum;
            }
        }

        /// <summary>
        /// Соединяет соприкасающиеся интервалы
        /// </summary>
        /// <param name="intervals">Список объединяемых интервалов</param>
        /// <param name="step">Допустимый шаг для слияния интервалов</param>
        public static void Join(ref List<IntervalProbability> intervals, int step)
        {
            int i = 0;
            while (i + 1 < intervals.Count)
            {
                if (intervals[i].Max + step == intervals[i + 1].Min)
                {
                    bool equal = intervals[i].Probabilities.Count == intervals[i + 1].Probabilities.Count;
                    if (equal)
                        for (int j = 0; j < intervals[i].Probabilities.Count; j++)
                            equal = equal && intervals[i].Probabilities[j] == intervals[i + 1].Probabilities[j];

                    if (equal)
                    {
                        intervals[i].Max = intervals[i + 1].Max;
                        intervals.RemoveAt(i + 1);
                    }
                    else
                        i++;
                }
                else
                    i++;
            }
        }


        /// <summary>
        /// Находит точку во множестве интервалов
        /// </summary>
        /// <param name="intervals">Множество интервалов</param>
        /// <param name="position">Номер точки во множестве</param>
        public static int Locale(ref List<IntervalProbability> intervals, int position)
        {
            if (intervals.Count == 0)
                return -1;

            int total = intervals[0].Length;
            int i = 0;
            while (total <= position && i + 1 < intervals.Count)
                total += intervals[++i].Length;

            return intervals[i].Max - total + position + 1;
        }
    }
}
