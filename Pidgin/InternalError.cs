using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Pidgin
{
    [StructLayout(LayoutKind.Auto)]
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
    }
}