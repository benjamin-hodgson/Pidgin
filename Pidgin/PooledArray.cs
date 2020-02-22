using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;

namespace Pidgin
{
    [StructLayout(LayoutKind.Auto)]
    public readonly struct PooledArray<T>
    {
        private readonly T[] _array;
        private readonly int _length;

        public PooledArray(T[] array, int length)
        {
            _array = array;
            _length = length;
        }

        public Span<T> AsSpan()
            => _array.AsSpan().Slice(0, _length);

        public IEnumerable<T> AsEnumerable()
            => _array.Take(_length);

        public static PooledArray<T> From(ReadOnlySpan<T> span)
        {
            var array = ArrayPool<T>.Shared.Rent(span.Length);
            span.CopyTo(array);
            return new PooledArray<T>(array, span.Length);
        }

        public void Dispose(bool clearArray = false)
        {
            ArrayPool<T>.Shared.Return(_array, clearArray);
        }
    }
}
