using System;

namespace Pidgin;

public abstract partial class Parser<TToken, T>
{
    /// <summary>
    /// Creates a parser that applies a transformation function to the return value of the current parser.
    /// The transformation function dynamically chooses a second parser, which is applied after applying the current parser.
    /// </summary>
    /// <param name="selector">A transformation function which returns a parser to apply after applying the current parser.</param>
    /// <param name="result">A function to apply to the return values of the two parsers.</param>
    /// <typeparam name="U">The type of the return value of the second parser.</typeparam>
    /// <typeparam name="R">The type of the return value of the resulting parser.</typeparam>
    /// <returns>A parser which applies the current parser before applying the result of the <paramref name="selector"/> function.</returns>
    public Parser<TToken, R> SelectMany<U, R>(Func<T, Parser<TToken, U>> selector, Func<T, U, R> result)
    {
        if (selector == null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return Bind(selector, result);
    }
}
