﻿namespace SIPackages.Helpers;

internal static class CollectionHelper
{
    internal static int GetCollectionHashCode<T>(this IEnumerable<T> collection) where T: notnull =>
        collection.Aggregate(0, (x, y) => x.GetHashCode() ^ y.GetHashCode());
}
