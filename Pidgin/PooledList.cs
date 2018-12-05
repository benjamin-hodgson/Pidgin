using System;
using System.Buffers;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Pidgin
{
    /// <summary>
    /// A mutable struct! Careful!
    /// </summary>
    internal struct PooledList<T>
    {
        private const int InitialCapacity = 16;
        private T[] _items;
        private int _size;

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _size;
            }
        }

        public PooledList(int initialCapacity)
        {
            _items = ArrayPool<T>.Shared.Rent(initialCapacity);
            _size = 0;
        }

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (index >= _size)
                {
                    ThrowArgumentOutOfRangeException(nameof(index));
                }
                return _items[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            GrowIfNecessary(1);
            _items[_size] = item;
            _size++;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(ImmutableArray<T> items)
        {
            GrowIfNecessary(items.Length);
            items.CopyTo(_items, _size);
            _size += items.Length;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(ReadOnlySpan<T> items)
        {
            GrowIfNecessary(items.Length);
            items.CopyTo(_items.AsSpan().Slice(_size));
            _size += items.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop()
        {
            if (_size == 0)
            {
                ThrowInvalidOperationException();
            }
            _size -= 1;
            var result = _items[_size];
            _items[_size] = default;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Shrink(int newCount)
        {
            if (newCount > _size || newCount < 0)
            {
                ThrowArgumentOutOfRangeException(nameof(newCount));
            }
            _items.AsSpan().Slice(newCount).Clear();
            _size = newCount;
        }

        public ReadOnlySpan<T> AsSpan() => _items.AsSpan().Slice(0, _size);

        public ImmutableSortedSet<T> ToImmutableSortedSet()
            => (_items ?? Enumerable.Empty<T>()).Take(_size).ToImmutableSortedSet();

        public void Clear()
        {
            if (_items != null)
            {
                _items.AsSpan().Clear();
                ArrayPool<T>.Shared.Return(_items);
            }
            _items = null;
            _size = 0;
        }

        public U Aggregate<U>(U seed, Func<U, T, U> func)
        {
            var z = seed;
            for (var i = 0; i < _size; i++)
            {
                z = func(z, _items[i]);
            }
            return z;
        }
        public U AggregateR<U>(U seed, Func<T, U, U> func)
        {
            var z = seed;
            for (var i = _size - 1; i >= 0; i--)
            {
                z = func(_items[i], z);
            }
            return z;
        }

        private void GrowIfNecessary(int additionalSpace)
        {
            if (_items == null)
            {
                _items = ArrayPool<T>.Shared.Rent(Math.Max(InitialCapacity, additionalSpace));
            }
            else if (_size == _items.Length)
            {
                var newBuffer = ArrayPool<T>.Shared.Rent(Math.Max(_items.Length * 2, _items.Length + additionalSpace));
                Array.Copy(_items, newBuffer, _items.Length);
                ArrayPool<T>.Shared.Return(_items);
                _items = newBuffer;
            }
        }
        private static void ThrowArgumentOutOfRangeException(string paramName)
        {
            throw new ArgumentOutOfRangeException(paramName);
        }
        private static void ThrowInvalidOperationException()
        {
            throw new InvalidOperationException();
        }
    }
}