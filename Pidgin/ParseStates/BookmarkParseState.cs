using System.Collections.Generic;

namespace Pidgin.ParseStates
{
    internal abstract class BookmarkParseState<TToken, TBookmark> : IParseState<TToken>
    {
        private readonly Stack<TBookmark> _bookmarks = new Stack<TBookmark>();

        public abstract Maybe<TToken> Peek();
        public abstract void Advance();
        public abstract SourcePos SourcePos { get; }
        protected abstract TBookmark GetBookmark();
        protected abstract void Rewind(TBookmark bookmark);

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
    }
}
