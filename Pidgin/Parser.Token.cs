using System;
using System.Collections.Generic;
using Pidgin.ParseStates;

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

            public TokenParser(TToken token)
                : base(new SortedSet<Expected<TToken>>{ new Expected<TToken>(new[]{token}) })
            {
                _token = token;
            }

            internal sealed override Result<TToken, TToken> Parse(IParseState<TToken> state)
            {
                var x = state.Peek();
                if (!x.HasValue)
                {
                    return Result.Failure<TToken, TToken>(
                        new ParseError<TToken>(
                            x,
                            true,
                            Expected,
                            state.SourcePos,
                            null
                        ),
                        false
                    );
                }
                var val = x.GetValueOrDefault();
                if (!val.Equals(_token))
                {
                    return Result.Failure<TToken, TToken>(
                        new ParseError<TToken>(
                            x,
                            false,
                            Expected,
                            state.SourcePos,
                            null
                        ),
                        false
                    );
                }
                state.Advance();
                return Result.Success<TToken, TToken>(val, true);
            }
        }

        /// <summary>
        /// Creates a parser that parses and returns a single token satisfying a predicate
        /// </summary>
        /// <param name="predicate">A predicate function to apply to a token</param>
        /// <returns>A parser that parses and returns a single token satisfying a predicate</returns>
        public static Parser<TToken, TToken> Token(Func<TToken, bool> predicate)
            => new PredicateTokenParser(predicate);

        private sealed class PredicateTokenParser : Parser<TToken, TToken>
        {
            private readonly Func<TToken, bool> _predicate;

            public PredicateTokenParser(Func<TToken, bool> predicate)
                : base(ExpectedUtil<TToken>.Empty)
            {
                _predicate = predicate;
            }

            internal sealed override Result<TToken, TToken> Parse(IParseState<TToken> state)
            {
                var x = state.Peek();
                if (!x.HasValue)
                {
                    return Result.Failure<TToken, TToken>(
                        new ParseError<TToken>(
                            x,
                            true,
                            Expected,
                            state.SourcePos,
                            null
                        ),
                        false
                    );
                }
                var val = x.GetValueOrDefault();
                if (!_predicate(val))
                {
                    return Result.Failure<TToken, TToken>(
                        new ParseError<TToken>(
                            x,
                            false,
                            Expected,
                            state.SourcePos,
                            null
                        ),
                        false
                    );
                }
                state.Advance();
                return Result.Success<TToken, TToken>(val, true);
            }
        }
    }
}