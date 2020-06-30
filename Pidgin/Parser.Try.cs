using System;
using System.Collections.Immutable;

namespace Pidgin
{
    public static partial class Parser
    {
        /// <summary>
        /// Creates a parser which applies <paramref name="parser"/> and backtracks upon failure
        /// </summary>
        /// <typeparam name="TToken">The type of tokens in the parser's input stream</typeparam>
        /// <typeparam name="T">The return type of the parser</typeparam>
        /// <param name="parser">The parser</param>
        /// <returns>A parser which applies <paramref name="parser"/> and backtracks upon failure</returns>
        public static Parser<TToken, T> Try<TToken, T>(Parser<TToken, T> parser)
        {
            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }
            return new TryParser<TToken, T>(parser);
        }
    }

    internal sealed class TryParser<TToken, T> : Parser<TToken, T>
    {
        private readonly Parser<TToken, T> _parser;

        public TryParser(Parser<TToken, T> parser)
        {
            _parser = parser;
        }

        internal sealed override InternalResult<T> Parse(ref ParseState<TToken> state, ref ExpectedCollector<TToken> expecteds)
        {
            // start buffering the input
            state.PushBookmark();
            var result = _parser.Parse(ref state, ref expecteds);
            if (!result.Success)
            {
                // return to the start of the buffer and discard the bookmark
                state.Rewind();
                return InternalResult.Failure<T>();
            }

            // discard the buffer
            state.PopBookmark();
            return result;
        }
    }
}
