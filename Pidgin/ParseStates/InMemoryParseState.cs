using System;

namespace Pidgin.ParseStates
{
    internal abstract class InMemoryParseState<TToken> : BookmarkParseState<TToken, Positioned<int>>
    {
        private readonly int _count;
        private readonly Func<TToken, SourcePos, SourcePos> _calculatePos;
        private int _pos = -1;
        private SourcePos _sourcePos = new SourcePos(1,1);
        private Maybe<TToken> _current = Maybe.Nothing<TToken>();

        protected InMemoryParseState(int count, Func<TToken, SourcePos, SourcePos> calculatePos)
        {
            _count = count;
            _calculatePos = calculatePos;
        }

        public sealed override Maybe<TToken> Peek() => _current;
        public sealed override void Advance()
        {
            if (_current.HasValue)
            {
                _sourcePos = _calculatePos(_current.GetValueOrDefault(), _sourcePos);
            }
            _pos++;
            SetCurrent();
        }
        protected sealed override Positioned<int> GetBookmark()
            => new Positioned<int>(_pos, _sourcePos);
        protected sealed override void Rewind(Positioned<int> bookmark)
        {
            _pos = bookmark.Value;
            SetCurrent();
            _sourcePos = bookmark.Pos;
        }

        public sealed override SourcePos SourcePos => _sourcePos;
        
        protected abstract TToken GetElement(int index);
        private void SetCurrent()
        {
            if (_pos >= _count || _pos < 0)
            {
                _current = Maybe.Nothing<TToken>();
                return;
            }
            _current = Maybe.Just(GetElement(_pos));
        }
    }
}