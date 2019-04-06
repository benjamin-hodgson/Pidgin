using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pidgin
{
    public static partial class Parser
    {
        /// <summary>
        /// Creates a parser which applies the current parser zero or more times, packing the resulting characters into a string.
        /// Equivalent to <c>parser.Many().Select(cs => string.Concat(cs))</c>
        /// </summary>
        /// <param name="parser">A parser returning a single character</param>
        /// <returns>A parser which applies the current parser zero or more times, packing the resulting characters into a string.</returns>
        public static Parser<TToken, string> ManyString<TToken>(this Parser<TToken, char> parser)
        {
            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }
            
            return parser.AtLeastOnceString().Or(Parser<TToken>.Return(""));
        }

        /// <summary>
        /// Creates a parser which applies the current parser zero or more times, concatenating the resulting string pieces.
        /// Equivalent to <c>parser.Many().Select(cs => string.Concat(cs))</c>
        /// </summary>
        /// <param name="parser">A parser returning a single character</param>
        /// <returns>A parser which applies the current parser zero or more times, concatenating the resulting string pieces.</returns>
        public static Parser<TToken, string> ManyString<TToken>(this Parser<TToken, string> parser)
        {
            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }

            return parser.AtLeastOnceString().Or(Parser<TToken>.Return(""));
        }
        
        /// <summary>
        /// Creates a parser which applies the current parser one or more times, packing the resulting characters into a string.
        /// Equivalent to <c>parser.Many().Select(cs => string.Concat(cs))</c>
        /// </summary>
        /// <param name="parser">A parser returning a single character</param>
        /// <returns>A parser which applies the current parser one or more times, packing the resulting characters into a string.</returns>
        public static Parser<TToken, string> AtLeastOnceString<TToken>(this Parser<TToken, char> parser)
        {
            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }

            return parser.AtLeastOncePooled()
                .Select(sb => GetStringAndClear(sb));
        }
        
        /// <summary>
        /// Creates a parser which applies the current parser one or more times, concatenating the resulting string pieces.
        /// Equivalent to <c>parser.Many().Select(cs => string.Concat(cs))</c>
        /// </summary>
        /// <param name="parser">A parser returning a single character</param>
        /// <returns>A parser which applies the current parser one or more times, concatenating the resulting string pieces.</returns>
        public static Parser<TToken, string> AtLeastOnceString<TToken>(this Parser<TToken, string> parser)
        {
            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }

            return parser.ChainAtLeastOnceL(
                () => new PooledList<char>(),
                (sb, c) => { sb.AddRange(c.AsSpan()); return sb; },
                onFail: sb => { sb.Clear(); }
            ).Select(sb => GetStringAndClear(sb));
        }

        private static string GetStringAndClear(PooledList<char> sb)
        {
            var str = sb.AsSpan().ToString();
            sb.Clear();
            return str;
        }
    }

    public abstract partial class Parser<TToken, T>
    {
        private static Parser<TToken, IEnumerable<T>> _returnEmptyEnumerable;
        private static Parser<TToken, IEnumerable<T>> ReturnEmptyEnumerable
        {
            get
            {
                if (_returnEmptyEnumerable == null)
                {
                    _returnEmptyEnumerable = Parser<TToken>.Return(Enumerable.Empty<T>());
                }
                return _returnEmptyEnumerable;
            }
        }
        private static Parser<TToken, Unit> _returnUnit;
        private static Parser<TToken, Unit> ReturnUnit
        {
            get
            {
                if (_returnUnit == null)
                {
                    _returnUnit = Parser<TToken>.Return(Unit.Value);
                }
                return _returnUnit;
            }
        }

        /// <summary>
        /// Creates a parser which applies the current parser zero or more times.
        /// The resulting parser fails if the current parser fails after consuming input.
        /// </summary>
        /// <returns>A parser which applies the current parser zero or more times</returns>
        public Parser<TToken, IEnumerable<T>> Many()
            => this.AtLeastOnce()
                .Or(ReturnEmptyEnumerable);

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

        internal Parser<TToken, PooledList<T>> AtLeastOncePooled()
            => this.ChainAtLeastOnceL(
                () => new PooledList<T>(),
                (xs, x) => { xs.Add(x); return xs; },
                onFail: xs => { xs.Clear(); }
            );

        /// <summary>
        /// Creates a parser which applies the current parser zero or more times, discarding the results.
        /// This is more efficient than <see cref="Many()"/>, if you don't need the results.
        /// The resulting parser fails if the current parser fails after consuming input.
        /// </summary>
        /// <returns>A parser which applies the current parser zero or more times</returns>
        public Parser<TToken, Unit> SkipMany()
            => this.SkipAtLeastOnce()
                .Or(ReturnUnit);

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