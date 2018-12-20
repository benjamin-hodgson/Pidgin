using System;

namespace Pidgin.TokenStreams
{
    internal interface ITokenStream<TToken> : IDisposable
    {
        int ChunkSizeHint { get; }

        int ReadInto(TToken[] buffer, int startIndex, int length);
    }
}
