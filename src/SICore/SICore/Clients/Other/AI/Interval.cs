namespace SICore;

/// <summary>
/// Числовой интервал
/// </summary>
internal class Interval
{
    /// <summary>
    /// Создание интервала
    /// </summary>
    /// <param name="min">Нижняя граница</param>
    /// <param name="max">Верхняя граница</param>
    public Interval(int min, int max)
    {
        if (max < min)
            max = min;

        Min = min;
        Max = max;
    }

    /// <summary>
    /// Нижняя граница интервала
    /// </summary>
    public int Min { get; set; } = 0;

    /// <summary>
    /// Верхняя граница интервала
    /// </summary>
    public int Max { get; set; } = 0;

    /// <summary>
    /// Длина интервала
    /// </summary>
    public int Length => Max - Min + 1;

    /// <summary>
    /// Строковое представление интервала
    /// </summary>
    /// <returns>Строковое представление интервала</returns>
    public new string ToString() => $"[{Min}; {Max}]";

    /// <summary>
    /// Соединяет соприкасающиеся интервалы
    /// </summary>
    /// <param name="intervals">Список объединяемых интервалов</param>
    /// <param name="step">Допустимый шаг для слияния интервалов</param>
    public static void Join(ref List<Interval> intervals, int step)
    {
        int i = 0;

        while (i + 1 < intervals.Count)
        {
            if (intervals[i].Max + step == intervals[i + 1].Min && intervals[i].Min > 0)
            {
                intervals[i].Max = intervals[i + 1].Max;
                intervals.RemoveAt(i + 1);
            }
            else
            {
                i++;
            }
        }
    }

    /// <summary>
    /// Рассечение одного множества интервалов другим
    /// </summary>
    /// <param name="original"></param>
    /// <param name="splitter"></param>
    public static void SplitBy(ref List<Interval> original, List<Interval> splitter)
    {
        int i = 0, j = 0;
        while (i < splitter.Count && j < original.Count)
        {
            if (original[j].Max <= splitter[i].Min)
                j++;
            else if (original[j].Min < splitter[i].Min)
            {
                original.Insert(j, new Interval(original[j].Min, splitter[i].Min - 100));
                j++;
                original[j].Min = splitter[i].Min;
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
    public static int Locale(ref List<Interval> intervals, int position)
    {
        if (intervals.Count == 0)
            return -1;
        int total = intervals[0].Length;
        int i = 0;
        while (total <= position)
            total += intervals[++i].Length;
        return intervals[i].Max - total + position + 1;
    }
}
