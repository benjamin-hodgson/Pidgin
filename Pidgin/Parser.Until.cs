using System;
using System.Collections.Generic;
using System.Collections.Immutable;

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
            return new AtLeastOnceUntilParser<U>(this, terminator, true);
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
            return new AtLeastOnceUntilParser<U>(this, terminator, false).Then(ReturnUnit);
        }

        private sealed class AtLeastOnceUntilParser<U> : Parser<TToken, IEnumerable<T>>
        {
            private readonly Parser<TToken, T> _parser;
            private readonly Parser<TToken, U> _terminator;
            private ImmutableSortedSet<Expected<TToken>> _round2Expected;
            private ImmutableSortedSet<Expected<TToken>> Round2Expected
            {
                get
                {
                    if (_round2Expected == null)
                    {
                        _round2Expected = ExpectedUtil.Union(_parser.Expected, _terminator.Expected);
                    }
                    return _round2Expected;
                }
            }
            private readonly bool _keepResults;

            public AtLeastOnceUntilParser(Parser<TToken, T> parser, Parser<TToken, U> terminator, bool keepResults) : base()
            {
                _parser = parser;
                _terminator = terminator;
                _keepResults = keepResults;
            }

            private protected override ImmutableSortedSet<Expected<TToken>> CalculateExpected()
                => ExpectedUtil.Concat(_parser.Expected, _terminator.Expected);

            internal override InternalResult<IEnumerable<T>> Parse(ref ParseState<TToken> state)
            {
                var ts = _keepResults ? new List<T>() : null;
                var firstTime = true;
                var consumedInput = false;
                InternalResult<U> terminatorResult;
                do
                {
                    var itemResult = _parser.Parse(ref state);
                    consumedInput = consumedInput || itemResult.ConsumedInput;
                    if (!itemResult.Success)
                    {
                        if (itemResult.ConsumedInput)
                        {
                            // state.Error set by _parser
                            return InternalResult.Failure<IEnumerable<T>>(consumedInput);
                        }
                        state.Error = state.Error.WithExpected(firstTime ? Expected : Round2Expected);
                        return InternalResult.Failure<IEnumerable<T>>(consumedInput);
                    }
                    if (!itemResult.ConsumedInput)
                    {
                        throw new InvalidOperationException("Until() used with a parser which consumed no input");
                    }
                    ts?.Add(itemResult.Value);


                    terminatorResult = _terminator.Parse(ref state);
                    if (terminatorResult.ConsumedInput && !terminatorResult.Success)
                    {
                        // state.Error set by _terminator
                        return InternalResult.Failure<IEnumerable<T>>(consumedInput);
                    }
                    firstTime = false;
                } while (!terminatorResult.Success);
                return InternalResult.Success<IEnumerable<T>>(ts, consumedInput);
            }
        }
    }
}