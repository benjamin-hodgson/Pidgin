using System.Diagnostics.CodeAnalysis;

namespace Pidgin
{
    public static partial class Parser<TToken>
    {
        /// <summary>
        /// A parser which returns the current source position
        /// </summary>
        /// <returns>A parser which returns the current source position</returns>
        [SuppressMessage("design", "CA1000")]  // "Do not declare static members on generic types"
        public static Parser<TToken, SourcePosDelta> CurrentSourcePosDelta { get; }
            = new CurrentPosParser<TToken>();

        /// <summary>
        /// A parser which returns the current source position
        /// </summary>
        /// <returns>A parser which returns the current source position</returns>
        [SuppressMessage("design", "CA1000")]  // "Do not declare static members on generic types"
        public static Parser<TToken, SourcePos> CurrentPos { get; }
            = CurrentSourcePosDelta.Select(d => new SourcePos(1, 1) + d);
    }

    internal sealed class CurrentPosParser<TToken> : Parser<TToken, SourcePosDelta>
    {
        public sealed override bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, out SourcePosDelta result)
        {
            result = state.ComputeSourcePosDelta();
            return true;
        }
    }
}
