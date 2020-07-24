using System;
using System.Collections.Generic;

namespace Pidgin.TokenStreams
{
    internal class EnumeratorTokenStream<TToken> : ITokenStream<TToken>
    {
        public int ChunkSizeHint => 16;
        private readonly IEnumerator<TToken> _input;

        public EnumeratorTokenStream(IEnumerator<TToken> input)
        {
            _input = input;
        }


        public int Read(Span<TToken> buffer)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                var hasNext = _input.MoveNext();
                if (!hasNext)
                {
                    return i;
                }
                buffer[i] = _input.Current;
            }
            return buffer.Length;
        }
    }
}