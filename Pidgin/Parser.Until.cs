using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Pidgin
{
    public abstract partial class Parser<TToken, T>
    {
        /// <summary>
        /// Creates a parser which applies this parser zero or more times until <paramref name="terminator"/> succeeds.
        /// Fails if this parser fails or if <paramref name="terminator"/> fails after consuming input.
        /// The return value of <paramref name="terminator"/> is ignored.
        /// </summary>
        /// <typeparam name="U">The return type of <paramref name="terminator"/></typeparam>
        /// <param name="terminator">A parser to parse a terminator</param>
        /// <returns>A parser which applies this parser repeatedly until <paramref name="terminator"/> succeeds</returns>
        public Parser<TToken, IEnumerable<T>> Until<U>(Parser<TToken, U> terminator)
        {
            if (terminator == null)
            {
                throw new ArgumentNullException(nameof(terminator));
            }
            return terminator.Then(ReturnEmptyEnumerable)
                .Or(this.AtLeastOnceUntil(terminator));
        }
        
        /// <summary>
        /// Creates a parser which applies this parser one or more times until <paramref name="terminator"/> succeeds.
        /// Fails if this parser fails or if <paramref name="terminator"/> fails after consuming input.
        /// The return value of <paramref name="terminator"/> is ignored.
        /// </summary>
        /// <param name="terminator">A parser to parse a terminator</param>
        /// <returns>A parser which applies this parser repeatedly until <paramref name="terminator"/> succeeds</returns>
        public Parser<TToken, IEnumerable<T>> AtLeastOnceUntil<U>(Parser<TToken, U> terminator)
        {
            if (terminator == null)
            {
                throw new ArgumentNullException(nameof(terminator));
            }
            return new AtLeastOnceUntilParser<TToken, T, U>(this, terminator, true)!;
        }

        /// <summary>
        /// Creates a parser which applies this parser zero or more times until <paramref name="terminator"/> succeeds, discarding the results.
        /// This is more efficient than <see cref="Until{U}(Parser{TToken, U})"/> if you don't need the results.
        /// Fails if this parser fails or if <paramref name="terminator"/> fails after consuming input.
        /// The return value of <paramref name="terminator"/> is ignored.
        /// </summary>
        /// <typeparam name="U">The return type of <paramref name="terminator"/></typeparam>
        /// <param name="terminator">A parser to parse a terminator</param>
        /// <returns>A parser which applies this parser repeatedly until <paramref name="terminator"/> succeeds, discarding the results</returns>
        public Parser<TToken, Unit> SkipUntil<U>(Parser<TToken, U> terminator)
        {
            if (terminator == null)
            {
                throw new ArgumentNullException(nameof(terminator));
            }
            return terminator.Then(ReturnUnit)
                .Or(this.SkipAtLeastOnceUntil(terminator));
        }
        
        /// <summary>
        /// Creates a parser which applies this parser one or more times until <paramref name="terminator"/> succeeds, discarding the results.
        /// This is more efficient than <see cref="AtLeastOnceUntil{U}(Parser{TToken, U})"/> if you don't need the results.
        /// Fails if this parser fails or if <paramref name="terminator"/> fails after consuming input.
        /// The return value of <paramref name="terminator"/> is ignored.
        /// </summary>
        /// <param name="terminator">A parser to parse a terminator</param>
        /// <returns>A parser which applies this parser repeatedly until <paramref name="terminator"/> succeeds, discarding the results</returns>
        public Parser<TToken, Unit> SkipAtLeastOnceUntil<U>(Parser<TToken, U> terminator)
        {
            if (terminator == null)
            {
                throw new ArgumentNullException(nameof(terminator));
            }
            return new AtLeastOnceUntilParser<TToken, T, U>(this, terminator, false).Then(ReturnUnit);
        }
    }

    internal sealed class AtLeastOnceUntilParser<TToken, T, U> : Parser<TToken, IEnumerable<T>?>
    {
        private readonly Parser<TToken, T> _parser;
        private readonly Parser<TToken, U> _terminator;
        private readonly bool _keepResults;

        public AtLeastOnceUntilParser(Parser<TToken, T> parser, Parser<TToken, U> terminator, bool keepResults) : base()
        {
            _parser = parser;
            _terminator = terminator;
            _keepResults = keepResults;
        }

        // see comment about expecteds in ParseState.Error.cs
        internal sealed override bool TryParse(ref ParseState<TToken> state, ref ExpectedCollector<TToken> expecteds, [MaybeNullWhen(false)] out IEnumerable<T>? result)
        {
            var ts = _keepResults ? new List<T>() : null;

            var firstItemStartLoc = state.Location;

            if (!_parser.TryParse(ref state, ref expecteds, out var result1))
            {
                // state.Error set by _parser
                result = null;
                return false;
            }
            if (state.Location <= firstItemStartLoc)
            {
                throw new InvalidOperationException("Until() used with a parser which consumed no input");
            }
            ts?.Add(result1);

            var terminatorExpecteds = new ExpectedCollector<TToken>();
            var itemExpecteds = new ExpectedCollector<TToken>();
            while (true)
            {
                var terminatorStartLoc = state.Location;
                var terminatorSuccess = _terminator.TryParse(ref state, ref terminatorExpecteds, out var terminatorResult);
                if (terminatorSuccess)
                {
                    terminatorExpecteds.Dispose();
                    itemExpecteds.Dispose();
                    result = ts;
                    return true;
                }
                if (state.Location > terminatorStartLoc)
                {
                    // state.Error set by _terminator
                    expecteds.Add(ref terminatorExpecteds);
                    terminatorExpecteds.Dispose();
                    itemExpecteds.Dispose();
                    result = null;
                    return false;
                }

                var itemStartLoc = state.Location;
                var itemSuccess = _parser.TryParse(ref state, ref itemExpecteds, out var itemResult);
                var itemConsumedInput = state.Location > itemStartLoc;
                if (!itemSuccess)
                {
                    if (!itemConsumedInput)
                    {
                        // get the expected from both _terminator and _parser
                        expecteds.Add(ref terminatorExpecteds);
                        expecteds.Add(ref itemExpecteds);
                    }
                    else
                    {
                        // throw out the _terminator expecteds and keep only _parser
                        expecteds.Add(ref itemExpecteds);
                    }
                    terminatorExpecteds.Dispose();
                    itemExpecteds.Dispose();
                    result = null;
                    return false;
                }
                // throw out both sets of expecteds
                terminatorExpecteds.Clear();
                itemExpecteds.Clear();
                if (!itemConsumedInput)
                {
                    throw new InvalidOperationException("Until() used with a parser which consumed no input");
                }
                ts?.Add(itemResult!);
            }
        }
    }
}