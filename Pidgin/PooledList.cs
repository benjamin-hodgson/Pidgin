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
    /// A version of <see cref="List{T}"/> which uses an array pool.
    ///
    /// For efficiency, <see cref="PooledList{T}"/> is implemented as a mutable struct.
    /// It's intended to be passed by reference.
    /// </summary>
    /// <typeparam name="T">The type of elements of the list</typeparam>
    public struct PooledList<T> : IDisposable, IList<T>
    {
        private static readonly bool _needsClear = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
        private const int InitialCapacity = 16;

        private ArrayPool<T> _arrayPool;
        private T[]? _items;
        private int _count;

        /// <summary>The number of elements in the list</summary>
        /// <returns>The number of elements in the list</returns>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _count;
            }
        }

        /// <summary>Returns false</summary>
        /// <returns>False</returns>
        public bool IsReadOnly => false;

        /// <summary>Creates a <see cref="PooledList{T}"/> which uses the supplied <see cref="ArrayPool{T}"/>.</summary>
        public PooledList(ArrayPool<T> arrayPool)
        {
            _arrayPool = arrayPool;
            _items = null;
            _count = 0;
        }

        /// <summary>Gets or sets the element at index <paramref name="index"/>.</summary>
        /// <param name="index">The index</param>
        /// <returns>The element at index <paramref name="index"/>.</returns>
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

        /// <summary>Adds an item to the end of the list.</summary>
        /// <param name="item">The item to add</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            GrowIfNecessary(1);
            _items![_count] = item;
            _count++;
        }
        /// <summary>Adds a collection of items to the end of the list.</summary>
        /// <param name="items">The items to add</param>
        public void AddRange(ImmutableArray<T> items)
        {
            GrowIfNecessary(items.Length);
            items.CopyTo(_items!, _count);
            _count += items.Length;
        }
        /// <summary>Adds a collection of items to the end of the list.</summary>
        /// <param name="items">The items to add</param>
        public void AddRange(ReadOnlySpan<T> items)
        {
            GrowIfNecessary(items.Length);
            items.CopyTo(_items.AsSpan().Slice(_count));
            _count += items.Length;
        }
        /// <summary>Adds a collection of items to the end of the list.</summary>
        /// <param name="items">The items to add</param>
        public void AddRange(ICollection<T> items)
        {
            GrowIfNecessary(items.Count);
            items.CopyTo(_items, _count);
            _count += items.Count;
        }
        /// <summary>Adds a collection of items to the end of the list.</summary>
        /// <param name="items">The items to add</param>
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

        /// <summary>Removes and returns an item from the end of the list.</summary>
        /// <exception cref="InvalidOperationException">The list is empty.</exception>
        /// <returns>The last item in the list.</returns>
        public T Pop()
        {
            if (_count == 0)
            {
                ThrowInvalidOperationException();
            }
            _count--;
            return _items![_count];
        }

        /// <summary>Returns a <see cref="Span{T}"/> view of the list.</summary>
        /// <returns>A <see cref="Span{T}"/> view of the list.</returns>
        public Span<T> AsSpan() => _items.AsSpan().Slice(0, _count);

        /// <summary>
        /// Searches for <paramref name="item"/> in the list and returns its index.
        /// Returns <c>-1</c> if the <paramref name="item"/> is missing.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <returns>The index of <paramref name="item"/>, or <c>-1</c> if it is missing.</returns>
        public int IndexOf(T item)
        {
            if (_count == 0)
            {
                return -1;
            }
            return Array.IndexOf(_items!, item, 0, _count);
        }

        /// <summary>
        /// Inserts <paramref name="item"/> into the list at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index at which to insert the item.</param>
        /// <param name="item">The item to insert.</param>
        /// <exception cref="ArgumentOutOfRangeException">The index is outside the bounds of the list.</exception>
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

        /// <summary>
        /// Removes the item at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index from which to remove the item.</param>
        /// <exception cref="ArgumentOutOfRangeException">The index is outside the bounds of the list.</exception>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            _count--;
            Array.Copy(_items!, index + 1, _items!, index, _count - index);
        }

        /// <summary>
        /// Searches for <paramref name="item"/> in the list.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <returns>True if the item is in the list, false if it is not.</returns>
        public bool Contains(T item)
            => Array.IndexOf(_items!, item, 0, _count) >= 0;

        /// <summary>
        /// Copies the list into an array.
        /// </summary>
        /// <param name="array">The destination array to copy the list into.</param>
        /// <param name="arrayIndex">The starting index in the destination array.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> was null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> was less than 0.</exception>
        /// <exception cref="ArgumentException">There was not enough space in the <paramref name="array"/>.</exception>
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
            Array.Copy(_items!, 0, array, arrayIndex, _count);
        }

        /// <summary>
        /// Searches for <paramref name="item"/> in the list and removes it.
        /// Returns <c>false</c> if the <paramref name="item"/> is missing.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <returns>True if the item was removed, false if it was missing.</returns>
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

        /// <summary>
        /// Empties the list.
        /// </summary>
        public void Clear()
        {
            _count = 0;
        }

        /// <summary>
        /// Returns any allocated memory to the pool.
        /// </summary>
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
        [MemberNotNull(nameof(_arrayPool), nameof(_items))]
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
        [MemberNotNull(nameof(_items))]
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