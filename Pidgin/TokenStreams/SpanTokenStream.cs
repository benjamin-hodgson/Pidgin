using System;

namespace Pidgin.TokenStreams
{
    internal class SpanTokenStream<TToken> : ITokenStream<TToken>
    {
        public int ChunkSizeHint => 4096;

        private ReadOnlySpanReference<TToken> _input;
        protected int _index = 0;

        public SpanTokenStream(ref ReadOnlySpan<TToken> value)
        {
            _input = new ReadOnlySpanReference<TToken>(ref value);
        }

        public int ReadInto(TToken[] buffer, int startIndex, int length)
        {
            var span = _input.Get();
            var actualLength = Math.Min(span.Length - _index, length);
            span
                .Slice(_index, actualLength)
                .CopyTo(buffer.AsSpan().Slice(startIndex));
            _index += actualLength;
            return actualLength;
        }

        public void Dispose()
        {
            _input = default;
        }
    }
}
