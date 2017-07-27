using System;
using System.Collections.Generic;
using System.Linq;
using Pidgin.ParseStates;

namespace Pidgin
{
    public static partial class Parser
    {
        /// <summary>
        /// Creates a parser which succeeds only if the given parser fails.
        /// The resulting parser does not perform any backtracking; it consumes the same amount of input as the supplied parser.
        /// </summary>
        /// <param name="parser">The parser that is expected to fail</param>
        /// <returns>A parser which succeeds only if the given parser fails.</returns>
        public static Parser<TToken, Unit> Not<TToken, T>(Parser<TToken, T> parser)
        {
            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }
            return new NegatedParser<TToken, T>(parser);            
        }
        
        private sealed class NegatedParser<TToken, T> : Parser<TToken, Unit>
        {
            private readonly Parser<TToken, T> _parser;

            public NegatedParser(Parser<TToken, T> parser) : base(ExpectedUtil<TToken>.Empty)
            {
                _parser = parser;
            }

            internal sealed override InternalResult<Unit> Parse(IParseState<TToken> state)
            {
                var startingPosition = state.SourcePos;
                var token = state.Peek();
                var result = _parser.Parse(state);
                if (result.Success)
                {
                    state.Error = new ParseError<TToken>(
                        token,
                        false,
                        null,
                        startingPosition,
                        null
                    );
                    return InternalResult.Failure<Unit>(result.ConsumedInput);
                }

                return InternalResult.Success(
                    Unit.Value,
                    result.ConsumedInput
                );
            }
        }
    }
}