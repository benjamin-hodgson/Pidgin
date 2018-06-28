using System;
using System.Collections.Generic;

namespace Pidgin.TokenStreams
{
    internal class EnumeratorTokenStream<TToken> : BufferedTokenStream<TToken>
    {
        private readonly IEnumerator<TToken> _input;

        public EnumeratorTokenStream(IEnumerator<TToken> input) : base(16)
        {
            _input = input;
        }

        protected override int Read()
        {
            var hasNext = _input.MoveNext();
            if (hasNext)
            {
                _buffer[_index] = _input.Current;
                return 1;
            }
            return 0;
        }
    }
}