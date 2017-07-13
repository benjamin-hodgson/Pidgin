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
        public static Parser<TToken, Unit> Not<TToken, T>(Parser<TToken, T> parser) {
            return new NegatedParser<TToken, T>(parser);
        }
        
        private sealed class NegatedParser<TToken, T> : Parser<TToken, Unit>
        {
            private readonly Parser<TToken, T> _parser;

            public NegatedParser(Parser<TToken, T> parser) : base(ExpectedUtil<TToken>.Empty)
            {
                _parser = parser;
            }

            internal sealed override Result<TToken, Unit> Parse(IParseState<TToken> state)
            {
                var startingPosition = state.SourcePos;
                var token = state.Peek();
                var result = _parser.Parse(state);
                if (result.Success)
                {
                    return Result.Failure<TToken, Unit>(
                            new ParseError<TToken>(
                                token,
                                false,
                                null,
                                startingPosition,
                                "unexpected token"
                            ),
                            true
                        );
                }

                return Result.Success<TToken, Unit>(
                        Unit.Value,
                        result.ConsumedInput
                    );
            }
        }
    }
}