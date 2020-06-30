namespace Pidgin
{
    public static partial class Parser<TToken>
    {
        /// <summary>
        /// A parser which returns the number of input tokens which have been consumed.
        /// </summary>
        /// <returns>A parser which returns the number of input tokens which have been consumed</returns>
        public static Parser<TToken, int> CurrentOffset { get; }
            = new CurrentOffsetParser<TToken>();
    }

    internal sealed class CurrentOffsetParser<TToken> : Parser<TToken, int>
    {
        internal override InternalResult<int> Parse(ref ParseState<TToken> state, ref ExpectedCollector<TToken> expecteds)
            => InternalResult.Success(state.Location);
    }
}