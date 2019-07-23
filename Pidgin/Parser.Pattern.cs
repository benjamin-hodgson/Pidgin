using System;

namespace Pidgin
{
    public static partial class Parser
    {
        ///
        public static Parser<TToken, T> Pattern<TToken, T>(Pattern<TToken> pattern, ReadOnlySpanFunc<TToken, T> resultSelector)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }
            if (resultSelector == null)
            {
                throw new ArgumentNullException(nameof(resultSelector));
            }
            return new PatternParser<TToken, T>(pattern, resultSelector);
        }

        // todo: public static Parser<char, T> Pattern<T>(string pattern, ReadOnlySpanFunc<char, T> resultSelector)

        private sealed class PatternParser<TToken, T> : Parser<TToken, T>
        {
            private readonly Pattern<TToken> _pattern;
            private readonly ReadOnlySpanFunc<TToken, T> _resultSelector;

            public PatternParser(Pattern<TToken> pattern, ReadOnlySpanFunc<TToken, T> resultSelector)
            {
                _pattern = pattern;
                _resultSelector = resultSelector;
            }

            internal override InternalResult<T> Parse(ref ParseState<TToken> state)
            {
                state.PushBookmark();

                var (success, consumed) = _pattern.Match(ref state);

                if (success)
                {
                    state.Rewind();
                    var span = state.Peek(consumed);
                    var result = _resultSelector(span);
                    state.Advance(consumed);
                    return InternalResult.Success(result, consumed > 0);
                }
                state.PopBookmark();
                return InternalResult.Failure<T>(consumed > 0);
            }
        }
    }
}