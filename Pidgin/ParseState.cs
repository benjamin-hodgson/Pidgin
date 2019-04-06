using System;
using System.Buffers;
using System.Collections.Immutable;
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
        private PooledList<Bookmark> _bookmarks;
        private readonly ITokenStream<TToken> _stream;
        private int _consumedCount;
        public SourcePos SourcePos { get; private set; }


        private readonly int _bufferChunkSize;
        private TToken[] _buffer;
        private int _currentIndex;
        private int _bufferedCount;


        public ParseState(Func<TToken, SourcePos, SourcePos> posCalculator, ITokenStream<TToken> stream)
        {
            _posCalculator = posCalculator;
            _bookmarks = new PooledList<Bookmark>();
            _stream = stream;
            _consumedCount = 0;
            SourcePos = new SourcePos(1, 1);

            _bufferChunkSize = stream.ChunkSizeHint;
            _buffer = ArrayPool<TToken>.Shared.Rent(_bufferChunkSize);
            _currentIndex = 0;
            _bufferedCount = 0;

            _eof = default;
            _unexpected = default;
            _errorPos = default;
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
            while (count > 0 && HasCurrent)
            {
                SourcePos = _posCalculator(Current, SourcePos);
                _consumedCount++;
                _currentIndex++;
                count--;
            }
            Buffer(count);
            while (count > 0 && HasCurrent)
            {
                SourcePos = _posCalculator(Current, SourcePos);
                _consumedCount++;
                _currentIndex++;
                count--;
            }
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
                    ? _consumedCount - _bookmarks[0].Value
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
            _bookmarks.Add(new Bookmark(_consumedCount, SourcePos));
        }

        public void PopBookmark()
        {
            _bookmarks.Pop();
        }

        public void Rewind()
        {
            var bookmark = _bookmarks.Pop();
            
            var delta = _consumedCount - bookmark.Value;

            if (delta > _currentIndex)
            {
                throw new InvalidOperationException("Tried to rewind past the start of the input. Please report this as a bug in Pidgin!");
            }
            _currentIndex -= delta;

            _consumedCount = bookmark.Value;
            SourcePos = bookmark.Pos;
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


        private struct Bookmark
        {
            public int Value { get; }
            public SourcePos Pos { get; }

            public Bookmark(int value, SourcePos sourcePos)
            {
                Value = value;
                Pos = sourcePos;
            }
        }
    }
}
