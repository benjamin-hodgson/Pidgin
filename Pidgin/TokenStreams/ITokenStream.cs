using System;

namespace Pidgin.TokenStreams
{
    internal interface ITokenStream<TToken> : IDisposable
    {
        TToken Current { get; }
        bool MoveNext();

        void StartBuffering();
        void StopBuffering();
        bool RewindBy(int count);
    }
}
