namespace Pidgin;

public static partial class Parser<TToken>
{
    /// <summary>
    /// A parser which returns the number of input tokens which have been consumed.
    /// </summary>
    public static Parser<TToken, int> CurrentOffset { get; }
        = BoxParser<TToken, int>.Create(default(CurrentOffsetParser<TToken>));
}

internal readonly struct CurrentOffsetParser<TToken> : IParser<TToken, int>
{
    public bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, out int result)
    {
        result = state.Location;
        return true;
    }
}
