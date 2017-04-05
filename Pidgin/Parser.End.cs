using System.Collections.Generic;
using Pidgin.ParseStates;

namespace Pidgin
{
    public static partial class Parser<TToken>
    {
        /// <summary>
        /// Creates a parser which parses the end of the input stream
        /// </summary>
        /// <returns>A parser which parses the end of the input stream and returns <see cref="Unit.Value"/></returns>
        public static Parser<TToken, Unit> End() => new EndParser();

        private sealed class EndParser : Parser<TToken, Unit>
        {
            private static readonly SortedSet<Expected<TToken>> _expected
                = new SortedSet<Expected<TToken>> { new Expected<TToken>() };

            public EndParser() : base(_expected)
            {
            }

            internal sealed override Result<TToken, Unit> Parse(IParseState<TToken> state)
            {
                var result = state.Peek();
                if (result.HasValue)
                {
                    return Result.Failure<TToken, Unit>(
                        new ParseError<TToken>(
                            result,
                            false,
                            Expected,
                            state.SourcePos,
                            null
                        ),
                        false
                    );
                }
                return Result.Success<TToken, Unit>(Unit.Value, false);
            }
        }
    }
}