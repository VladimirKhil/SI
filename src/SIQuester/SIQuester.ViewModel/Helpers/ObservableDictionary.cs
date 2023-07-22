using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace SIQuester.ViewModel.Helpers;

/// <summary>
/// Implements dictionary with collection change notifications.
/// </summary>
public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
    where TKey : notnull
{
    private readonly IDictionary<TKey, TValue> _dictionary;

    public ObservableDictionary() => _dictionary = new Dictionary<TKey, TValue>();

    public ObservableDictionary(IDictionary<TKey, TValue> dictionary) => _dictionary = dictionary;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public ICollection<TKey> Keys => _dictionary.Keys;

    public ICollection<TValue> Values => _dictionary.Values;

    public int Count => _dictionary.Count;

    public bool IsReadOnly => _dictionary.IsReadOnly;

    public TValue this[TKey key]
    {
        get => _dictionary[key];
        set
        {
            bool exists = _dictionary.TryGetValue(key, out var oldValue);

            if (exists)
            {
                _dictionary[key] = value;

                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace, new KeyValuePair<TKey, TValue>(key, value), new KeyValuePair<TKey, TValue>(key, oldValue)));
            }
            else
            {
                _dictionary[key] = value;

                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value)));

                OnPropertyChanged(nameof(Count));
            }
        }
    }

    public void Add(TKey key, TValue value)
    {
        _dictionary.Add(key, value);

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value)));

        OnPropertyChanged(nameof(Count));
    }

    public bool Remove(TKey key)
    {
        if (_dictionary.TryGetValue(key, out var value))
        {
            var index = Array.IndexOf(_dictionary.ToArray(), new KeyValuePair<TKey, TValue>(key, value));
            bool removed = _dictionary.Remove(key);

            if (removed)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove, new KeyValuePair<TKey, TValue>(key, value), index));

                OnPropertyChanged(nameof(Count));
            }

            return removed;
        }

        return false;
    }

    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _dictionary.TryGetValue(key, out value);

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        _dictionary.Add(item);

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        OnPropertyChanged(nameof(Count));
    }

    public void Clear()
    {
        _dictionary.Clear();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        OnPropertyChanged(nameof(Count));
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) => _dictionary.Contains(item);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => _dictionary.CopyTo(array, arrayIndex);

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        bool removed = _dictionary.Remove(item);

        if (removed)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
            OnPropertyChanged(nameof(Count));
        }

        return removed;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);

    protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
