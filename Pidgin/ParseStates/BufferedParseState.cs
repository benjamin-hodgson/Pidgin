using System;
using System.Buffers;

namespace Pidgin.ParseStates
{
    internal abstract class BufferedParseState<TToken> : BaseParseState<TToken>
    {
        private static readonly int InitialCapacity =
            typeof(TToken).Equals(typeof(char))
                ? 4096 / sizeof(char)
                : 4096;

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
                    _buffer = ArrayPool<TToken>.Shared.Rent(InitialCapacity);
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
            if (_bufPos < _bufSize)
            {
                // we are re-reading previously buffered input
                _bufPos++;
                if (_bufPos < _bufSize)
                {
                    _current = Maybe.Just(Buffer[_bufPos]);
                    return;
                }
            }
            else if (!IsBuffering)
            {
                if (_buffer != null)
                {
                    // an explanatory Haiku:
                    //
                    // At end of buffer,
                    // not right now writing to it.
                    // Buffer: discarded.
                    _bufPos = _bufSize = 0;
                    ArrayPool<TToken>.Shared.Return(_buffer);
                    _buffer = null;
                }
            }
            else if (_current.HasValue)
            {
                // don't forget the old value
                PushBuf(_current.GetValueOrDefault());
            }

            _current = AdvanceInput();
        }

        protected sealed override Bookmark GetBookmark()
            => new Bookmark(_bufPos, _sourcePos);
        protected sealed override void Rewind(Bookmark bookmark)
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
                throw new InvalidOperationException("Please report this as a bug in Pidgin! https://github.com/benjamin-hodgson/Pidgin/issues");
            }
            if (_bufSize == Buffer.Length)
            {
                // the buffer is full, allocate a new bigger one
                var newBuf = ArrayPool<TToken>.Shared.Rent(Buffer.Length * 2);
                Buffer.CopyTo(newBuf, 0);  // i wonder if a linked-list style paged approach would be faster here?
                ArrayPool<TToken>.Shared.Return(Buffer);
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
                ArrayPool<TToken>.Shared.Return(_buffer);
            }
            _buffer = null;
        }
    }
}
