using System;
using System.Diagnostics.CodeAnalysis;

namespace Pidgin;

public partial class Parser<TToken, T>
{
    /// <summary>
    /// Creates a parser which runs the current parser, running <paramref name="errorHandler" /> on failure.
    /// </summary>
    /// <param name="errorHandler">A function which returns a parser to apply when the current parser fails.</param>
    /// <returns>A parser which runs the current parser, running <paramref name="errorHandler" /> on failure.</returns>
    public Parser<TToken, T> RecoverWith(Func<ParseError<TToken>, Parser<TToken, T>> errorHandler)
    {
        if (errorHandler == null)
        {
            throw new ArgumentNullException(nameof(errorHandler));
        }

        return Accept(new RecoverWithParserFactory<TToken, T>(errorHandler));
    }
}

internal class RecoverWithParserFactory<TToken, T>(Func<ParseError<TToken>, Parser<TToken, T>> errorHandler)
    : IUnboxer<TToken, T, BoxParser<TToken, T>>
{
    public BoxParser<TToken, T> Unbox<Next>(BoxParser<TToken, T>.Of<Next> box)
        where Next : IParser<TToken, T>
        => BoxParser<TToken, T>.Create(new RecoverWithParser<Next, TToken, T>(box, errorHandler));
}

internal readonly struct RecoverWithParser<Next, TToken, T> : IParser<TToken, T>
    where Next : IParser<TToken, T>
{
    private readonly BoxParser<TToken, T>.Of<Next> _parser;
    private readonly Func<ParseError<TToken>, Parser<TToken, T>> _errorHandler;

    public RecoverWithParser(BoxParser<TToken, T>.Of<Next> parser, Func<ParseError<TToken>, Parser<TToken, T>> errorHandler)
    {
        _parser = parser;
        _errorHandler = errorHandler;
    }

    // see comment about expecteds in ParseState.Error.cs
    public bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out T result)
    {
        var childExpecteds = new PooledList<Expected<TToken>>(state.Configuration.ArrayPoolProvider.GetArrayPool<Expected<TToken>>());
        if (_parser.Value.TryParse(ref state, ref childExpecteds, out result))
        {
            childExpecteds.Dispose();
            return true;
        }

        var recoverParser = _errorHandler(state.BuildError(ref childExpecteds));

        childExpecteds.Dispose();

        return recoverParser.TryParse(ref state, ref expecteds, out result);
    }
}
