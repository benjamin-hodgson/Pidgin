namespace Pidgin
{
    public abstract partial class Parser<TToken, T>
    {
        private static readonly Parser<TToken, Maybe<T>> _returnNothing
            = Parser<TToken>.Return(Maybe.Nothing<T>());
        /// <summary>
        /// Creates a parser which applies the current parser and returns <see cref="Maybe.Nothing{T}()"/> if the current parser fails without consuming any input.
        /// The resulting parser fails if the current parser fails after consuming input.
        /// </summary>
        /// <returns>A parser which applies the current parser and returns <see cref="Maybe.Nothing{T}()"/> if the current parser fails without consuming any input</returns>
        public Parser<TToken, Maybe<T>> Optional()
            => this.Select(Maybe.Just).Or(_returnNothing);
    }
}