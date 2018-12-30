using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Pidgin
{
    /// <summary>
    /// A mutable struct! Careful!
    /// </summary>
    internal struct PooledList<T>
    {
        // If T is not primitive, we should clear out the pooled arrays so as not to leak objects.
        // Ideally I'd look at typeof(T).IsManagedType but there's no such thing.
        private static readonly bool _needsClear = !typeof(T).GetTypeInfo().IsPrimitive;

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
            _size--;
            return _items[_size];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Shrink(int newCount)
        {
            if (newCount > _size || newCount < 0)
            {
                ThrowArgumentOutOfRangeException(nameof(newCount));
            }
            _size = newCount;
        }

        public ReadOnlySpan<T> AsSpan() => _items.AsSpan().Slice(0, _size);

        public IEnumerable<T> AsEnumerable()
            => (_items ?? Enumerable.Empty<T>()).Take(_size);

        public void Clear()
        {
            if (_items != null)
            {
                ArrayPool<T>.Shared.Return(_items, _needsClear);
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
                ArrayPool<T>.Shared.Return(_items, _needsClear);
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