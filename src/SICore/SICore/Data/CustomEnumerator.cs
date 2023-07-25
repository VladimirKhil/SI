using System.Collections;

namespace SICore;

/// <summary>
/// Provides enumeration capabilities and allows to remove values while enumerating.
/// </summary>
/// <typeparam name="T">Enumerator data type.</typeparam>
public sealed class CustomEnumerator<T> : IEnumerator<T>
    where T: struct
{
    private readonly List<T> _data;
    private int _index = -1;

    public T Current => _data[_index];

    object IEnumerator.Current => Current!;

    public CustomEnumerator(IEnumerable<T> data) => _data = new List<T>(data);

    public void Dispose() { }

    public bool MoveNext()
    {
        _index++;
        return _index < _data.Count;
    }

    public void Reset() => _index = -1;

    /// <summary>
    /// Updates underlying collection.
    /// </summary>
    /// <param name="itemUpdate">Items updater. If it returns null, the item is removed.</param>
    public void Update(Func<T, T?> itemUpdate)
    {
        for (var i = 0; i < _data.Count; i++)
        {
            var updatedValue = itemUpdate(_data[i]);

            if (!updatedValue.HasValue)
            {
                _data.RemoveAt(i);

                if (i <= _index)
                {
                    _index--;
                }

                i--;
            }
            else
            {
                _data[i] = updatedValue.Value;
            }
        }
    }

    public override string ToString() => $"{_index}:[{string.Join(',', _data)}]";
}
