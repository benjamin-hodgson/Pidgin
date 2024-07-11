using System;

namespace Pidgin;

public static partial class Parser
{
    /// <summary>
    /// Creates a parser that applies the specified parsers sequentially and applies the specified transformation function to their results.
    /// </summary>
    /// <param name="func">A function to apply to the return values of the specified parsers.</param>
    /// <param name="parser1">The first parser.</param>
    /// <typeparam name="TToken">The type of tokens in the parser's input stream.</typeparam>
    /// <typeparam name="T1">The return type of the first parser.</typeparam>
    /// <typeparam name="R">The return type of the resulting parser.</typeparam>
    /// <returns>
    /// A parser that applies the specified parsers sequentially and applies the specified transformation function to their results.
    /// </returns>
    public static Parser<TToken, R> Map<TToken, T1, R>(
        Func<T1, R> func,
        Parser<TToken, T1> parser1
    )
    {
        if (func == null)
        {
            throw new ArgumentNullException(nameof(func));
        }

        if (parser1 == null)
        {
            throw new ArgumentNullException(nameof(parser1));
        }

        return parser1 is IMapParser<TToken, T1> p
            ? p.MapFast(func)
            : parser1.Accept(new Map1ParserFactory<TToken, T1, R>(func));
    }
}

internal interface IMapParser<TToken, T>
{
    Parser<TToken, U> MapFast<U>(Func<T, U> func);
}

internal sealed class Map1ParserFactory<TToken, T1, R>(Func<T1, R> func)
    : IUnboxer<TToken, T1, BoxParser<TToken, R>>
{
    public BoxParser<TToken, R> Unbox<Next>(BoxParser<TToken, T1>.Of<Next> box)
        where Next : IParser<TToken, T1>
        => BoxParser<TToken, R>.Create(new Map1Parser<Next, TToken, T1, R>(func, box));
}

internal sealed class Map1Parser<Next, TToken, T1, R> : Parser<TToken, R>, IMapParser<TToken, R>
    where Next : IParser<TToken, T1>
{
    private readonly Func<T1, R> _func;
    private readonly BoxParser<TToken, T1>.Of<Next> _p1;

    public Map1Parser(
        Func<T1, R> func,
        BoxParser<TToken, T1>.Of<Next> parser1
    )
    {
        _func = func;
        _p1 = parser1;
    }

    public sealed override bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, out R result)
    {
        var success1 = _p1.Value.TryParse(ref state, ref expecteds, out var result1);
        if (!success1)
        {
            result = default!;
            return false;
        }

        result = _func(
            result1!
        );
        return true;
    }

    Parser<TToken, U> IMapParser<TToken, R>.MapFast<U>(Func<R, U> func)
        => new Map1Parser<Next, TToken, T1, U>(
            (x1) => func(_func(x1)),
            _p1
        );
}
