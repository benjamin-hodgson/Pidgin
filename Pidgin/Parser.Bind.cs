using System;
using System.Diagnostics.CodeAnalysis;

namespace Pidgin;

public abstract partial class Parser<TToken, T>
{
    /// <summary>
    /// Creates a parser that applies a transformation function to the return value of the current parser.
    /// The transformation function dynamically chooses a second parser, which is applied after applying the current parser.
    /// </summary>
    /// <param name="selector">A transformation function which returns a parser to apply after applying the current parser.</param>
    /// <typeparam name="U">The type of the return value of the second parser.</typeparam>
    /// <returns>A parser which applies the current parser before applying the result of the <paramref name="selector"/> function.</returns>
    public Parser<TToken, U> Bind<U>(Func<T, Parser<TToken, U>> selector)
    {
        if (selector == null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        return Bind(selector, (t, u) => u);
    }

    /// <summary>
    /// Creates a parser that applies a transformation function to the return value of the current parser.
    /// The transformation function dynamically chooses a second parser, which is applied after applying the current parser.
    /// </summary>
    /// <param name="selector">A transformation function which returns a parser to apply after applying the current parser.</param>
    /// <param name="result">A function to apply to the return values of the two parsers.</param>
    /// <typeparam name="U">The type of the return value of the second parser.</typeparam>
    /// <typeparam name="R">The type of the return value of the resulting parser.</typeparam>
    /// <returns>A parser which applies the current parser before applying the result of the <paramref name="selector"/> function.</returns>
    public Parser<TToken, R> Bind<U, R>(Func<T, Parser<TToken, U>> selector, Func<T, U, R> result)
    {
        if (selector == null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return Accept(new BindParserFactory<TToken, T, U, R>(selector, result));
    }
}

internal class BindParserFactory<TToken, T, U, R>(Func<T, Parser<TToken, U>> func, Func<T, U, R> result)
    : IUnboxer<TToken, T, BoxParser<TToken, R>>
{
    public BoxParser<TToken, R> Unbox<Next>(BoxParser<TToken, T>.Of<Next> box)
        where Next : IParser<TToken, T>
        => BoxParser<TToken, R>.Create(new BindParser<Next, TToken, T, U, R>(box, func, result));
}

internal readonly struct BindParser<Next, TToken, T, U, R> : IParser<TToken, R>
    where Next : IParser<TToken, T>
{
    private readonly BoxParser<TToken, T>.Of<Next> _parser;
    private readonly Func<T, Parser<TToken, U>> _func;
    private readonly Func<T, U, R> _result;

    public BindParser(BoxParser<TToken, T>.Of<Next> parser, Func<T, Parser<TToken, U>> func, Func<T, U, R> result)
    {
        _parser = parser;
        _func = func;
        _result = result;
    }

    public bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out R result)
    {
        var success = _parser.Value.TryParse(ref state, ref expecteds, out var childResult);
        if (!success)
        {
            // state.Error set by _parser
            result = default;
            return false;
        }

        var nextParser = _func(childResult!);
        var nextSuccess = nextParser.TryParse(ref state, ref expecteds, out var nextResult);

        if (!nextSuccess)
        {
            // state.Error set by nextParser
            result = default;
            return false;
        }

        result = _result(childResult!, nextResult!);
        return true;
    }
}
