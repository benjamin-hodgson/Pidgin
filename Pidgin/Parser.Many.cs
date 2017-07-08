using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pidgin.ParseStates;

namespace Pidgin
{
    public static partial class Parser
    {
        /// <summary>
        /// Creates a parser which applies the current parser zero or more times, packing the resulting characters into a string.
        /// Equivalent to <code>parser.Many().Select(cs => string.Concat(cs))</code>
        /// </summary>
        /// <param name="parser">A parser returning a single character</param>
        /// <returns>A parser which applies the current parser zero or more times, packing the resulting characters into a string.</returns>
        public static Parser<TToken, string> ManyString<TToken>(this Parser<TToken, char> parser)
            => parser.AtLeastOnceString().Or(Parser<TToken>.Return(""));
        
        /// <summary>
        /// Creates a parser which applies the current parser one or more times, packing the resulting characters into a string.
        /// Equivalent to <code>parser.Many().Select(cs => string.Concat(cs))</code>
        /// </summary>
        /// <param name="parser">A parser returning a single character</param>
        /// <returns>A parser which applies the current parser one or more times, packing the resulting characters into a string.</returns>
        public static Parser<TToken, string> AtLeastOnceString<TToken>(this Parser<TToken, char> parser)
            => parser.ChainAtLeastOnceL(
                () => new StringBuilder(),
                (sb, c) => sb.Append(c)  // returns itself
            ).Select(sb => sb.ToString());
    }

    public abstract partial class Parser<TToken, T>
    {
        private static readonly Parser<TToken, IEnumerable<T>> _returnEmptyEnumerable
            = Parser<TToken>.Return(Enumerable.Empty<T>());
        private static readonly Parser<TToken, Unit> _returnUnit
            = Parser<TToken>.Return(Unit.Value);

        /// <summary>
        /// Creates a parser which applies the current parser zero or more times.
        /// The resulting parser fails if the current parser fails after consuming input.
        /// </summary>
        /// <returns>A parser which applies the current parser zero or more times</returns>
        public Parser<TToken, IEnumerable<T>> Many()
            => this.AtLeastOnce()
                .Or(_returnEmptyEnumerable);

        /// <summary>
        /// Creates a parser that applies the current parser one or more times.
        /// The resulting parser fails if the current parser fails the first time it is applied, or if the current parser fails after consuming input
        /// </summary>
        /// <returns>A parser that applies the current parser one or more times</returns>
        public Parser<TToken, IEnumerable<T>> AtLeastOnce()
            => this.ChainAtLeastOnceL(
                () => new List<T>(),
                (xs, x) => { xs.Add(x); return xs; }
            ).Cast<IEnumerable<T>>();

        /// <summary>
        /// Creates a parser which applies the current parser zero or more times, discarding the results.
        /// This is more efficient than <see cref="Many()"/>, if you don't need the results.
        /// The resulting parser fails if the current parser fails after consuming input.
        /// </summary>
        /// <returns>A parser which applies the current parser zero or more times</returns>
        public Parser<TToken, Unit> SkipMany()
            => this.SkipAtLeastOnce()
                .Or(_returnUnit);

        /// <summary>
        /// Creates a parser that applies the current parser one or more times, discarding the results.
        /// This is more efficient than <see cref="AtLeastOnce()"/>, if you don't need the results.
        /// The resulting parser fails if the current parser fails the first time it is applied, or if the current parser fails after consuming input
        /// </summary>
        /// <returns>A parser that applies the current parser one or more times, discarding the results</returns>
        public Parser<TToken, Unit> SkipAtLeastOnce()
            => this.ChainAtLeastOnceL(
                () => Unit.Value,
                (u, _) => u
            );
    }
}