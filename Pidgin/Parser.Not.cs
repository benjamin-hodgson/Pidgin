using System;
using System.Diagnostics.CodeAnalysis;

namespace Pidgin;

public static partial class Parser
{
    /// <summary>
    /// Creates a parser which succeeds only if the given parser fails.
    /// The resulting parser does not perform any backtracking; it consumes the same amount of input as the supplied parser.
    /// Combine this function with <see cref="Try{TToken, T}(Parser{TToken, T})"/> if this behaviour is undesirable.
    /// </summary>
    /// <param name="parser">The parser that is expected to fail.</param>
    /// <typeparam name="TToken">The type of the tokens in the parser's input stream.</typeparam>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <returns>A parser which succeeds only if the given parser fails.</returns>
    public static Parser<TToken, Unit> Not<TToken, T>(Parser<TToken, T> parser)
    {
        if (parser == null)
        {
            throw new ArgumentNullException(nameof(parser));
        }

        return parser.Accept(NegatedParserFactory<TToken, T>.Instance);
    }
}

internal class NegatedParserFactory<TToken, T> : IReboxer<TToken, T, Unit>
{
    private NegatedParserFactory()
    {
    }

    public BoxParser<TToken, Unit> WithBox<Next>(BoxParser<TToken, T>.Of<Next> box)
        where Next : IParser<TToken, T>
        => BoxParser<TToken, Unit>.Create(new NegatedParser<Next, TToken, T>(box));

    public static NegatedParserFactory<TToken, T> Instance { get; } = new();
}

internal readonly struct NegatedParser<Next, TToken, T> : IParser<TToken, Unit>
    where Next : IParser<TToken, T>
{
    private readonly BoxParser<TToken, T>.Of<Next> _parser;

    public NegatedParser(BoxParser<TToken, T>.Of<Next> parser)
    {
        _parser = parser;
    }

    public bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out Unit result)
    {
        var startingLocation = state.Location;
        var token = state.HasCurrent ? Maybe.Just(state.Current) : Maybe.Nothing<TToken>();

        var bookmark = state.Bookmark();  // make sure we don't throw out the buffer, we may need it to compute a SourcePos
        var childExpecteds = new PooledList<Expected<TToken>>(state.Configuration.ArrayPoolProvider.GetArrayPool<Expected<TToken>>());
        var success = _parser.Value.TryParse(ref state, ref childExpecteds, out _);

        childExpecteds.Dispose();
        state.DiscardBookmark(bookmark);

        if (success)
        {
            state.SetError(token, false, startingLocation, null);
            result = default;
            return false;
        }

        result = Unit.Value;
        return true;
    }
}
