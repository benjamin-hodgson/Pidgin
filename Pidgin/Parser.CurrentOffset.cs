namespace Pidgin;

public static partial class Parser<TToken>
{
    /// <summary>
    /// A parser which returns the number of input tokens which have been consumed.
    /// </summary>
    public static Parser<TToken, long> CurrentOffsetLong { get; }
        = new CurrentOffsetParser<TToken>();

    /// <summary>
    /// A parser which returns the number of input tokens which have been consumed.
    /// </summary>
    public static Parser<TToken, int> CurrentOffset { get; }
        = CurrentOffsetLong.Select(x => (int)x);
}

internal sealed class CurrentOffsetParser<TToken> : Parser<TToken, long>
{
    public sealed override bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, out long result)
    {
        result = state.Location;
        return true;
    }
}
