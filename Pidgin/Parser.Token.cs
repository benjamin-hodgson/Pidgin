using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Pidgin
{
    public static partial class Parser<TToken>
    {
        /// <summary>
        /// Creates a parser that parses and returns a single token
        /// </summary>
        /// <param name="token">The token to parse</param>
        /// <returns>A parser that parses and returns a single token</returns>
        public static Parser<TToken, TToken> Token(TToken token)
            // equivalent to Token(token.Equals) but with better error messages
            => new TokenParser(token);

        private sealed class TokenParser : Parser<TToken, TToken>
        {
            private readonly TToken _token;
            private Expected<TToken> _expected;
            private Expected<TToken> Expected
            {
                get
                {
                    if (_expected.InternalTokens.IsDefault)
                    {
                        _expected = new Expected<TToken>(ImmutableArray.Create(_token));
                    }
                    return _expected;
                }
            }

            public TokenParser(TToken token)
            {
                _token = token;
            }

            internal sealed override InternalResult<TToken> Parse(ref ParseState<TToken> state)
            {
                if (!state.HasCurrent)
                {
                    state.Error = new InternalError<TToken>(
                        Maybe.Nothing<TToken>(),
                        true,
                        state.Location,
                        null
                    );
                    state.AddExpected(Expected);
                    return InternalResult.Failure<TToken>(false);
                }
                var token = state.Current;
                if (!EqualityComparer<TToken>.Default.Equals(token, _token))
                {
                    state.Error = new InternalError<TToken>(
                        Maybe.Just(token),
                        false,
                        state.Location,
                        null
                    );
                    state.AddExpected(Expected);
                    return InternalResult.Failure<TToken>(false);
                }
                state.Advance();
                return InternalResult.Success<TToken>(token, true);
            }
        }

        /// <summary>
        /// Creates a parser that parses and returns a single token satisfying a predicate
        /// </summary>
        /// <param name="predicate">A predicate function to apply to a token</param>
        /// <returns>A parser that parses and returns a single token satisfying a predicate</returns>
        public static Parser<TToken, TToken> Token(Func<TToken, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            return new PredicateTokenParser(predicate);
        }

        private sealed class PredicateTokenParser : Parser<TToken, TToken>
        {
            private readonly Func<TToken, bool> _predicate;

            public PredicateTokenParser(Func<TToken, bool> predicate)
            {
                _predicate = predicate;
            }

            internal sealed override InternalResult<TToken> Parse(ref ParseState<TToken> state)
            {
                if (!state.HasCurrent)
                {
                    state.Error = new InternalError<TToken>(
                        Maybe.Nothing<TToken>(),
                        true,
                        state.Location,
                        null
                    );
                    return InternalResult.Failure<TToken>(false);
                }
                var token = state.Current;
                if (!_predicate(token))
                {
                    state.Error = new InternalError<TToken>(
                        Maybe.Just(token),
                        false,
                        state.Location,
                        null
                    );
                    return InternalResult.Failure<TToken>(false);
                }
                state.Advance();
                return InternalResult.Success<TToken>(token, true);
            }
        }
    }
}