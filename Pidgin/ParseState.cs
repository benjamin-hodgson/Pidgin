using System;
using System.Buffers;
using System.Collections.Immutable;
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
        private bool _hasCurrent;
        public SourcePos SourcePos { get; private set; }
        
        
        public ParseState(Func<TToken, SourcePos, SourcePos> posCalculator, ITokenStream<TToken> stream)
        {
            _posCalculator = posCalculator;
            _bookmarks = new PooledList<Bookmark>();
            _stream = stream;
            _consumedCount = 0;
            _hasCurrent = false;
            SourcePos = new SourcePos(1, 1);

            _eof = default;
            _unexpected = default;
            _errorPos = default;
            _message = default;
            _expecteds = new PooledList<Expected<TToken>>();
            _expectedBookmarks = new PooledList<int>();
        }

        public Maybe<TToken> Peek() => _hasCurrent ? Maybe.Just(_stream.Current) : Maybe.Nothing<TToken>();

        public void Advance()
        {
            if (_hasCurrent)
            {
                SourcePos = _posCalculator(_stream.Current, SourcePos);
                _consumedCount++;
            }
            _hasCurrent = _stream.MoveNext();
        }
        

        public void PushBookmark()
        {
            if (_bookmarks.Count == 0)
            {
                _stream.StartBuffering();
            }
            _bookmarks.Add(new Bookmark(_consumedCount, SourcePos));
        }

        public void PopBookmark()
        {
            _bookmarks.Pop();
            if (_bookmarks.Count == 0)
            {
                _stream.StopBuffering();
            }
        }

        public void Rewind()
        {
            var bookmark = _bookmarks.Pop();
            
            var delta = _consumedCount - bookmark.Value;
            _hasCurrent = _stream.RewindBy(delta);

            _consumedCount = bookmark.Value;
            SourcePos = bookmark.Pos;
            
            if (_bookmarks.Count == 0)
            {
                _stream.StopBuffering();
            }
        }

        public void Dispose()
        {
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
