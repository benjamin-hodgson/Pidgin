using System;
using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Pidgin
{
    internal struct ExpectedCollector<TToken>
    {
        private static readonly bool _needsClear = RuntimeHelpers.IsReferenceOrContainsReferences<TToken>();

        private ArrayPool<Expected<TToken>> _arrayPool;
        private bool _discard;
        private Expected<TToken>[]? _items;
        private int _count;

        public ExpectedCollector(ArrayPool<Expected<TToken>> arrayPool, bool discard = false)
        {
            _arrayPool = arrayPool;
            _discard = discard;
            _items = null;
            _count = 0;
        }

        public void Add(Expected<TToken> item)
        {
            if (_discard)
            {
                return;
            }
            EnsureCapacity();
            _items![_count] = item;
            _count++;
        }
        public void AddIf(ref ExpectedCollector<TToken> items, bool shouldAdd)
        {
            if (!_discard && shouldAdd)
            {
                Add(ref items);
            }
        }
        public void Add(ImmutableArray<Expected<TToken>> items)
        {
            if (_discard)
            {
                return;
            }
            EnsureCapacity(items.Length);
            items.CopyTo(_items!, _count);
            _count += items.Length;
        }
        public void Add(ref ExpectedCollector<TToken> items)
        {
            if (_discard)
            {
                return;
            }
            EnsureCapacity(items._count);
            Array.Copy(items._items ?? Array.Empty<Expected<TToken>>(), 0, _items!, _count, items._count);
            _count += items._count;
        }
        public void Clear()
        {
            _count = 0;
        }
        public ImmutableArray<Expected<TToken>> ToImmutableArray()
            => ImmutableArray.Create(_items ?? Array.Empty<Expected<TToken>>(), 0, _count);

        private void EnsureCapacity(int count = 1)
        {
            if (_items == null)
            {
                _items = _arrayPool.Rent(Math.Max(count, 16));
            }
            if (_count + count >= _items.Length)
            {
                var newItems = _arrayPool.Rent(Math.Max(_items.Length * 2, _count + count));
                Array.Copy(_items, newItems, _count);
                _arrayPool.Return(_items, _needsClear);
                _items = newItems;
            }
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
    }
}