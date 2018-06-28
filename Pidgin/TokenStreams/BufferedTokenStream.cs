using System;
using System.Buffers;
using System.IO;

namespace Pidgin.TokenStreams
{
    internal abstract class BufferedTokenStream<TToken> : ITokenStream<TToken>
    {
        protected readonly int _chunkSize;
        protected TToken[] _buffer;
        protected int _index;
        private int _length;
        private bool _retainBuffer;

        public BufferedTokenStream(int chunkSize)
        {
            _chunkSize = chunkSize;
            _buffer = ArrayPool<TToken>.Shared.Rent(_chunkSize);
            _index = -1;
            _length = 0;
            _retainBuffer = false;
        }

        protected abstract int Read();

        public TToken Current => _buffer[_index];

        public bool MoveNext()
        {
            _index++;
            if (_index == _length)
            {
                // we're at the end of the current chunk. Pull a new chunk from the stream
                if (_retainBuffer)
                {
                    // extend the current buffer

                    if (_buffer.Length == _length)
                    {
                        // buffer is full. Rent more space from the array pool
                        var newBuffer = ArrayPool<TToken>.Shared.Rent(_buffer.Length * 2);
                        Array.Copy(_buffer, newBuffer, _buffer.Length);
                        ArrayPool<TToken>.Shared.Return(_buffer);
                        _buffer = newBuffer;
                    }
                    
                    _length += Read();
                }
                else  // it's safe to discard the buffer and overwrite with a new chunk
                {
                    _index = 0;
                    _length = Read();
                }
            }
            return _index < _length;
        }

        public bool RewindBy(int count)
        {
            if (count > _index)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Please report this as a bug in Pidgin!");
            }
            _index -= count;
            return _index < _length;
        }

        public void StartBuffering()
        {
            _retainBuffer = true;
        }

        public void StopBuffering()
        {
            _retainBuffer = false;
        }

        public void Dispose()
        {
            if (_buffer != null)
            {
                ArrayPool<TToken>.Shared.Return(_buffer);
                _buffer = null;
            }
        }
    }
}