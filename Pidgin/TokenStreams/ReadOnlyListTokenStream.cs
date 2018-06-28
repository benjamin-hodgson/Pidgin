using System.Collections.Generic;

namespace Pidgin.TokenStreams
{
    internal sealed class ReadOnlyListTokenStream<TToken> : InMemoryTokenStream<TToken>
    {
        private readonly IReadOnlyList<TToken> _input;

        public ReadOnlyListTokenStream(IReadOnlyList<TToken> value) : base(value.Count)
        {
            _input = value;
        }

        public override TToken Current => _input[_index];
    }
}
