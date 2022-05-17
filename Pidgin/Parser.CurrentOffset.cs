using System.Diagnostics.CodeAnalysis;

namespace Pidgin
{
    public static partial class Parser<TToken>
    {
        /// <summary>
        /// A parser which returns the number of input tokens which have been consumed.
        /// </summary>
        /// <returns>A parser which returns the number of input tokens which have been consumed</returns>
        [SuppressMessage("design", "CA1000")]  // "Do not declare static members on generic types"
        public static Parser<TToken, int> CurrentOffset { get; }
            = new CurrentOffsetParser<TToken>();
    }

    internal sealed class CurrentOffsetParser<TToken> : Parser<TToken, int>
    {
        public sealed override bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, out int result)
        {
            result = state.Location;
            return true;
        }
    }
}
