﻿using SICore.Utils;

namespace SICore;

/// <summary>
/// Represents agent logic.
/// </summary>
/// <typeparam name="D">Agent data type.</typeparam>
public abstract class Logic<D>
    where D : Data
{
    /// <summary>
    /// Agent data.
    /// </summary>
    protected D _data; // TODO: field must be private. Implement a property for protected access.

    /// <summary>
    /// Typed agent data.
    /// </summary>
    public D ClientData => _data;

    /// <summary>
    /// Common-typed agent data.
    /// </summary>
    public Data Data => _data;

    public Logic(D data) => _data = data ?? throw new ArgumentNullException(nameof(data));

    protected int SelectRandom<T>(IEnumerable<T> list, Predicate<T> condition) =>
        list.SelectRandom(condition, Random.Shared);

    protected int SelectRandomOnIndex<T>(IEnumerable<T> list, Predicate<int> condition) =>
        list.SelectRandomOnIndex(condition, Random.Shared);

    public string GetRandomString(string resource) => Random.Shared.GetRandomString(resource);
}
