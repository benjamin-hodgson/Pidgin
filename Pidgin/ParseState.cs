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
        private int _bufferIndex;
        private int _bufferLength;


        public ParseState(Func<TToken, SourcePos, SourcePos> posCalculator, ITokenStream<TToken> stream)
        {
            _posCalculator = posCalculator;
            _bookmarks = new PooledList<Bookmark>();
            _stream = stream;
            _consumedCount = 0;
            SourcePos = new SourcePos(1, 1);

            _bufferChunkSize = stream.ChunkSizeHint;
            _buffer = ArrayPool<TToken>.Shared.Rent(_bufferChunkSize);
            _bufferIndex = -1;
            _bufferLength = 0;

            _eof = default;
            _unexpected = default;
            _errorPos = default;
            _message = default;
            _expecteds = new PooledList<Expected<TToken>>();
            _expectedBookmarks = new PooledList<int>();
        }

        public bool HasCurrent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _bufferIndex >= 0 && _bufferIndex < _bufferLength;
            }
        }
        public TToken Current
        {
            get
            {
                return _buffer[_bufferIndex];
            }
        }

        public void Advance()
        {
            if (HasCurrent)
            {
                SourcePos = _posCalculator(Current, SourcePos);
                _consumedCount++;
            }


            _bufferIndex++;
            if (_bufferIndex == _bufferLength)
            {
                // we're at the end of the current chunk. Pull a new chunk from the stream
                var keepLength = _bookmarks.Count > 0
                    ? _consumedCount - _bookmarks[0].Value
                    : 0;

                if (keepLength >= _buffer.Length)
                {
                    var newBuffer = ArrayPool<TToken>.Shared.Rent(_buffer.Length * 2);

                    Array.Copy(_buffer, _bufferLength - keepLength, newBuffer, 0, keepLength);

                    ArrayPool<TToken>.Shared.Return(_buffer);
                    _buffer = newBuffer;
                }
                else
                {
                    Array.Copy(_buffer, _bufferLength - keepLength, _buffer, 0, keepLength);
                }
                _bufferIndex = _bufferLength = keepLength;
                _bufferLength += _stream.ReadInto(_buffer, _bufferLength, Math.Min(_bufferChunkSize, _buffer.Length - _bufferLength));
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

            if (delta > _bufferIndex)
            {
                throw new InvalidOperationException("Tried to rewind past the start of the input. Please report this as a bug in Pidgin!");
            }
            _bufferIndex -= delta;

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
