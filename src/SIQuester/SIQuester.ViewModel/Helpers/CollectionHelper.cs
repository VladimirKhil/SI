using System.Collections.ObjectModel;

namespace SIQuester.ViewModel.Helpers;

internal static class CollectionHelper
{
    /// <summary>
    /// Clears list by deleting items one by one allowing to capture changes and redo clearing later.
    /// </summary>
    /// <typeparam name="T">List item type.</typeparam>
    /// <param name="list">List object.</param>
    internal static void ClearOneByOne<T>(this IList<T> list)
    {
        while (list.Count > 0)
        {
            list.RemoveAt(0);
        }
    }

    internal static void Merge<T>(this Collection<T> collection, IList<T> origin, Action<T, T>? merger = null)
    {
        var commonLength = Math.Min(collection.Count, origin.Count);

        for (var i = 0; i < commonLength; i++)
        {
            if (merger != null)
            {
                merger(collection[i], origin[i]);
            }
            else if (collection[i] == null || !collection[i]!.Equals(origin[i]))
            {
                collection[i] = origin[i];
            }
        }

        if (collection.Count < origin.Count)
        {
            foreach (var item in origin.Skip(collection.Count))
            {
                collection.Add(item);
            }
        }
        else if (collection.Count > origin.Count)
        {
            var removeCount = collection.Count - origin.Count;

            for (var i = 0; i < removeCount; i++)
            {
                collection.RemoveAt(origin.Count);
            }
        }
    }

    internal static void Merge<T, U>(
        this Collection<T> collection,
        IList<U> origin,
        Func<U, T> generator,
        Action<T, U>? merger = null)
    {
        var commonLength = Math.Min(collection.Count, origin.Count);

        for (var i = 0; i < commonLength; i++)
        {
            if (merger != null)
            {
                merger(collection[i], origin[i]);
            }
            else
            {
                collection[i] = generator(origin[i]);
            }
        }

        if (collection.Count < origin.Count)
        {
            foreach (var item in origin.Skip(collection.Count))
            {
                collection.Add(generator(item));
            }
        }
        else if (collection.Count > origin.Count)
        {
            var removeCount = collection.Count - origin.Count;

            for (var i = 0; i < removeCount; i++)
            {
                collection.RemoveAt(origin.Count);
            }
        }
    }
}
