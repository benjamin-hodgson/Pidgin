using System;
using System.Linq.Expressions;

namespace Pidgin
{
    public abstract partial class Parser<TToken, T>
    {
        /// <summary>
        /// Creates a parser that applies a transformation function to the return value of the current parser.
        /// The transformation function dynamically chooses a second parser, which is applied after applying the current parser.
        /// </summary>
        /// <param name="selector">A transformation function which returns a parser to apply after applying the current parser</param>
        /// <param name="result">A function to apply to the return values of the two parsers</param>
        /// <typeparam name="U">The type of the return value of the second parser</typeparam>
        /// <typeparam name="R">The type of the return value of the resulting parser</typeparam>
        /// <returns>A parser which applies the current parser before applying the result of the <paramref name="selector"/> function</returns>
        public Parser<TToken, R> SelectMany<U, R>(Expression<Func<T, Parser<TToken, U>>> selector, Expression<Func<T, U, R>> result)
        {
            // TODO: do a free variable analysis on the body of the lambda
            // to determine whether it's a constant function or not. (often it will be.)
            // If it is, sequence the body of the lambda instead.

            // TODO #2: figure out how expensive that analysis is. Too expensive to do inside the body of an earlier Bind?
            
            // TODO #3: if R is an anonymous type (eg we're in a query expr) can we rewrite it to a value type? No, right?
            // https://github.com/dotnet/roslyn/issues/8192
            return Bind(selector.Compile(), result.Compile());
        }
    }
}