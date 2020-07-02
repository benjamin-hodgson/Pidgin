using System;

namespace Pidgin.TokenStreams
{
    internal interface ITokenStream<TToken> : IDisposable
    {
        int ChunkSizeHint => 1024;
        int ReadInto(Span<TToken> buffer);
    }
}
