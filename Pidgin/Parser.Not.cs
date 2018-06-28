using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Pidgin
{
    public static partial class Parser
    {
        /// <summary>
        /// Creates a parser which succeeds only if the given parser fails.
        /// The resulting parser does not perform any backtracking; it consumes the same amount of input as the supplied parser.
        /// Combine this function with <see cref="Parser.Try{TToken, T}(Parser{TToken, T})"/> if this behaviour is undesirable.
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

            public NegatedParser(Parser<TToken, T> parser) : base(ImmutableSortedSet.Create<Expected<TToken>>())
            {
                _parser = parser;
            }

            internal sealed override InternalResult<Unit> Parse(ref ParseState<TToken> state)
            {
                var startingPosition = state.SourcePos;
                var token = state.Peek();
                var result = _parser.Parse(ref state);
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