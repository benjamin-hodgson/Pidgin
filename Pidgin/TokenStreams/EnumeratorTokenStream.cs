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


        public int ReadInto(TToken[] buffer, int startIndex, int length)
        {
            for (var i = 0; i < length; i++)
            {
                var hasNext = _input.MoveNext();
                if (!hasNext)
                {
                    return i;
                }
                buffer[startIndex + i] = _input.Current;
            }
            return length;
        }

        public void Dispose() { }
    }
}