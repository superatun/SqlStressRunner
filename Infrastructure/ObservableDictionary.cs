using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SqlStressRunner.Infrastructure;

/// <summary>
/// Observable dictionary that supports WPF two-way binding.
/// Implements INotifyCollectionChanged and INotifyPropertyChanged for DataGrid compatibility.
/// </summary>
public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _dictionary = new();

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public TValue this[TKey key]
    {
        get => _dictionary[key];
        set
        {
            bool exists = _dictionary.ContainsKey(key);
            TValue? oldValue = exists ? _dictionary[key] : default;
            _dictionary[key] = value;

            if (exists)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace,
                    new KeyValuePair<TKey, TValue>(key, value),
                    new KeyValuePair<TKey, TValue>(key, oldValue!)));
            }
            else
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add,
                    new KeyValuePair<TKey, TValue>(key, value)));
                OnPropertyChanged(nameof(Count));
            }

            // Notify indexer change
            OnPropertyChanged("Item[]");
        }
    }

    public ICollection<TKey> Keys => _dictionary.Keys;
    public ICollection<TValue> Values => _dictionary.Values;
    public int Count => _dictionary.Count;
    public bool IsReadOnly => false;

    public void Add(TKey key, TValue value)
    {
        _dictionary.Add(key, value);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Add,
            new KeyValuePair<TKey, TValue>(key, value)));
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged("Item[]");
    }

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    public void Clear()
    {
        _dictionary.Clear();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged("Item[]");
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) => _dictionary.Contains(item);
    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
        ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(array, arrayIndex);

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Remove(TKey key)
    {
        if (_dictionary.TryGetValue(key, out TValue? value))
        {
            _dictionary.Remove(key);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove,
                new KeyValuePair<TKey, TValue>(key, value)));
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged("Item[]");
            return true;
        }
        return false;
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

    public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value!);

    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
        CollectionChanged?.Invoke(this, args);
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
