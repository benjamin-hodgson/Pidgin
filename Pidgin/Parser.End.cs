using System.Diagnostics.CodeAnalysis;

namespace Pidgin;

public static partial class Parser<TToken>
{
    /// <summary>
    /// Creates a parser which parses the end of the input stream.
    /// </summary>
    /// <returns>A parser which parses the end of the input stream and returns <see cref="Unit.Value"/>.</returns>
    public static Parser<TToken, Unit> End { get; } = new EndParser<TToken>();
}

internal sealed class EndParser<TToken> : Parser<TToken, Unit>
{
    public sealed override bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out Unit result)
    {
        if (state.HasCurrent)
        {
            state.SetError(Maybe.Just(state.Current), false, state.Location, null);
            expecteds.Add(default);
            result = default;
            return false;
        }

        result = Unit.Value;
        return true;
    }
}
