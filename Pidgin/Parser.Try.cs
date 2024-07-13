using System;
using System.Diagnostics.CodeAnalysis;

namespace Pidgin;

public static partial class Parser
{
    /// <summary>
    /// Creates a parser which applies <paramref name="parser"/> and backtracks upon failure.
    /// </summary>
    /// <typeparam name="TToken">The type of tokens in the parser's input stream.</typeparam>
    /// <typeparam name="T">The return type of the parser.</typeparam>
    /// <param name="parser">The parser.</param>
    /// <returns>A parser which applies <paramref name="parser"/> and backtracks upon failure.</returns>
    public static Parser<TToken, T> Try<TToken, T>(Parser<TToken, T> parser)
    {
        if (parser == null)
        {
            throw new ArgumentNullException(nameof(parser));
        }

        return TryParserFactory<TToken, T>.Instance.Unbox(parser);
    }
}

internal class TryParserFactory<TToken, T> : IUnboxer<TToken, T, BoxParser<TToken, T>>
{
    private TryParserFactory()
    {
    }

    public BoxParser<TToken, T> Unbox<Next>(BoxParser<TToken, T>.Of<Next> box)
        where Next : IParser<TToken, T>
        => BoxParser<TToken, T>.Create(new TryParser<Next, TToken, T>(box));

    public static TryParserFactory<TToken, T> Instance { get; }
        = new();
}

internal readonly struct TryParser<Next, TToken, T> : IParser<TToken, T>
    where Next : IParser<TToken, T>
{
    private readonly BoxParser<TToken, T>.Of<Next> _parser;

    public TryParser(BoxParser<TToken, T>.Of<Next> parser)
    {
        _parser = parser;
    }

    public bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out T result)
    {
        // start buffering the input
        var bookmark = state.Bookmark();
        if (!_parser.Value.TryParse(ref state, ref expecteds, out result))
        {
            // return to the start of the buffer and discard the bookmark
            state.Rewind(bookmark);
            return false;
        }

        // discard the buffer
        state.DiscardBookmark(bookmark);
        return true;
    }
}
