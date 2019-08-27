using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Pidgin.TokenStreams;

namespace Pidgin
{
    /// <summary>
    /// A mutable struct! Careful!
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    internal partial struct ParseState<TToken>
    {
        private readonly Func<TToken, SourcePos, SourcePos> _posCalculator;
        private readonly ITokenStream<TToken> _stream;
        private readonly int _bufferChunkSize;

        private TToken[] _buffer;
        private int _bufferStartLocation;  // how many tokens had been consumed up to the start of the buffer?
        private int _currentIndex;
        private int _bufferedCount;
        private SourcePos _bufferStartSourcePos;

        // a monotonic stack of locations.
        // I know you'll forget this, so: you can't make this into a stack of _currentIndexes,
        // because dropping the buffer's prefix would invalidate the bookmarks
        private PooledList<int> _bookmarks;


        public ParseState(Func<TToken, SourcePos, SourcePos> posCalculator, ITokenStream<TToken> stream)
        {
            _posCalculator = posCalculator;
            _bookmarks = new PooledList<int>();
            _stream = stream;

            _bufferChunkSize = stream.ChunkSizeHint;
            _buffer = ArrayPool<TToken>.Shared.Rent(_bufferChunkSize);
            _bufferStartLocation = 0;
            _currentIndex = 0;
            _bufferedCount = 0;
            _bufferStartSourcePos = new SourcePos(1,1);

            _eof = default;
            _unexpected = default;
            _errorLocation = default;
            _message = default;
            _expecteds = new PooledList<Expected<TToken>>();
            _expectedBookmarks = new PooledList<int>();

            Buffer(0);
        }

        /// <summary>
        /// How many tokens have been consumed in total?
        /// </summary>
        /// <value></value>
        public int Location
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _bufferStartLocation + _currentIndex;
            }
        }

        public bool HasCurrent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _currentIndex < _bufferedCount;
            }
        }
        public TToken Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _buffer[_currentIndex];
            }
        }

        public void Advance(int count = 1)
        {
            var alreadyBufferedCount = Math.Min(count, _bufferedCount - _currentIndex);
            _currentIndex += alreadyBufferedCount;
            count -= alreadyBufferedCount;

            Buffer(count);
            
            var bufferedCount = Math.Min(count, _bufferedCount - _currentIndex);
            _currentIndex += bufferedCount;
            count -= bufferedCount;
        }
        // if it returns a span shorter than count it's because you reached the end of the input
        public ReadOnlySpan<TToken> LookAhead(int count)
        {
            Buffer(count);
            return _buffer.AsSpan().Slice(_currentIndex, Math.Min(_bufferedCount - _currentIndex, count));
        }
        // if it returns a span shorter than count it's because you looked further back than the buffer goes
        public ReadOnlySpan<TToken> LookBehind(int count)
        {
            var start = Math.Max(0, _currentIndex - count);
            return _buffer.AsSpan().Slice(start, _currentIndex - start);
        }

        // postcondition: bufferedLength >= _currentIndex + min(readAhead, AmountLeft(_stream))
        private void Buffer(int readAhead)
        {
            var readAheadTo = _currentIndex + readAhead;
            if (readAheadTo >= _bufferedCount)
            {
                // we're about to read past the end of the current chunk. Pull a new chunk from the stream
                var keepSeenLength = _bookmarks.Count > 0
                    ? Location - _bookmarks[0]
                    : 0;
                var keepFrom = _currentIndex - keepSeenLength;
                var keepLength = _bufferedCount - keepFrom;
                var amountToRead = Math.Max(_bufferChunkSize, readAheadTo - keepFrom);
                var newBufferLength = _bufferedCount + amountToRead;

                //                  _currentIndex
                //                        |
                //                        | _bufferedCount
                //              keepFrom  |      |
                //                 |      |      | readAheadTo
                //                 |      |      |    |
                //              abcdefghijklmnopqrstuvwxyz
                //       readAhead        |-----------|
                //  keepSeenLength |------|
                //      keepLength |-------------|
                //    amountToRead               |----|
                // newBufferLength |------------------|


                for (var i = 0; i < keepFrom; i++)
                {
                    _bufferStartSourcePos = _posCalculator(_buffer[i], _bufferStartSourcePos);
                }

                if (newBufferLength > _buffer.Length)
                {
                    // grow the buffer
                    var newBuffer = ArrayPool<TToken>.Shared.Rent(Math.Max(newBufferLength, _buffer.Length * 2));

                    Array.Copy(_buffer, keepFrom, newBuffer, 0, keepLength);

                    ArrayPool<TToken>.Shared.Return(_buffer);
                    _buffer = newBuffer;
                }
                else if (keepFrom != 0 && keepLength != 0)
                {
                    // move the buffer's contents to the start

                    // todo: find out how expensive this Copy tends to be.
                    // Could prevent it by using a ring buffer, but might make reads slower
                    Array.Copy(_buffer, keepFrom, _buffer, 0, keepLength);
                }
                _bufferStartLocation += keepFrom;
                _currentIndex = keepSeenLength;
                _bufferedCount = keepLength;
                _bufferedCount += _stream.ReadInto(_buffer, _bufferedCount, amountToRead);
            }
        }
        
        public void PushBookmark()
        {
            _bookmarks.Add(Location);
        }

        public void PopBookmark()
        {
            _bookmarks.Pop();
        }

        public void Rewind()
        {
            var bookmark = _bookmarks.Pop();
            
            var delta = Location - bookmark;

            if (delta > _currentIndex)
            {
                throw new InvalidOperationException("Tried to rewind past the start of the input. Please report this as a bug in Pidgin!");
            }
            _currentIndex -= delta;
        }

        public SourcePos ComputeSourcePos()
            => ComputeSourcePosAt(Location);

        private SourcePos ComputeSourcePosAt(int location)
        {
            if (location < _bufferStartLocation)
            {
                throw new ArgumentOutOfRangeException(nameof(location), location, "Tried to compute a SourcePos from too far in the past. Please report this as a bug in Pidgin!");
            }
            if (location > _bufferStartLocation + _bufferedCount)
            {
                throw new ArgumentOutOfRangeException(nameof(location), location, "Tried to compute a SourcePos from too far in the future. Please report this as a bug in Pidgin!");
            }

            var pos = _bufferStartSourcePos;
            for (var i = 0; i < location - _bufferStartLocation; i++)
            {
                pos = _posCalculator(_buffer[i], pos);
            }
            return pos;
        }

        public void Dispose()
        {
            if (_buffer != null)
            {
                ArrayPool<TToken>.Shared.Return(_buffer);
                _buffer = null;
            }
            _stream.Dispose();
            _bookmarks.Clear();
            _expecteds.Clear();
            _expectedBookmarks.Clear();
        }
    }
}
