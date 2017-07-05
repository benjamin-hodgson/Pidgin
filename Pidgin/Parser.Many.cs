using System.Collections.Generic;
using System.Linq;
using Pidgin.ParseStates;

namespace Pidgin
{
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