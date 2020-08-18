using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Pidgin.TokenStreams
{
    internal class ResumableTokenStream<TToken> : ITokenStream<TToken>, IDisposable
    {
        private static readonly bool _needsClear = RuntimeHelpers.IsReferenceOrContainsReferences<TToken>();
        private readonly ArrayPool<TToken> _pool;
        private readonly ITokenStream<TToken> _next;
        private TToken[]? _buffer = null;
        private int _bufferStart = 0;  // amount of empty space at left-hand end of _buffer, aka index of first value

        public ResumableTokenStream(ITokenStream<TToken> next, ArrayPool<TToken>? pool = null)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            _next = next;
            _pool = pool ?? ArrayPool<TToken>.Shared;
        }

        public int Read(Span<TToken> buffer)
        {
            var bufferedCount = 0;
            if (_buffer != null && _bufferStart < _buffer.Length)
            {
                bufferedCount = Math.Min(_buffer.Length - _bufferStart, buffer.Length);
                _buffer.AsSpan().Slice(_bufferStart, bufferedCount).CopyTo(buffer);
                _bufferStart += bufferedCount;
            }
            return bufferedCount + _next.Read(buffer.Slice(bufferedCount));
        }

        public void Return(ReadOnlySpan<TToken> leftovers)
        {
            if (leftovers.Length == 0)
            {
                return;
            }
            if (_buffer == null)
            {
                _buffer = _pool.Rent(leftovers.Length);
                _bufferStart = _buffer.Length;
            }
            if (_bufferStart < leftovers.Length)
            {
                var bufferedCount = _buffer.Length - _bufferStart;
                var newBuffer = _pool.Rent(bufferedCount + leftovers.Length);
                var newBufferStart = newBuffer.Length - bufferedCount;

                Array.Copy(_buffer, _bufferStart, newBuffer, newBufferStart, bufferedCount);
                
                _pool.Return(_buffer, _needsClear);
                _buffer = newBuffer;
                _bufferStart = newBufferStart;
            }
            _bufferStart -= leftovers.Length;
            leftovers.CopyTo(_buffer.AsSpan().Slice(_bufferStart));
        }

        public void Dispose()
        {
            if (_buffer != null)
            {
                _pool.Return(_buffer, _needsClear);
                _bufferStart = 0;
            }
        }
    }
}
