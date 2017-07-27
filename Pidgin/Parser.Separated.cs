using System;
using System.Collections.Generic;
using Pidgin.ParseStates;

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
                .Or(_returnEmptyEnumerable);
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
            return new SeparatedAtLeastOnceParser<U>(this, separator);
        }

        private sealed class SeparatedAtLeastOnceParser<U> : Parser<TToken, IEnumerable<T>>
        {
            private readonly Parser<TToken, T> _parser;
            private readonly Parser<TToken, T> _remainderParser;

            public SeparatedAtLeastOnceParser(Parser<TToken, T> parser, Parser<TToken, U> separator) : base(parser.Expected)
            {
                _parser = parser;
                _remainderParser = separator.Then(parser);
            }

            internal override InternalResult<IEnumerable<T>> Parse(IParseState<TToken> state)
            {
                var result = _parser.Parse(state);
                if (!result.Success)
                {
                    // state.Error set by _parser
                    return InternalResult.Failure<IEnumerable<T>>(result.ConsumedInput);
                }
                return Rest(_remainderParser, state, new List<T> { result.Value }, result.ConsumedInput);
            }

            private InternalResult<IEnumerable<T>> Rest(Parser<TToken, T> parser, IParseState<TToken> state, List<T> ts, bool consumedInput)
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
                    // state.Error set by parser
                    return InternalResult.Failure<IEnumerable<T>>(true);
                }
                return InternalResult.Success<IEnumerable<T>>(ts, consumedInput);
            }
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
                .Or(_returnEmptyEnumerable);
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
            return new SeparatedAndOptionallyTerminatedAtLeastOnceParser<U>(this, separator);
        }

        private sealed class SeparatedAndOptionallyTerminatedAtLeastOnceParser<U> : Parser<TToken, IEnumerable<T>>
        {
            private readonly Parser<TToken, T> _parser;
            private readonly Parser<TToken, U> _separator;

            public SeparatedAndOptionallyTerminatedAtLeastOnceParser(Parser<TToken, T> parser, Parser<TToken, U> separator) : base(parser.Expected)
            {
                _parser = parser;
                _separator = separator;
            }

            internal override InternalResult<IEnumerable<T>> Parse(IParseState<TToken> state)
            {
                var result = _parser.Parse(state);
                if (!result.Success)
                {
                    // state.Error set by _parser
                    return InternalResult.Failure<IEnumerable<T>>(result.ConsumedInput);
                }
                var ts = new List<T> { result.Value };
                var consumedInput = result.ConsumedInput;

                while (true)
                {
                    var sepResult = _separator.Parse(state);
                    consumedInput = consumedInput || sepResult.ConsumedInput;
                    if (!sepResult.Success)
                    {
                        if (sepResult.ConsumedInput)
                        {
                            // state.Error set by _separator
                            return InternalResult.Failure<IEnumerable<T>>(consumedInput);
                        }
                        return InternalResult.Success<IEnumerable<T>>(ts, consumedInput);
                    }

                    var itemResult = _parser.Parse(state);
                    consumedInput = consumedInput || itemResult.ConsumedInput;
                    if (!itemResult.Success)
                    {
                        if (itemResult.ConsumedInput)
                        {
                            // state.Error set by _parser
                            return InternalResult.Failure<IEnumerable<T>>(consumedInput);
                        }
                        return InternalResult.Success<IEnumerable<T>>(ts, consumedInput);
                    }
                    ts.Add(itemResult.Value);
                }
            }
        }
    }
}