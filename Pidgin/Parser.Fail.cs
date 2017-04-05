using Pidgin.ParseStates;

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
            => new FailParser<T>(message);
        
        private sealed class FailParser<T> : Parser<TToken, T>
        {
            private readonly string _message;

            public FailParser(string message) : base(ExpectedUtil<TToken>.Nil)
            {
                _message = message;
            }

            internal sealed override Result<TToken, T> Parse(IParseState<TToken> state)
                => Result.Failure<TToken, T>(
                    new ParseError<TToken>(
                        Maybe.Nothing<TToken>(),
                        false,
                        Expected,
                        state.SourcePos,
                        _message
                    ),
                    false
                );
        }
    }
}