using System;
using System.Collections.Generic;
using Pidgin.ParseStates;

namespace Pidgin
{
    public abstract partial class Parser<TToken, T>
    {
        /// <summary>
        /// Creates a parser that fails if the value returned by the current parser fails to satisfy a predicate.
        /// </summary>
        /// <param name="predicate">The predicate to apply to the value returned by the current parser</param>
        /// <returns>A parser that fails if the value returned by the current parser fails to satisfy <paramref name="predicate"/></returns>
        public Parser<TToken, T> Assert(Func<T, bool> predicate)
            => Assert(predicate, "Assertion failed");
        /// <summary>
        /// Creates a parser that fails if the value returned by the current parser fails to satisfy a predicate.
        /// </summary>
        /// <param name="predicate">The predicate to apply to the value returned by the current parser</param>
        /// <param name="message">A custom error message to return when the value returned by the current parser fails to satisfy the predicate</param>
        /// <returns>A parser that fails if the value returned by the current parser fails to satisfy <paramref name="predicate"/></returns>
        public Parser<TToken, T> Assert(Func<T, bool> predicate, string message)
            => Assert(predicate, _ => message);

        /// <summary>
        /// Creates a parser that fails if the value returned by the current parser fails to satisfy a predicate.
        /// </summary>
        /// <param name="predicate">The predicate to apply to the value returned by the current parser</param>
        /// <param name="message">A function to produce a custom error message to return when the value returned by the current parser fails to satisfy the predicate</param>
        /// <returns>A parser that fails if the value returned by the current parser fails to satisfy <paramref name="predicate"/></returns>
        public Parser<TToken, T> Assert(Func<T, bool> predicate, Func<T, string> message)
            => new AssertParser(this, predicate, message);

        private sealed class AssertParser : Parser<TToken, T>
        {
            private readonly Parser<TToken, T> _parser;
            private readonly Func<T, bool> _predicate;
            private readonly Func<T, string> _message;

            public AssertParser(Parser<TToken, T> parser, Func<T, bool> predicate, Func<T, string> message) : base(new SortedSet<Expected<TToken>> { new Expected<TToken>("result satisfying assertion") })
            {
                _parser = parser;
                _predicate = predicate;
                _message = message;
            }

            internal sealed override InternalResult<T> Parse(IParseState<TToken> state)
            {
                var result = _parser.Parse(state);
                if (!result.Success)
                {
                    return result;
                }
                var val = result.Value;
                if (!_predicate(val))
                {
                    state.Error = new ParseError<TToken>(
                        Maybe.Nothing<TToken>(),
                        false,
                        Expected,
                        state.SourcePos,
                        _message(val)
                    );
                    return InternalResult.Failure<T>(result.ConsumedInput);
                }
                return result;
            }
        }
    }
}