using System.Collections.Generic;
using Pidgin.ParseStates;

namespace Pidgin
{
    public partial class Parser<TToken, T>
    {
        /// <summary>
        /// Creates a parser equivalent to the current parser, with a custom label.
        /// The label will be reported in an error message if the parser fails, instead of the default error message.
        /// <seealso cref="ParseError{TToken}.Expected"/>
        /// <seealso cref="Expected{TToken}.Label"/>
        /// </summary>
        /// <param name="label">The custom label to apply to the current parser</param>
        /// <returns>A parser equivalent to the current parser, with a custom label</returns>
        public Parser<TToken, T> Labelled(string label)
            => WithExpected(new SortedSet<Expected<TToken>> { new Expected<TToken>(label) });
            
        internal Parser<TToken, T> WithExpected(SortedSet<Expected<TToken>> expected)
            => new WithExpectedParser(this, expected);

        private sealed class WithExpectedParser : Parser<TToken, T>
        {
            private readonly Parser<TToken, T> _parser;

            public WithExpectedParser(Parser<TToken, T> parser, SortedSet<Expected<TToken>> expected) : base(expected)
            {
                _parser = parser;
            }

            internal override InternalResult<T> Parse(IParseState<TToken> state)
            {
                var result = _parser.Parse(state);
                if (!result.Success)
                {
                    state.Error = state.Error.WithExpected(Expected);
                }
                return result;
            }
        }
    }
}