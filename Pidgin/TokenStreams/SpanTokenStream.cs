using System;

namespace Pidgin.TokenStreams
{
    internal class SpanTokenStream<TToken> : InMemoryTokenStream<TToken>
    {
        private ReadOnlySpanReference<TToken> _input;

        public SpanTokenStream(ref ReadOnlySpan<TToken> value) : base(value.Length)
        {
            _input = new ReadOnlySpanReference<TToken>(ref value);
        }

        public override TToken Current => _input.Get()[_index];

        public override void Dispose()
        {
            _input = default;
        }
    }
}
