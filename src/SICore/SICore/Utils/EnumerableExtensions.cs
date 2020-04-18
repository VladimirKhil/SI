using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SICore.Utils
{
    internal static class EnumerableExtensions
    {
        internal static int SelectRandom<T>(this IEnumerable<T> list, Predicate<T> condition)
        {
            var goodItems = list
                .Select((item, index) => new { Item = item, Index = index })
                .Where(item => condition(item.Item)).ToArray();

            var ind = Data.Rand.Next(goodItems.Length);
            return goodItems[ind].Index;
        }

        internal static int SelectRandomOnIndex<T>(this IEnumerable<T> list, Predicate<int> condition)
        {
            var goodItems = list
                .Select((item, index) => new { Item = item, Index = index })
                .Where(item => condition(item.Index)).ToArray();

            var ind = Data.Rand.Next(goodItems.Length);
            return goodItems[ind].Index;
        }
    }
}
