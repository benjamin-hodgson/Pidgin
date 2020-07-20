using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Pidgin
{
    /// <summary>
    /// A mutable struct! Careful!
    /// </summary>
    internal struct PooledList<T> : IDisposable, IList<T>
    {
        private static readonly bool _needsClear = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
        private const int InitialCapacity = 16;

        private ArrayPool<T> _arrayPool;
        private T[]? _items;
        private int _count;

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _count;
            }
        }

        public bool IsReadOnly => false;

        public PooledList(ArrayPool<T> arrayPool)
        {
            _arrayPool = arrayPool;
            _items = null;
            _count = 0;
        }

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (index >= _count)
                {
                    ThrowArgumentOutOfRangeException(nameof(index));
                }
                return _items![index];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (index >= _count)
                {
                    ThrowArgumentOutOfRangeException(nameof(index));
                }
                _items![index] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            GrowIfNecessary(1);
            _items![_count] = item;
            _count++;
        }
        public void AddRange(ImmutableArray<T> items)
        {
            GrowIfNecessary(items.Length);
            items.CopyTo(_items, _count);
            _count += items.Length;
        }
        public void AddRange(ReadOnlySpan<T> items)
        {
            GrowIfNecessary(items.Length);
            items.CopyTo(_items.AsSpan().Slice(_count));
            _count += items.Length;
        }
        public void AddRange(ICollection<T> items)
        {
            GrowIfNecessary(items.Count);
            items.CopyTo(_items, _count);
            _count += items.Count;
        }
        public void AddRange(IEnumerable<T> items)
        {
            switch (items)
            {
                case T[] a:
                    AddRange(a.AsSpan());
                    return;
                case ImmutableArray<T> i:
                    AddRange(i);
                    return;
                case ICollection<T> c:
                    AddRange(c);
                    return;
                default:
                    foreach (var item in items)
                    {
                        Add(item);
                    }
                    return;
            }
        }

        public T Pop()
        {
            if (_count == 0)
            {
                ThrowInvalidOperationException();
            }
            _count--;
            return _items![_count];
        }

        public Span<T> AsSpan() => _items.AsSpan().Slice(0, _count);

        public int IndexOf(T item)
        {
            if (_count == 0)
            {
                return -1;
            }
            return Array.IndexOf(_items, item, 0, _count);
        }

        public void Insert(int index, T item)
        {
            if (index < 0 || index > _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            GrowIfNecessary(1);
            Array.Copy(_items, index, _items, index + 1, _count - index);
            _items![index] = item;
            _count++;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            _count--;
            Array.Copy(_items, index + 1, _items, index, _count - index);
        }

        public bool Contains(T item)
            => Array.IndexOf(_items, item, 0, _count) >= 0;

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }
            if (_count > array.Length - arrayIndex)
            {
                throw new ArgumentException();
            }
            Array.Copy(_items, 0, array, arrayIndex, _count);
        }

        public bool Remove(T item)
        {
            var ix = IndexOf(item);
            if (ix < 0)
            {
                return false;
            }
            RemoveAt(ix);
            return true;
        }

        public void Clear()
        {
            _count = 0;
        }

        public void Dispose()
        {
            if (_items != null)
            {
                _arrayPool.Return(_items, _needsClear);
            }
            _items = null;
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GrowIfNecessary(int additionalSpace)
        {
            if (_arrayPool == null)
            {
                _arrayPool = ArrayPool<T>.Shared;
            }
            if (_items == null)
            {
                Init(additionalSpace);
            }
            else if (_count + additionalSpace >= _items.Length)
            {
                Grow(additionalSpace);
            }
        }
        private void Init(int space)
        {
            _items = _arrayPool.Rent(Math.Max(InitialCapacity, space));
        }

        private void Grow(int additionalSpace)
        {
            var newBuffer = _arrayPool.Rent(Math.Max(_items!.Length * 2, _count + additionalSpace));
            Array.Copy(_items, newBuffer, _count);
            _arrayPool.Return(_items, _needsClear);
            _items = newBuffer;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            for (var i = 0; i < _count; i++)
            {
                yield return _items![i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable<T>)this).GetEnumerator();

        [DoesNotReturn]
        private static void ThrowArgumentOutOfRangeException(string paramName)
        {
            throw CreateArgumentOutOfRangeException(paramName);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception CreateArgumentOutOfRangeException(string paramName)
            => new ArgumentOutOfRangeException(paramName);

        [DoesNotReturn]
        private static void ThrowInvalidOperationException()
        {
            throw CreateInvalidOperationException();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception CreateInvalidOperationException()
            => new InvalidOperationException();
    }
}