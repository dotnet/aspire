// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <inheritdoc />
public class ValidatingDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : notnull
{
    /// <summary>
    /// Constructs the <see cref="ValidatingDictionary{TKey, TValue}"/> by providing callbacks that are invoked
    /// whenever the dictionary is mutated to ensure that the keys and values are valid.
    /// </summary>
    /// <param name="keyValidator">Function to validate key.</param>
    /// <param name="valueValidator">Function to validate value.</param>
    public ValidatingDictionary(Func<TKey, bool>? keyValidator = null, Func<TValue, bool>? valueValidator = null)
    {
        _keyValidator = keyValidator ?? (_ => true);
        _valueValidator = valueValidator ?? (_ => true);
    }

    private readonly Func<TKey, bool> _keyValidator;

    private readonly Func<TValue, bool> _valueValidator;

    private readonly IDictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

    private T WhenKeyIsValid<T>(TKey key, Func<T> action)
    {
        if (!_keyValidator(key))
        {
            throw new ArgumentException("Key is invalid");
        }
        else
        {
            return action();
        }
    }

    private void WhenKeyIsValid(TKey key, Action action)
    {
        if (!_keyValidator(key))
        {
            throw new ArgumentException("Key is invalid");
        }
        else
        {
            action();
        }
    }

    private T WhenValueIsValid<T>(TValue value, Func<T> action)
    {
        if (!_valueValidator(value))
        {
            throw new ArgumentException("Value is invalid");
        }
        else
        {
            return action();
        }
    }

    private void WhenValueIsValid(TValue value, Action action)
    {
        if (!_valueValidator(value))
        {
            throw new ArgumentException("Value is invalid");
        }
        else
        {
            action();
        }
    }

    /// <inheritdoc />
    public TValue this[TKey key]
    {
        get => _dictionary[key]!;
        set => WhenKeyIsValid(key, () => WhenValueIsValid(value, () => _dictionary[key] = value));
    }

    /// <inheritdoc />
    public ICollection<TKey> Keys => _dictionary.Keys;

    /// <inheritdoc />
    public ICollection<TValue> Values => _dictionary.Values;

    /// <inheritdoc />
    public int Count => _dictionary.Count;

    /// <inheritdoc />
    public bool IsReadOnly => _dictionary.IsReadOnly;

    /// <inheritdoc />
    public void Add(TKey key, TValue value) => WhenKeyIsValid(key, () => WhenValueIsValid(value, () => _dictionary.Add(key, value)));

    /// <inheritdoc />
    public void Add(KeyValuePair<TKey, TValue> item) => WhenKeyIsValid(item.Key, () => WhenValueIsValid(item.Value, () => _dictionary.Add(item)));

    /// <inheritdoc />
    public void Clear() => _dictionary.Clear();

    /// <inheritdoc />
    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => _dictionary.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();

    /// <inheritdoc />
    public bool Remove(TKey key) => _dictionary.Remove(key);

    /// <inheritdoc />
    public bool Remove(KeyValuePair<TKey, TValue> item) => _dictionary.Remove(item);

    /// <inheritdoc />
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _dictionary.TryGetValue(key, out value);

    IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();
}
