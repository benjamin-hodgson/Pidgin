using System;
using System.Collections.Generic;

namespace Pidgin.TokenStreams
{
    internal sealed class ReadOnlyListTokenStream<TToken> : ITokenStream<TToken>
    {
        public int ChunkSizeHint => 16;

        private readonly IReadOnlyList<TToken> _input;
        private int _index = 0;


        public ReadOnlyListTokenStream(IReadOnlyList<TToken> value)
        {
            _input = value;
        }

        public int ReadInto(Span<TToken> buffer)
        {
            var actualLength = Math.Min(_input.Count - _index, buffer.Length);
            for (var i = 0; i < actualLength; i++)
            {
                buffer[i] = _input[_index];
                _index++;
            }
            return actualLength;
        }

        public void Dispose() { }
    }
}
