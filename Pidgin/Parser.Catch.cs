using System;
using System.Diagnostics.CodeAnalysis;

namespace Pidgin;

public abstract partial class Parser<TToken, T>
{
    /// <summary>
    /// Creates a parser which runs the current parser, running <paramref name="errorHandler"/> if it throws <typeparamref name="TException"/>.
    /// </summary>
    /// <typeparam name="TException">The exception to catch.</typeparam>
    /// <param name="errorHandler">A function which returns a parser to apply when the current parser throws <typeparamref name="TException"/>.</param>
    /// <returns>A parser twhich runs the current parser, running <paramref name="errorHandler"/> if it throws <typeparamref name="TException"/>.</returns>
    public Parser<TToken, T> Catch<TException>(Func<TException, Parser<TToken, T>> errorHandler)
        where TException : Exception
    {
        return Catch((TException e, int _) => errorHandler(e));
    }

    /// <summary>
    /// Creates a parser which runs the current parser, calling <paramref name="errorHandler"/> with the number of inputs consumed
    /// by the current parser until failure if it throws <typeparamref name="TException"/>.
    /// </summary>
    /// <typeparam name="TException">The exception to catch.</typeparam>
    /// <param name="errorHandler">A function which returns a parser to apply when the current parser throws <typeparamref name="TException"/>.</param>
    /// <returns>A parser twhich runs the current parser, running <paramref name="errorHandler"/> if it throws <typeparamref name="TException"/>.</returns>
    public Parser<TToken, T> Catch<TException>(Func<TException, int, Parser<TToken, T>> errorHandler)
        where TException : Exception
    {
        return new CatchParser<TToken, T, TException>(this, errorHandler);
    }
}

internal sealed class CatchParser<TToken, T, TException> : Parser<TToken, T>
    where TException : Exception
{
    private readonly Parser<TToken, T> _parser;

    private readonly Func<TException, int, Parser<TToken, T>> _errorHandler;

    public CatchParser(Parser<TToken, T> parser, Func<TException, int, Parser<TToken, T>> errorHandler)
    {
        _errorHandler = errorHandler;
        _parser = parser;
    }

    public override bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out T result)
    {
        var bookmark = state.Bookmark();
        try
        {
            var success = _parser.TryParse(ref state, ref expecteds, out result);
            state.DiscardBookmark(bookmark);

            return success;
        }
        catch (TException e)
        {
            var count = state.Location - bookmark;
            state.Rewind(bookmark);

            return _errorHandler(e, count).TryParse(ref state, ref expecteds, out result);
        }
    }
}
