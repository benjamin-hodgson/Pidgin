using System.Collections.Generic;

namespace Pidgin.TokenStreams
{
    internal sealed class ListTokenStream<TToken> : InMemoryTokenStream<TToken>
    {
        private readonly IList<TToken> _input;

        public ListTokenStream(IList<TToken> value) : base(value.Count)
        {
            _input = value;
        }

        public override TToken Current => _input[_index];
    }
}
