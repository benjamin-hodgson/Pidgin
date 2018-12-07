using System;
using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Pidgin.TokenStreams;

namespace Pidgin
{
    internal partial struct ParseState<TToken>
    {
        private bool _eof;
        private Maybe<TToken> _unexpected;
        private SourcePos _errorPos;
        private string _message;
        private PooledList<Expected<TToken>> _expecteds;
        private PooledList<int> _expectedBookmarks;
        
        public InternalError<TToken> Error
        {
            get
            {
                return new InternalError<TToken>(_unexpected, _eof, _errorPos, _message);
            }
            set
            {
                _unexpected = value.Unexpected;
                _eof = value.EOF;
                _errorPos = value.ErrorPos;
                _message = value.Message;
            }
        }
        public void BeginExpectedTran()
        {
            _expectedBookmarks.Add(_expecteds.Count);
        }
        public void AddExpected(Expected<TToken> expected)
        {
            _expecteds.Add(expected);
        }
        public void AddExpected(ImmutableArray<Expected<TToken>> expected)
        {
            _expecteds.AddRange(expected);
        }
        public void AddExpected(ReadOnlySpan<Expected<TToken>> expected)
        {
            _expecteds.AddRange(expected);
        }
        public void EndExpectedTran(bool commit)
        {
            var newCount = _expectedBookmarks.Pop();
            if (!commit)
            {
                _expecteds.Shrink(newCount);
            }
        }
        // todo: pool this
        public Expected<TToken>[] ExpectedTranState()
            => _expecteds
                .AsSpan()
                .Slice(_expectedBookmarks[_expectedBookmarks.Count - 1])
                .ToArray();
        public ParseError<TToken> BuildError()
            => new ParseError<TToken>(_unexpected, _eof, _expecteds.ToImmutableSortedSet(), _errorPos, _message);
    }
}
