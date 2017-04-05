using System;

namespace Pidgin.ParseStates
{
    internal abstract class BufferedParseState<TToken> : BookmarkParseState<TToken, Positioned<int>>
    {
        // TODO: what is a good size for initialCapacity?
        private const int InitialCapacity = 16;

        private int _bufSize = 0;
        private int _bufPos = 0;
        private SourcePos _sourcePos = new SourcePos(1, 1);

        // alloc on first use
        private TToken[] _buffer = null;
        // no need for locking, it (shouldn't be) possible
        // for multiple threads to access the same ParseState
        private TToken[] Buffer 
        {
            get
            {
                if (_buffer == null)
                {
                    _buffer = BufferPool<TToken>.Acquire(InitialCapacity);
                }
                return _buffer;
            }
        }
        private Maybe<TToken> _current;
        private readonly Func<TToken, SourcePos, SourcePos> _calculatePos;

        protected BufferedParseState(Func<TToken, SourcePos, SourcePos> calculatePos)
        {
            _calculatePos = calculatePos;
        }

        public sealed override Maybe<TToken> Peek() => _current;
        public sealed override SourcePos SourcePos => _sourcePos;

        protected abstract Maybe<TToken> AdvanceInput();
        public sealed override void Advance()
        {
            if (_current.HasValue)
            {
                _sourcePos = _calculatePos(_current.GetValueOrDefault(), _sourcePos);
            }
            if (_bufPos < _bufSize - 1)
            {
                // we are re-reading previously buffered input
                _bufPos++;
                _current = Maybe.Just(Buffer[_bufPos]);
                return;
            }
            _bufPos = _bufSize;  // just to be sure
            if (!IsBuffering)
            {
                // we're at the end of the buffer,
                // and we're not currently writing to the buffer,
                // so the buffer can be discarded
                _bufPos = _bufSize = 0;
                BufferPool<TToken>.Release(Buffer);
                _buffer = null;
            }
            else if (_current.HasValue)
            {
                // don't forget the old value
                PushBuf(_current.GetValueOrDefault());
            }

            _current = AdvanceInput();
        }

        protected sealed override Positioned<int> GetBookmark()
            => new Positioned<int>(_bufPos, _sourcePos);
        protected sealed override void Rewind(Positioned<int> bookmark)
        {
            if (_bufPos == _bufSize && _current.HasValue)
            {
                // we were writing to the buffer before the rewind,
                // so don't forget to store the last value
                PushBuf(_current.GetValueOrDefault());
            }
            _bufPos = bookmark.Value;
            _current = _bufPos >= _bufSize
                ? Maybe.Nothing<TToken>()
                : Maybe.Just(Buffer[_bufPos]);
            _sourcePos = bookmark.Pos;
        }

        private bool IsBuffering => HasBookmarks;

        private void PushBuf(TToken token)
        {
            if (_bufPos != _bufSize)
            {
                // we're not at the end of the buffer
                throw new InvalidOperationException();
            }
            if (_bufSize == Buffer.Length)
            {
                // the array is full, allocate a new bigger one
                var newBuf = BufferPool<TToken>.Acquire(Buffer.Length * 2);
                Buffer.CopyTo(newBuf, 0);
                BufferPool<TToken>.Release(Buffer);
                _buffer = newBuf;
            }
            Buffer[_bufSize++] = token;
            _bufPos = _bufSize;
        }


        ~BufferedParseState()
        {
            Dispose();
        }
        public override void Dispose()
        {
            if (_buffer != null)
            {
                BufferPool<TToken>.Release(_buffer);
            }
            _buffer = null;
        }
    }
}
