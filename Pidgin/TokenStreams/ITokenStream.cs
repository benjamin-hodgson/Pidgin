using System;

namespace Pidgin.TokenStreams
{
    public interface ITokenStream<TToken> : IDisposable
    {
        int ChunkSizeHint { get; }

        int ReadInto(TToken[] buffer, int startIndex, int length);
    }
}
