using System;

namespace Pidgin.ParseStates
{
    internal interface IParseState<TToken> : IDisposable
    {
        Maybe<TToken> Peek();
        void Advance();
        void PushBookmark();
        void PopBookmark();
        void Rewind();
        SourcePos SourcePos { get; }
        ParseError<TToken> Error { get; set; }
    }
}
