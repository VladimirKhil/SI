using System.Collections;

namespace SICore;

/// <summary>
/// Provides enumeration capabilities and allows to remove values while enumerating.
/// </summary>
/// <typeparam name="T">Enumerator data type.</typeparam>
public sealed class CustomEnumerator<T> : IEnumerator<T>
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

    public void RemoveValue(T value)
    {
        for (int i = 0; i < _data.Count; i++)
        {
            if (Equals(_data[i], value))
            {
                _data.RemoveAt(i);

                if (i <= _index)
                {
                    _index--;
                }

                i--;
            }
        }
    }
}
