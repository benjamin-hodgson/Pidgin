using System.Collections.Generic;

namespace Pidgin.ParseStates
{
    internal abstract class BaseParseState<TToken> : IParseState<TToken>
    {
        private readonly Stack<Bookmark> _bookmarks = new Stack<Bookmark>();

        public abstract Maybe<TToken> Peek();
        public abstract void Advance();
        public abstract SourcePos SourcePos { get; }
        protected abstract Bookmark GetBookmark();
        protected abstract void Rewind(Bookmark bookmark);

        public void PushBookmark()
        {
            _bookmarks.Push(GetBookmark());
        }
        public void PopBookmark()
        {
            _bookmarks.Pop();
        }
        public void Rewind()
        {
            Rewind(_bookmarks.Pop());
        }
        protected bool HasBookmarks => _bookmarks.Count != 0;

        public virtual void Dispose()
        {
        }

        public ParseError<TToken> Error { get; set; } = default(ParseError<TToken>);
    }
}
