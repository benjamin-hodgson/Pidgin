namespace Pidgin
{
    public static partial class Parser<TToken>
    {
        /// <summary>
        /// A parser which returns the current source position
        /// </summary>
        /// <returns>A parser which returns the current source position</returns>
        public static Parser<TToken, SourcePos> CurrentPos { get; }
            = new CurrentPosParser<TToken>();
    }

    internal sealed class CurrentPosParser<TToken> : Parser<TToken, SourcePos>
    {
        internal sealed override bool TryParse(ref ParseState<TToken> state, ref ExpectedCollector<TToken> expecteds, out SourcePos result)
        {
            result = state.ComputeSourcePos();
            return true;
        }
    }
}