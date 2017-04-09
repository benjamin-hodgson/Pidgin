using System.Collections.Generic;
using Pidgin.ParseStates;

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
            => terminator.Then(_returnEmptyEnumerable)
                .Or(this.AtLeastOnceUntil(terminator));
        
        /// <summary>
        /// Creates a parser which applies this parser one or more times until <paramref name="terminator"/> succeeds.
        /// Fails if this parser fails or if <paramref name="terminator"/> fails after consuming input.
        /// The return value of <paramref name="terminator"/> is ignored.
        /// </summary>
        /// <param name="terminator">A parser to parse a terminator</param>
        /// <returns>A parser which applies this parser repeatedly until <paramref name="terminator"/> succeeds</returns>
        public Parser<TToken, IEnumerable<T>> AtLeastOnceUntil<U>(Parser<TToken, U> terminator)
            => new AtLeastOnceUntilParser<U>(this, terminator, true);

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
            => terminator.Then(_returnUnit)
                .Or(this.SkipAtLeastOnceUntil(terminator));
        
        /// <summary>
        /// Creates a parser which applies this parser one or more times until <paramref name="terminator"/> succeeds, discarding the results.
        /// This is more efficient than <see cref="AtLeastOnceUntil{U}(Parser{TToken, U})"/> if you don't need the results.
        /// Fails if this parser fails or if <paramref name="terminator"/> fails after consuming input.
        /// The return value of <paramref name="terminator"/> is ignored.
        /// </summary>
        /// <param name="terminator">A parser to parse a terminator</param>
        /// <returns>A parser which applies this parser repeatedly until <paramref name="terminator"/> succeeds, discarding the results</returns>
        public Parser<TToken, Unit> SkipAtLeastOnceUntil<U>(Parser<TToken, U> terminator)
            => new AtLeastOnceUntilParser<U>(this, terminator, false).Then(_returnUnit);

        private sealed class AtLeastOnceUntilParser<U> : Parser<TToken, IEnumerable<T>>
        {
            private readonly Parser<TToken, T> _parser;
            private readonly Parser<TToken, U> _terminator;
            private readonly SortedSet<Expected<TToken>> _round2Expected;
            private readonly bool _keepResults;

            public AtLeastOnceUntilParser(Parser<TToken, T> parser, Parser<TToken, U> terminator, bool keepResults) : base(ExpectedUtil.Concat(parser.Expected, terminator.Expected))
            {
                _parser = parser;
                _terminator = terminator;
                _round2Expected = ExpectedUtil.Union(parser.Expected, terminator.Expected);
                _keepResults = keepResults;
            }

            internal override Result<TToken, IEnumerable<T>> Parse(IParseState<TToken> state)
            {
                var ts = _keepResults ? new List<T>() : null;
                var firstTime = true;
                var consumedInput = false;
                Result<TToken, U> terminatorResult;
                do
                {
                    var tResult = _parser.Parse(state);
                    consumedInput = consumedInput || tResult.ConsumedInput;
                    if (!tResult.Success)
                    {
                        if (tResult.ConsumedInput)
                        {
                            return Result.Failure<TToken, IEnumerable<T>>(tResult.Error, consumedInput);
                        }
                        return Result.Failure<TToken, IEnumerable<T>>(tResult.Error.WithExpected(firstTime ? Expected : _round2Expected), consumedInput);
                    }
                    ts?.Add(tResult.GetValueOrDefault());


                    terminatorResult = _terminator.Parse(state);
                    consumedInput = consumedInput || terminatorResult.ConsumedInput;
                    if (terminatorResult.ConsumedInput && !terminatorResult.Success)
                    {
                        return Result.Failure<TToken, IEnumerable<T>>(terminatorResult.Error, consumedInput);
                    }
                    firstTime = false;
                } while (!terminatorResult.Success);
                return Result.Success<TToken, IEnumerable<T>>(ts, consumedInput);
            }
        }
    }
}