using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Pidgin
{
    internal partial class ExpectedCollector<TToken> : ICollection<Expected<TToken>>
    {
        private static readonly bool _needsClear = RuntimeHelpers.IsReferenceOrContainsReferences<TToken>();

        private ArrayPool<Expected<TToken>> _arrayPool;
        private Expected<TToken>[]? _items;

        public int Count { get; private set; }

        public bool IsReadOnly => false;

        private ExpectedCollector(ArrayPool<Expected<TToken>> arrayPool)
        {
            _arrayPool = arrayPool;
            _items = null;
            Count = 0;
        }

        public void Add(Expected<TToken> item)
        {
            EnsureCapacity();
            _items![Count] = item;
            Count++;
        }
        
        public void AddRange(ImmutableArray<Expected<TToken>> items)
        {
            EnsureCapacity(items.Length);
            items.CopyTo(_items!, Count);
            Count += items.Length;
        }

        public void AddRange(ExpectedCollector<TToken> items)
        {
            EnsureCapacity(items.Count);
            Array.Copy(items._items ?? Array.Empty<Expected<TToken>>(), 0, _items!, Count, items.Count);
            Count += items.Count;
        }

        public void AddRange(IEnumerable<Expected<TToken>> items)
        {
            if (items is ExpectedCollector<TToken> c)
            {
                AddRange(c);
                return;
            }
            if (items is ImmutableArray<Expected<TToken>> a)
            {
                AddRange(a);
                return;
            }
            foreach (var x in items)
            {
                Add(x);
            }
        }

        public void Clear()
        {
            Count = 0;
        }

        public ImmutableArray<Expected<TToken>> ToImmutableArray()
            => ImmutableArray.Create(_items ?? Array.Empty<Expected<TToken>>(), 0, Count);

        private void Dispose()
        {
            if (_items != null)
            {
                _arrayPool.Return(_items, _needsClear);
            }
            _items = null;
            Count = 0;
        }

        public bool Contains(Expected<TToken> item)
            => Array.IndexOf(_items, item, 0, Count) >= 0;

        public void CopyTo(Expected<TToken>[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }
            if (array.Length - arrayIndex > Count)
            {
                throw new ArgumentException();
            }
            Array.Copy(_items, 0, array, arrayIndex, Count);
        }

        public bool Remove(Expected<TToken> item)
        {
            var i = Array.IndexOf(_items, item, 0, Count);
            if (i >= 0)
            {
                Array.Copy(_items, i + 1, _items, i, Count - i);
                Count--;
                return true;
            }
            return false;
        }

        public IEnumerator<Expected<TToken>> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return _items![i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void EnsureCapacity(int count = 1)
        {
            if (_items == null)
            {
                _items = _arrayPool.Rent(Math.Max(count, 16));
            }
            if (Count + count >= _items.Length)
            {
                var newItems = _arrayPool.Rent(Math.Max(_items.Length * 2, Count + count));
                Array.Copy(_items, newItems, Count);
                _arrayPool.Return(_items, _needsClear);
                _items = newItems;
            }
        }
    }
}