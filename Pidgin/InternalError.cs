using System.Runtime.InteropServices;

namespace Pidgin;

[StructLayout(LayoutKind.Auto)]
internal readonly struct InternalError<TToken>
{
    public bool EOF { get; }

    public Maybe<TToken> Unexpected { get; }

    public int ErrorLocation { get; }

    public string? Message { get; }

    public InternalError(Maybe<TToken> unexpected, bool eof, int errorLocation, string? message)
    {
        Unexpected = unexpected;
        EOF = eof;
        ErrorLocation = errorLocation;
        Message = message;
    }
}
