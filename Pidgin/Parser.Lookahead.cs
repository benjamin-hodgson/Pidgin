using System;
using System.Diagnostics.CodeAnalysis;

namespace Pidgin;

public static partial class Parser
{
    /// <summary>
    /// If <paramref name="parser"/> succeeds, <c>Lookahead(parser)</c> backtracks,
    /// behaving as if <paramref name="parser"/> had not consumed any input.
    /// No backtracking is performed upon failure.
    /// </summary>
    /// <param name="parser">The parser to look ahead with.</param>
    /// <typeparam name="TToken">The type of the tokens in the parser's input stream.</typeparam>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <returns>A parser which rewinds the input stream if <paramref name="parser"/> succeeds.</returns>
    public static Parser<TToken, T> Lookahead<TToken, T>(Parser<TToken, T> parser)
    {
        if (parser == null)
        {
            throw new ArgumentNullException(nameof(parser));
        }

        return LookaheadParserFactory<TToken, T>.Instance.Unbox(parser);
    }
}

internal class LookaheadParserFactory<TToken, T> : IUnboxer<TToken, T, BoxParser<TToken, T>>
{
    public LookaheadParserFactory()
    {
    }

    public BoxParser<TToken, T> Unbox<TImpl>(BoxParser<TToken, T>.Of<TImpl> box)
        where TImpl : IParser<TToken, T>
        => BoxParser<TToken, T>.Create(new LookaheadParser<TImpl, TToken, T>(box));

    public static LookaheadParserFactory<TToken, T> Instance { get; }
        = new();
}

internal readonly struct LookaheadParser<Next, TToken, T> : IParser<TToken, T>
    where Next : IParser<TToken, T>
{
    private readonly BoxParser<TToken, T>.Of<Next> _parser;

    public LookaheadParser(BoxParser<TToken, T>.Of<Next> parser)
    {
        _parser = parser;
    }

    public bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out T result)
    {
        var bookmark = state.Bookmark();

        if (_parser.Value.TryParse(ref state, ref expecteds, out result))
        {
            state.Rewind(bookmark);
            return true;
        }

        state.DiscardBookmark(bookmark);
        return false;
    }
}
