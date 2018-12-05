using System.Collections.Immutable;

namespace Pidgin
{
    internal readonly struct InternalError<TToken>
    {
        public bool EOF { get; }
        public Maybe<TToken> Unexpected { get; }
        public SourcePos ErrorPos { get; }
        public string Message { get; }

        internal InternalError(Maybe<TToken> unexpected, bool eof, SourcePos errorPos, string message)
        {
            Unexpected = unexpected;
            EOF = eof;
            ErrorPos = errorPos;
            Message = message;
        }

        public ParseError<TToken> Build(ImmutableSortedSet<Expected<TToken>> expecteds)
            => new ParseError<TToken>(Unexpected, EOF, expecteds, ErrorPos, Message);
    }
}