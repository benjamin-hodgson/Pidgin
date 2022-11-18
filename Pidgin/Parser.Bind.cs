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

        return new BindParser<TToken, T, U, R>(this, selector, result);
    }
}

[SuppressMessage(
    "StyleCop.CSharp.MaintainabilityRules",
    "SA1402:FileMayOnlyContainASingleType",
    Justification = "This class belongs next to the accompanying API method"
)]
internal sealed class BindParser<TToken, T, U, R> : Parser<TToken, R>
{
    private readonly Parser<TToken, T> _parser;
    private readonly Func<T, Parser<TToken, U>> _func;
    private readonly Func<T, U, R> _result;

    public BindParser(Parser<TToken, T> parser, Func<T, Parser<TToken, U>> func, Func<T, U, R> result)
    {
        _parser = parser;
        _func = func;
        _result = result;
    }

    public sealed override bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out R result)
    {
        var success = _parser.TryParse(ref state, ref expecteds, out var childResult);
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
