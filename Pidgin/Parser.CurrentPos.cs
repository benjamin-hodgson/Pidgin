namespace Pidgin;

public static partial class Parser<TToken>
{
    /// <summary>
    /// A parser which returns the current source position.
    /// </summary>
    public static Parser<TToken, SourcePosDelta> CurrentSourcePosDelta { get; }
        = BoxParser<TToken, SourcePosDelta>.Create(default(CurrentPosParser<TToken>));

    /// <summary>
    /// A parser which returns the current source position.
    /// </summary>
    public static Parser<TToken, SourcePos> CurrentPos { get; }
        = CurrentSourcePosDelta.Select(d => new SourcePos(1, 1) + d);
}

internal readonly struct CurrentPosParser<TToken> : IParser<TToken, SourcePosDelta>
{
    public bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, out SourcePosDelta result)
    {
        result = state.ComputeSourcePosDelta();
        return true;
    }
}
