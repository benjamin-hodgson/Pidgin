using System;

namespace Pidgin
{
    public abstract partial class Parser<TToken, T>
    {
        /// <summary>
        /// Creates a parser which applies the specified transformation function to the result of the current parser.
        /// </summary>
        /// <typeparam name="U">The return type of the transformation function</typeparam>
        /// <param name="selector">A transformation function</param>
        /// <returns>A parser which applies <paramref name="selector"/> to the result of the current parser</returns>
        public Parser<TToken, U> Select<U>(Func<T, U> selector)
            => Parser.Map(selector, this);
    }
}