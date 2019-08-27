using System;

namespace Pidgin
{
    public abstract partial class Parser<TToken, T>
    {
        public Parser<TToken, U> MapWithInput<U>(ReadOnlySpanFunc<TToken, T, U> selector)
        {
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }
            return new MapWithInputParser<U>(this, selector);
        }

        private class MapWithInputParser<U> : Parser<TToken, U>
        {
            private Parser<TToken, T> _parser;
            private ReadOnlySpanFunc<TToken, T, U> _selector;

            public MapWithInputParser(Parser<TToken, T> parser, ReadOnlySpanFunc<TToken, T, U> selector)
            {
                _parser = parser;
                _selector = selector;
            }

            internal override InternalResult<U> Parse(ref ParseState<TToken> state)
            {
                var start = state.Location;

                state.PushBookmark();  // don't discard input buffer
                var result = _parser.Parse(ref state);


                if (!result.Success)
                {
                    state.PopBookmark();
                    return InternalResult.Failure<U>(result.ConsumedInput);
                }


                var delta = state.Location - start;
                var val = _selector(state.LookBehind(delta), result.Value);

                state.PopBookmark();

                return InternalResult.Success<U>(val, result.ConsumedInput);
            }
        }
    }
}