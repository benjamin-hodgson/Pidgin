namespace Pidgin
{
    public partial class Parser<TToken, T>
    {
        /// <summary>
        /// Creates a parser which behaves like the current parser but returns <paramref name="result"/> after a successful parse.
        /// This is a synonym for <see cref="ThenReturn"/>.
        /// </summary>
        /// <example>
        /// Equivalent to using <see cref="Select"/> with a function that returns a fixed value,
        /// or <see cref="Then{U}(Parser{TToken, U})"/> with <see cref="Parser{TToken}.Return{T}(T)"/>.
        /// <code>
        /// p.WithResult(x) == p.Select(_ => x) == p.Then(Return(x));
        /// </code>
        /// </example>
        /// <param name="result">The result</param>
        /// <returns>A parser which behaves like the current parser but returns <paramref name="result"/>.</returns>
        public Parser<TToken, U> WithResult<U>(U result)
            => this.Select(_ => result);

        /// <summary>
        /// Creates a parser which behaves like the current parser but returns <paramref name="result"/> after a successful parse.
        /// This is a synonym for <see cref="WithResult"/>.
        /// </summary>
        /// <example>
        /// Equivalent to using <see cref="Select"/> with a function that returns a fixed value,
        /// or <see cref="Then{U}(Parser{TToken, U})"/> with <see cref="Parser{TToken}.Return{T}(T)"/>.
        /// <code>
        /// p.ThenReturn(x) == p.Select(_ => x) == p.Then(Return(x));
        /// </code>
        /// </example>
        /// <param name="result">The result</param>
        /// <returns>A parser which behaves like the current parser but returns <paramref name="result"/>.</returns>
        public Parser<TToken, U> ThenReturn<U>(U result)
            => this.Select(_ => result);

        /// <summary>
        /// Creates a parser which behaves like the current parser but returns <see cref="Unit.Value"/>.
        /// Equivalent to <c>p.WithResult(Unit.Value)</c>.
        /// </summary>
        /// <returns>A parser which behaves like the current parser but returns <see cref="Unit.Value"/>.</returns>
        public Parser<TToken, Unit> IgnoreResult()
            => this.WithResult(Unit.Value);
    }
}