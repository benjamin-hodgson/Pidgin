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
        private PooledList<int> _bookmarks;
        private readonly ITokenStream<TToken> _stream;

        public int Location { get; private set; }  // how many tokens have been consumed in total?


        private readonly int _bufferChunkSize;
        private TToken[] _buffer;
        private int _currentIndex;
        private int _bufferedCount;
        private SourcePos _bufferStartSourcePos;


        public ParseState(Func<TToken, SourcePos, SourcePos> posCalculator, ITokenStream<TToken> stream)
        {
            _posCalculator = posCalculator;
            _bookmarks = new PooledList<int>();
            _stream = stream;

            Location = 0;

            _bufferChunkSize = stream.ChunkSizeHint;
            _buffer = ArrayPool<TToken>.Shared.Rent(_bufferChunkSize);
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
            Location += alreadyBufferedCount;
            _currentIndex += alreadyBufferedCount;
            count -= alreadyBufferedCount;

            Buffer(count);
            
            var bufferedCount = Math.Min(count, _bufferedCount - _currentIndex);
            Location += bufferedCount;
            _currentIndex += bufferedCount;
            count -= bufferedCount;
        }
        // if it returns a span shorter than count it's because you reached the end of the input
        public ReadOnlySpan<TToken> Peek(int count)
        {
            Buffer(count);
            return _buffer
                .AsSpan()
                .Slice(_currentIndex, Math.Min(_bufferedCount - _currentIndex, count));
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

            Location = bookmark;
        }

        public SourcePos ComputeSourcePos()
            => ComputeSourcePosAt(Location);

        private SourcePos ComputeSourcePosAt(int location)
        {
            var bufferStartLocation = Location - _currentIndex;
            if (location < bufferStartLocation)
            {
                throw new ArgumentOutOfRangeException(nameof(location), location, "Tried to compute a SourcePos from too far in the past. Please report this as a bug in Pidgin!");
            }
            if (location > bufferStartLocation + _bufferedCount)
            {
                throw new ArgumentOutOfRangeException(nameof(location), location, "Tried to compute a SourcePos from too far in the future. Please report this as a bug in Pidgin!");
            }

            var pos = _bufferStartSourcePos;
            for (var i = 0; i < location - bufferStartLocation; i++)
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
