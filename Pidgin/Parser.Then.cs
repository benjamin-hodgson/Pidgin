using System;

namespace Pidgin;

public abstract partial class Parser<TToken, T>
{
    /// <summary>
    /// Creates a parser which applies the current parser followed by a specified parser.
    /// The resulting parser returns the result of the second parser, ignoring the result of the current parser.
    /// </summary>
    /// <typeparam name="U">The return type of the second parser.</typeparam>
    /// <param name="parser">A parser to apply after applying the current parser.</param>
    /// <returns>A parser which applies the current parser followed by <paramref name="parser"/>.</returns>
    public Parser<TToken, U> Then<U>(Parser<TToken, U> parser)
    {
        if (parser == null)
        {
            throw new ArgumentNullException(nameof(parser));
        }

        return Then(parser, (t, u) => u);
    }

    /// <summary>
    /// Creates a parser which applies the current parser followed by a specified parser, applying a function to the two results.
    /// </summary>
    /// <remarks>
    /// This is a synonym for <see cref="Parser.Map{TToken, T1, T2, R}(Func{T1, T2, R}, Parser{TToken, T1}, Parser{TToken, T2})"/>
    /// with the arguments rearranged.
    /// </remarks>
    /// <typeparam name="U">The return type of the second parser.</typeparam>
    /// <typeparam name="R">The return type of the composed parser.</typeparam>
    /// <param name="parser">A parser to apply after applying the current parser.</param>
    /// <param name="result">A function to apply to the two parsed values.</param>
    /// <returns>A parser which applies the current parser followed by <paramref name="parser"/>.</returns>
    public Parser<TToken, R> Then<U, R>(Parser<TToken, U> parser, Func<T, U, R> result)
    {
        if (parser == null)
        {
            throw new ArgumentNullException(nameof(parser));
        }

        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return Parser.Map(result, this, parser);
    }

    /// <summary>
    /// Creates a parser that applies a transformation function to the return value of the current parser.
    /// The transformation function dynamically chooses a second parser, which is applied after applying the current parser.
    /// </summary>
    /// <remarks>This function is a synonym for <see cref="Parser{TToken, T}.Bind{U}(Func{T, Parser{TToken, U}})"/>.</remarks>
    /// <param name="selector">A transformation function which returns a parser to apply after applying the current parser.</param>
    /// <typeparam name="U">The type of the return value of the second parser.</typeparam>
    /// <returns>A parser which applies the current parser before applying the result of the <paramref name="selector"/> function.</returns>
    public Parser<TToken, U> Then<U>(Func<T, Parser<TToken, U>> selector)
    {
        if (selector == null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        return Bind(selector);
    }

    /// <summary>
    /// Creates a parser that applies a transformation function to the return value of the current parser.
    /// The transformation function dynamically chooses a second parser, which is applied after applying the current parser.
    /// </summary>
    /// <remarks>This function is a synonym for <see cref="Parser{TToken, T}.Bind{U, R}(Func{T, Parser{TToken, U}}, Func{T, U, R})"/>.</remarks>
    /// <param name="selector">A transformation function which returns a parser to apply after applying the current parser.</param>
    /// <param name="result">A function to apply to the return values of the two parsers.</param>
    /// <typeparam name="U">The type of the return value of the second parser.</typeparam>
    /// <typeparam name="R">The type of the return value of the resulting parser.</typeparam>
    /// <returns>A parser which applies the current parser before applying the result of the <paramref name="selector"/> function.</returns>
    public Parser<TToken, R> Then<U, R>(Func<T, Parser<TToken, U>> selector, Func<T, U, R> result)
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
