using System;
using System.Collections.Generic;

namespace Pidgin
{
    public partial class Parser<TToken, T>
    {
        /// <summary>
        /// Creates a parser which runs the current parser, running <paramref name="errorHandler" /> on failure.
        /// </summary>
        /// <param name="errorHandler">A function which returns a parser to apply when the current parser fails.</param>
        /// <returns>A parser which runs the current parser, running <paramref name="errorHandler" /> on failure.</returns>
        public Parser<TToken, T> RecoverWith(Func<ParseError<TToken>, Parser<TToken, T>> errorHandler)
        {
            if (errorHandler == null)
            {
                throw new ArgumentNullException(nameof(errorHandler));
            }
            return new RecoverWithParser(this, errorHandler);
        }

        private sealed class RecoverWithParser : Parser<TToken, T>
        {
            private readonly Parser<TToken, T> _parser;
            private readonly Func<ParseError<TToken>, Parser<TToken, T>> _errorHandler;

            public RecoverWithParser(Parser<TToken, T> parser, Func<ParseError<TToken>, Parser<TToken, T>> errorHandler) : base(parser.Expected)
            {
                _parser = parser;
                _errorHandler = errorHandler;
            }

            internal override InternalResult<T> Parse(ref ParseState<TToken> state)
            {
                var result = _parser.Parse(ref state);
                if (result.Success)
                {
                    return result;
                }
                var recoverParser = _errorHandler(state.Error);
                return recoverParser.Parse(ref state);
            }
        }
    }
}