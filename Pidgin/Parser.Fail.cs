using System;
using System.Collections.Immutable;

namespace Pidgin
{
    public static partial class Parser<TToken>
    {
        /// <summary>
        /// Creates a parser which always fails without consuming any input.
        /// </summary>
        /// <param name="message">A custom error message</param>
        /// <typeparam name="T">The return type of the resulting parser</typeparam>
        /// <returns>A parser which always fails</returns>
        public static Parser<TToken, T> Fail<T>(string message = "Failed")
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            return new FailParser<TToken, T>(message);
        }
    }

    internal sealed class FailParser<TToken, T> : Parser<TToken, T>
    {
        private static readonly Expected<TToken> _expected
            = new Expected<TToken>(ImmutableArray<TToken>.Empty);
        private readonly string _message;

        public FailParser(string message)
        {
            _message = message;
        }

        public sealed override InternalResult<T> Parse(ref ParseState<TToken> state)
        {
            state.Error = new InternalError<TToken>(
                Maybe.Nothing<TToken>(),
                false,
                state.Location,
                _message
            );
            state.AddExpected(_expected);
            return InternalResult.Failure<T>(false);
        }
    }
}
