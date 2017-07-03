using System;
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
            => new AtLeastOnceParser(this, true);

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
            => new AtLeastOnceParser(this, false).Then(_returnUnit);

        private abstract class ManyParserBase : Parser<TToken, IEnumerable<T>>
        {
            protected ManyParserBase(SortedSet<Expected<TToken>> expected) : base(expected)
            {
            }

            protected Result<TToken, IEnumerable<T>> ManyImpl(Parser<TToken, T> parser, IParseState<TToken> state, List<T> ts, bool consumedInput)
            {
                var result = parser.Parse(state);
                while (result.Success)
                {
                    if (!result.ConsumedInput)
                    {
                        throw new InvalidOperationException("Many() used with a parser which consumed no input");
                    }
                    consumedInput = true;
                    ts?.Add(result.Value);
                    result = parser.Parse(state);
                }
                if (result.ConsumedInput)  // the most recent parser failed after consuming input
                {
                    return Result.Failure<TToken, IEnumerable<T>>(
                        result.Error,
                        true
                    );
                }
                return Result.Success<TToken, IEnumerable<T>>(ts, consumedInput);
            }
        }

        private sealed class AtLeastOnceParser : ManyParserBase
        {
            private readonly Parser<TToken, T> _parser;
            private readonly bool _keepResults;

            public AtLeastOnceParser(Parser<TToken, T> parser, bool keepResults) : base(parser.Expected)
            {
                _parser = parser;
                _keepResults = keepResults;
            }

            internal sealed override Result<TToken, IEnumerable<T>> Parse(IParseState<TToken> state)
            {
                var result = _parser.Parse(state);
                if (!result.Success)
                {
                    return Result.Failure<TToken, IEnumerable<T>>(
                        result.Error,
                        result.ConsumedInput
                    );
                }
                return ManyImpl(_parser, state, _keepResults ? new List<T> { result.Value } : null, result.ConsumedInput);
            }
        }
    }
}