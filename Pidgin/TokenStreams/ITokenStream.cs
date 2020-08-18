using System;

namespace Pidgin.TokenStreams
{
    internal interface ITokenStream<TToken>
    {
        int ChunkSizeHint => 1024;
        int Read(Span<TToken> buffer);
        void Return(ReadOnlySpan<TToken> leftovers) { }
    }
}
