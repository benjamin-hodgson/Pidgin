using Pidgin.ParseStates;

namespace Pidgin
{
    public static partial class Parser<TToken>
    {
        /// <summary>
        /// Creates a parser which returns the specified value without consuming any input
        /// </summary>
        /// <param name="value">The value to return</param>
        /// <typeparam name="T">The type of the value to return</typeparam>
        /// <returns>A parser which returns the specified value without consuming any input</returns>
        public static Parser<TToken, T> Return<T>(T value)
            => new ReturnParser<T>(value);

        internal static Parser<TToken, Unit> ReturnUnit { get; }
            = Parser<TToken>.Return(Unit.Value);

        private sealed class ReturnParser<T> : Parser<TToken, T>
        {
            private readonly T _value;

            public ReturnParser(T value) : base(ExpectedUtil<TToken>.Nil)
            {
                _value = value;
            }

            internal sealed override Result<TToken, T> Parse(IParseState<TToken> state)
                => Result.Success<TToken, T>(_value, false);
        }
    }
}
