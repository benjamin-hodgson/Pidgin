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

        return new NegatedParser<TToken, T>(parser);
    }
}

[SuppressMessage(
    "StyleCop.CSharp.MaintainabilityRules",
    "SA1402:FileMayOnlyContainASingleType",
    Justification = "This class belongs next to the accompanying API method"
)]
internal sealed class NegatedParser<TToken, T> : Parser<TToken, Unit>
{
    private readonly Parser<TToken, T> _parser;

    public NegatedParser(Parser<TToken, T> parser)
    {
        _parser = parser;
    }

    public sealed override bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out Unit result)
    {
        var startingLocation = state.Location;
        var token = state.HasCurrent ? Maybe.Just(state.Current) : Maybe.Nothing<TToken>();

        var bookmark = state.Bookmark();  // make sure we don't throw out the buffer, we may need it to compute a SourcePos
        var childExpecteds = new PooledList<Expected<TToken>>(state.Configuration.ArrayPoolProvider.GetArrayPool<Expected<TToken>>());
        var success = _parser.TryParse(ref state, ref childExpecteds, out _);

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
