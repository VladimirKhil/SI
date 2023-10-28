namespace SICore.Utils;

internal static class RandomExtensions
{
    internal static double NextGaussian(this Random random, double mean, double stddev)
    {
        double x1 = 1 - random.NextDouble();
        double x2 = 1 - random.NextDouble();

        double y1 = Math.Sqrt(-2.0 * Math.Log(x1)) * Math.Cos(2.0 * Math.PI * x2);
        return y1 * stddev + mean;
    }

    internal static int SelectRandom<T>(this IEnumerable<T> list, Predicate<T> condition, Random random)
    {
        var goodItems = list
            .Select((item, index) => new { Item = item, Index = index })
            .Where(item => condition(item.Item)).ToArray();

        if (goodItems.Length == 0)
        {
            throw new Exception("goodItems.Length == 0");
        }

        var ind = random.Next(goodItems.Length);
        return goodItems[ind].Index;
    }

    internal static int SelectRandomOnIndex<T>(this IEnumerable<T> list, Predicate<int> condition, Random random)
    {
        var goodItems = list
            .Select((item, index) => new { Item = item, Index = index })
            .Where(item => condition(item.Index)).ToArray();

        var ind = random.Next(goodItems.Length);
        return goodItems[ind].Index;
    }

    /// <summary>
    /// Получить случайную строку ресурса
    /// </summary>
    /// <param name="resource">Строки ресурса, разделённые точкой с запятой</param>
    /// <returns>Одна из строк ресурса (случайная)</returns>
    public static string GetRandomString(this Random random, string resource)
    {
        var resources = resource.Split(';');
        var index = random.Next(resources.Length);

        return resources[index];
    }
}
