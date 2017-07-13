using System.Collections.Generic;
using System.Linq;
using Pidgin.ParseStates;

namespace Pidgin
{
    public static partial class Parser
    {
        /// <summary>
        /// Creates a parser which attempts to apply the specified parser and only succeeds if that parser fails.
        /// </summary>
        /// <param name="parser">The parser to apply if the current parser fails without consuming any input</param>
        /// <returns>A parser which tries to apply the current parser, and applies <paramref name="parser"/> if the current parser fails without consuming any input.</returns>
        public static Parser<TToken, TToken> Not<TToken>(Parser<TToken, TToken> parser) {
            return new NegatedParser<TToken>(parser);
        }
        
        private sealed class NegatedParser<TToken> : Parser<TToken, TToken>
        {
            private readonly Parser<TToken, TToken> _parser;

            public NegatedParser(Parser<TToken, TToken> parser) : base(parser.Expected)
            {
                _parser = parser;
            }

            internal sealed override Result<TToken, TToken> Parse(IParseState<TToken> state)
            {
                var startingPosition = state.SourcePos;
                var result = _parser.Parse(state);
                if (result.Success)
                {
                    return Result.Failure<TToken, TToken>(
                            new ParseError<TToken>(
                                new Maybe<TToken>(result.Value),
                                false,
                                null,
                                startingPosition,
                                "unexpected token"
                            ),
                            true
                        );
                }

                var token = state.Peek();
                if (token.HasValue) {
                    state.Advance();
                    return Result.Success<TToken, TToken>(
                            token.Value,
                            true
                        );
                }
                
                return Result.Failure<TToken, TToken>(new ParseError<TToken>(), false);
            }
        }
    }
}