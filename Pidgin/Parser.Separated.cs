using System;
using System.Collections.Generic;

namespace Pidgin
{
    public abstract partial class Parser<TToken, T>
    {
        /// <summary>
        /// Creates a parser which applies the current parser repeatedly, interleaved with a specified parser.
        /// The resulting parser ignores the return value of the separator parser.
        /// </summary>
        /// <typeparam name="U">The return type of the separator parser</typeparam>
        /// <param name="separator">A parser which parses a separator to be interleaved with the current parser</param>
        /// <returns>A parser which applies the current parser repeatedly, interleaved by <paramref name="separator"/></returns>
        public Parser<TToken, IEnumerable<T>> Separated<U>(Parser<TToken, U> separator)
        {
            if (separator == null)
            {
                throw new ArgumentNullException(nameof(separator));
            }
            return this.SeparatedAtLeastOnce(separator)
                .Or(ReturnEmptyEnumerable);
        }
        
        /// <summary>
        /// Creates a parser which applies the current parser at least once, interleaved with a specified parser.
        /// The resulting parser ignores the return value of the separator parser.
        /// </summary>
        /// <typeparam name="U">The return type of the separator parser</typeparam>
        /// <param name="separator">A parser which parses a separator to be interleaved with the current parser</param>
        /// <returns>A parser which applies the current parser at least once, interleaved by <paramref name="separator"/></returns>
        public Parser<TToken, IEnumerable<T>> SeparatedAtLeastOnce<U>(Parser<TToken, U> separator)
        {
            if (separator == null)
            {
                throw new ArgumentNullException(nameof(separator));
            }
            return new SeparatedAtLeastOnceParser<TToken, T, U>(this, separator);
        }

        /// <summary>
        /// Creates a parser which applies the current parser repeatedly, interleaved and terminated with a specified parser.
        /// The resulting parser ignores the return value of the separator parser.
        /// </summary>
        /// <typeparam name="U">The return type of the separator parser</typeparam>
        /// <param name="separator">A parser which parses a separator to be interleaved with the current parser</param>
        /// <returns>A parser which applies the current parser repeatedly, interleaved and terminated by <paramref name="separator"/></returns>
        public Parser<TToken, IEnumerable<T>> SeparatedAndTerminated<U>(Parser<TToken, U> separator)
        {
            if (separator == null)
            {
                throw new ArgumentNullException(nameof(separator));
            }
            return this.Before(separator).Many();
        }
        
        /// <summary>
        /// Creates a parser which applies the current parser at least once, interleaved and terminated with a specified parser.
        /// The resulting parser ignores the return value of the separator parser.
        /// </summary>
        /// <typeparam name="U">The return type of the separator parser</typeparam>
        /// <param name="separator">A parser which parses a separator to be interleaved with the current parser</param>
        /// <returns>A parser which applies the current parser at least once, interleaved and terminated by <paramref name="separator"/></returns>
        public Parser<TToken, IEnumerable<T>> SeparatedAndTerminatedAtLeastOnce<U>(Parser<TToken, U> separator)
        {
            if (separator == null)
            {
                throw new ArgumentNullException(nameof(separator));
            }
            return this.Before(separator).AtLeastOnce();
        }

        /// <summary>
        /// Creates a parser which applies the current parser repeatedly, interleaved and optionally terminated with a specified parser.
        /// The resulting parser ignores the return value of the separator parser.
        /// </summary>
        /// <typeparam name="U">The return type of the separator parser</typeparam>
        /// <param name="separator">A parser which parses a separator to be interleaved with the current parser</param>
        /// <returns>A parser which applies the current parser repeatedly, interleaved and optionally terminated by <paramref name="separator"/></returns>
        public Parser<TToken, IEnumerable<T>> SeparatedAndOptionallyTerminated<U>(Parser<TToken, U> separator)
        {
            if (separator == null)
            {
                throw new ArgumentNullException(nameof(separator));
            }
            return this.SeparatedAndOptionallyTerminatedAtLeastOnce(separator)
                .Or(ReturnEmptyEnumerable);
        }
        
        /// <summary>
        /// Creates a parser which applies the current parser at least once, interleaved and optionally terminated with a specified parser.
        /// The resulting parser ignores the return value of the separator parser.
        /// </summary>
        /// <typeparam name="U">The return type of the separator parser</typeparam>
        /// <param name="separator">A parser which parses a separator to be interleaved with the current parser</param>
        /// <returns>A parser which applies the current parser at least once, interleaved and optionally terminated by <paramref name="separator"/></returns>
        public Parser<TToken, IEnumerable<T>> SeparatedAndOptionallyTerminatedAtLeastOnce<U>(Parser<TToken, U> separator)
        {
            if (separator == null)
            {
                throw new ArgumentNullException(nameof(separator));
            }
            return new SeparatedAndOptionallyTerminatedAtLeastOnceParser<TToken, T, U>(this, separator);
        }
    }

    internal sealed class SeparatedAtLeastOnceParser<TToken, T, U> : Parser<TToken, IEnumerable<T>>
    {
        private readonly Parser<TToken, T> _parser;
        private readonly Parser<TToken, T> _remainderParser;

        public SeparatedAtLeastOnceParser(Parser<TToken, T> parser, Parser<TToken, U> separator)
        {
            _parser = parser;
            _remainderParser = separator.Then(parser);
        }

        internal sealed override bool TryParse(ref ParseState<TToken> state, ref ExpectedCollector<TToken> expecteds, out IEnumerable<T> result)
        {
            var success = _parser.TryParse(ref state, ref expecteds, out var result1);
            if (!success)
            {
                // state.Error set by _parser
                result = null;
                return false;
            }
            var list = new List<T> { result1 };
            success = Rest(_remainderParser, ref state, ref expecteds, list);
            if (!success)
            {
                result = null;
                return false;
            }
            result = list;
            return true;
        }

        private bool Rest(Parser<TToken, T> parser, ref ParseState<TToken> state, ref ExpectedCollector<TToken> expecteds, List<T> ts)
        {
            var lastStartingLoc = state.Location;
            var childExpecteds = new ExpectedCollector<TToken>();
            var success = parser.TryParse(ref state, ref childExpecteds, out var result);
            while (success)
            {
                var endingLoc = state.Location;
                childExpecteds.Clear();

                if (endingLoc <= lastStartingLoc)
                {
                    childExpecteds.Dispose();
                    throw new InvalidOperationException("Many() used with a parser which consumed no input");
                }

                ts.Add(result);

                lastStartingLoc = endingLoc;
                success = parser.TryParse(ref state, ref childExpecteds, out result);
            }
            var lastParserConsumedInput = state.Location > lastStartingLoc;
            expecteds.AddIf(ref childExpecteds, lastParserConsumedInput);
            childExpecteds.Dispose();

            // we fail if the most recent parser failed after consuming input.
            // it sets state.Error for us
            return !lastParserConsumedInput;
        }
    }

    internal sealed class SeparatedAndOptionallyTerminatedAtLeastOnceParser<TToken, T, U> : Parser<TToken, IEnumerable<T>>
    {
        private readonly Parser<TToken, T> _parser;
        private readonly Parser<TToken, U> _separator;

        public SeparatedAndOptionallyTerminatedAtLeastOnceParser(Parser<TToken, T> parser, Parser<TToken, U> separator)
        {
            _parser = parser;
            _separator = separator;
        }

        internal sealed override bool TryParse(ref ParseState<TToken> state, ref ExpectedCollector<TToken> expecteds, out IEnumerable<T> result)
        {
            var success = _parser.TryParse(ref state, ref expecteds, out var result1);
            if (!success)
            {
                // state.Error set by _parser
                result = null;
                return false;
            }
            var ts = new List<T> { result1 };

            var childExpecteds = new ExpectedCollector<TToken>();
            while (true)
            {
                var sepStartLoc = state.Location;
                var sepSuccess = _separator.TryParse(ref state, ref childExpecteds, out var _);
                var sepConsumedInput = state.Location > sepStartLoc;

                expecteds.AddIf(ref childExpecteds, !sepSuccess && sepConsumedInput);
                childExpecteds.Clear();

                if (!sepSuccess)
                {
                    childExpecteds.Dispose();
                    if (sepConsumedInput)
                    {
                        // state.Error set by _separator
                        result = null;
                        return false;
                    }
                    result = ts;
                    return true;
                }


                var itemStartLoc = state.Location;
                var itemSuccess = _parser.TryParse(ref state, ref childExpecteds, out var itemResult);
                var itemConsumedInput = state.Location > itemStartLoc;

                expecteds.AddIf(ref childExpecteds, !itemSuccess && itemConsumedInput);
                childExpecteds.Clear();

                if (!itemSuccess)
                {
                    childExpecteds.Dispose();
                    if (itemConsumedInput)
                    {
                        // state.Error set by _parser
                        result = null;
                        return false;
                    }
                    result = ts;
                    return true;
                }
                ts.Add(itemResult);
            }
        }
    }
}