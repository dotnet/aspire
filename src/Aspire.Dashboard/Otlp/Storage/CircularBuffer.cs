// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.InteropServices;

namespace Aspire.Dashboard.Otlp.Storage;

internal sealed class CircularBuffer<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable
{
    // Internal for testing.
    internal readonly List<T> _buffer;
    internal int _start;
    internal int _end;

    public CircularBuffer(int capacity)
    {
        if (capacity < 1)
        {
            throw new ArgumentException("Circular buffer must have a capacity greater than 0.", nameof(capacity));
        }

        _buffer = new List<T>();
        Capacity = capacity;
        _start = 0;
        _end = 0;
    }

    public int Capacity { get; }

    public bool IsFull => Count == Capacity;

    public bool IsEmpty => Count == 0;

    public int Count => _buffer.Count;

    public bool IsReadOnly { get; }

    public bool IsFixedSize { get; } = true;

    public object SyncRoot { get; } = new object();

    public bool IsSynchronized { get; }

    public int IndexOf(T item)
    {
        for (var index = 0; index < Count; ++index)
        {
            if (Equals(this[index], item))
            {
                return index;
            }
        }
        return -1;
    }

    public void Insert(int index, T item)
    {
        if (index == Count)
        {
            Add(item);
            return;
        }

        ValidateIndexInRange(index);

        if (IsFull)
        {
            if (index == 0)
            {
                // Item inserted at 0 is actually the "last" in the buffer and is removed.
                return;
            }

            var internalIndex = InternalIndex(index);

            var data = CollectionsMarshal.AsSpan(_buffer);
            // Data is shifted forward so save the last item to copy to the front.
            var overflowItem = data[data.Length - 1];

            // Shift data after index forward.
            var changeIndex = _end + index;
            if (changeIndex != data.Length)
            {
                data.Slice(changeIndex, data.Length - changeIndex - 1).CopyTo(data.Slice(changeIndex + 1));
            }

            // Shift data before index forward and set overflow item to start.
            data.Slice(0, _end).CopyTo(data.Slice(1));
            data[0] = overflowItem;

            // Set the actual item.
            data[internalIndex] = item;

            Increment(ref _end);
            _start = _end;
        }
        else
        {
            var internalIndex = index + _start;
            if (internalIndex > _buffer.Count)
            {
                internalIndex = internalIndex % _buffer.Count;
            }

            _buffer.Insert(internalIndex, item);
            if (internalIndex < _end)
            {
                Increment(ref _end);
                if (_end != _buffer.Count)
                {
                    _start = _end;
                }
            }
        }
    }

    public void RemoveAt(int index)
    {
        ValidateIndexInRange(index);

        var internalIndex = InternalIndex(index);
        _buffer.RemoveAt(internalIndex);
        if (internalIndex < _end)
        {
            Decrement(ref _end);
            _start = _end;
        }
    }

    private void ValidateIndexInRange(int index)
    {
        if (index >= Count)
        {
            throw new InvalidOperationException($"Cannot access index {index}. Buffer size is {Count}");
        }
    }

    public bool Remove(T item) => throw new NotImplementedException();

    public T this[int index]
    {
        get
        {
            ValidateIndexInRange(index);
            return _buffer[InternalIndex(index)];
        }
        set
        {
            ValidateIndexInRange(index);
            _buffer[InternalIndex(index)] = value;
        }
    }

    public void Add(T item)
    {
        if (IsFull)
        {
            _buffer[_end] = item;
            Increment(ref _end);
            _start = _end;
        }
        else
        {
            _buffer.Insert(_end, item);
            Increment(ref _end);
            if (_end != _buffer.Count)
            {
                _start = _end;
            }
        }
    }

    public void Clear()
    {
        _start = 0;
        _end = 0;
        _buffer.Clear();
    }

    public bool Contains(T item) => IndexOf(item) != -1;

    public void CopyTo(T[] array, int arrayIndex)
    {
        if (array.Length - arrayIndex < Count)
        {
            throw new ArgumentException("Array does not contain enough space for items");
        }

        for (var index = 0; index < Count; ++index)
        {
            array[index + arrayIndex] = this[index];
        }
    }

    public T[] ToArray()
    {
        if (IsEmpty)
        {
            return Array.Empty<T>();
        }

        var array = new T[Count];
        for (var index = 0; index < Count; ++index)
        {
            array[index] = this[index];
        }

        return array;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < Count; ++i)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private int InternalIndex(int index)
    {
        return (_start + index) % _buffer.Count;
    }

    private void Increment(ref int index)
    {
        if (++index < Capacity)
        {
            return;
        }

        index = 0;
    }

    private void Decrement(ref int index)
    {
        if (index <= 0)
        {
            index = Capacity - 1;
        }

        --index;
    }
}
