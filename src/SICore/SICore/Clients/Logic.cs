using SICore.Utils;

namespace SICore;

/// <summary>
/// Represents agent logic.
/// </summary>
/// <typeparam name="D">Agent data type.</typeparam>
public abstract class Logic<D> : ILogic
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

    /// <summary>
    /// Запись сообщения в лог
    /// </summary>
    /// <param name="s"></param>
    public void AddLog(string s) => _data.OnAddString(null, s, LogMode.Log);

    protected int SelectRandom<T>(IEnumerable<T> list, Predicate<T> condition) =>
        list.SelectRandom(condition, Random.Shared);

    protected int SelectRandomOnIndex<T>(IEnumerable<T> list, Predicate<int> condition) =>
        list.SelectRandomOnIndex(condition, Random.Shared);

    public string GetRandomString(string resource) => Random.Shared.GetRandomString(resource);
}
