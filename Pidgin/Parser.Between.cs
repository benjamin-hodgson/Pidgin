using System;

namespace Pidgin;

public abstract partial class Parser<TToken, T>
{
    /// <summary>
    /// Creates a parser that applies the specified parser both before and after applying the current parser.
    /// The resulting parser returns the result of the current parser, ignoring the return value of the bracketing parser.
    /// </summary>
    /// <param name="parser">The parser to apply before and after applying the current parser.</param>
    /// <typeparam name="U">The type of the value returned by the bracketing parser.</typeparam>
    /// <returns>A parser that applies the specified parser before and after applying the current parser.</returns>
    public Parser<TToken, T> Between<U>(Parser<TToken, U> parser)
    {
        if (parser == null)
        {
            throw new ArgumentNullException(nameof(parser));
        }

        return Between(parser, parser);
    }

    /// <summary>
    /// Creates a parser that applies the specified parsers before and after applying the current parser.
    /// The resulting parser returns the result of the current parser, ignoring the return values of the bracketing parsers.
    /// </summary>
    /// <param name="parser1">The parser to apply before applying the current parser.</param>
    /// <param name="parser2">The parser to apply after applying the current parser.</param>
    /// <typeparam name="U">The type of the value returned by the first parser.</typeparam>
    /// <typeparam name="V">The type of the value returned by the second parser.</typeparam>
    /// <returns>A parser that applies the specified parsers before and after applying the current parser.</returns>
    public Parser<TToken, T> Between<U, V>(Parser<TToken, U> parser1, Parser<TToken, V> parser2)
    {
        if (parser1 == null)
        {
            throw new ArgumentNullException(nameof(parser1));
        }

        if (parser2 == null)
        {
            throw new ArgumentNullException(nameof(parser2));
        }

        return Parser.Map((u, t, v) => t, parser1, this, parser2);
    }
}
