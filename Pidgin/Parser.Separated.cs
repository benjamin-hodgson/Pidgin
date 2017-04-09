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
            => this.SeparatedAtLeastOnce(separator)
                .Or(_returnEmptyEnumerable);
        
        /// <summary>
        /// Creates a parser which applies the current parser at least once, interleaved with a specified parser.
        /// The resulting parser ignores the return value of the separator parser.
        /// </summary>
        /// <typeparam name="U">The return type of the separator parser</typeparam>
        /// <param name="separator">A parser which parses a separator to be interleaved with the current parser</param>
        /// <returns>A parser which applies the current parser at least once, interleaved by <paramref name="separator"/></returns>
        public Parser<TToken, IEnumerable<T>> SeparatedAtLeastOnce<U>(Parser<TToken, U> separator)
            => new SeparatedAtLeastOnceParser<U>(this, separator);

        private sealed class SeparatedAtLeastOnceParser<U> : ManyParserBase
        {
            private readonly Parser<TToken, T> _parser;
            private readonly Parser<TToken, T> _remainderParser;

            public SeparatedAtLeastOnceParser(Parser<TToken, T> parser, Parser<TToken, U> separator) : base(parser.Expected)
            {
                _parser = parser;
                _remainderParser = separator.Then(parser);
            }

            internal override Result<TToken, IEnumerable<T>> Parse(IParseState<TToken> state)
            {
                var result = _parser.Parse(state);
                if (!result.Success)
                {
                    return Result.Failure<TToken, IEnumerable<T>>(
                        result.Error,
                        result.ConsumedInput
                    );
                }
                return ManyImpl(_remainderParser, state, new List<T> { result.Value }, result.ConsumedInput);
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
            => this.Before(separator).Many();
        
        /// <summary>
        /// Creates a parser which applies the current parser at least once, interleaved and terminated with a specified parser.
        /// The resulting parser ignores the return value of the separator parser.
        /// </summary>
        /// <typeparam name="U">The return type of the separator parser</typeparam>
        /// <param name="separator">A parser which parses a separator to be interleaved with the current parser</param>
        /// <returns>A parser which applies the current parser at least once, interleaved and terminated by <paramref name="separator"/></returns>
        public Parser<TToken, IEnumerable<T>> SeparatedAndTerminatedAtLeastOnce<U>(Parser<TToken, U> separator)
            => this.Before(separator).AtLeastOnce();

        /// <summary>
        /// Creates a parser which applies the current parser repeatedly, interleaved and optionally terminated with a specified parser.
        /// The resulting parser ignores the return value of the separator parser.
        /// </summary>
        /// <typeparam name="U">The return type of the separator parser</typeparam>
        /// <param name="separator">A parser which parses a separator to be interleaved with the current parser</param>
        /// <returns>A parser which applies the current parser repeatedly, interleaved and optionally terminated by <paramref name="separator"/></returns>
        public Parser<TToken, IEnumerable<T>> SeparatedAndOptionallyTerminated<U>(Parser<TToken, U> separator)
            => this.SeparatedAndOptionallyTerminatedAtLeastOnce(separator)
                .Or(_returnEmptyEnumerable);
        
        /// <summary>
        /// Creates a parser which applies the current parser at least once, interleaved and optionally terminated with a specified parser.
        /// The resulting parser ignores the return value of the separator parser.
        /// </summary>
        /// <typeparam name="U">The return type of the separator parser</typeparam>
        /// <param name="separator">A parser which parses a separator to be interleaved with the current parser</param>
        /// <returns>A parser which applies the current parser at least once, interleaved and optionally terminated by <paramref name="separator"/></returns>
        public Parser<TToken, IEnumerable<T>> SeparatedAndOptionallyTerminatedAtLeastOnce<U>(Parser<TToken, U> separator)
            => new SeparatedAndOptionallyTerminatedAtLeastOnceParser<U>(this, separator);

        private sealed class SeparatedAndOptionallyTerminatedAtLeastOnceParser<U> : Parser<TToken, IEnumerable<T>>
        {
            private readonly Parser<TToken, T> _parser;
            private readonly Parser<TToken, U> _separator;

            public SeparatedAndOptionallyTerminatedAtLeastOnceParser(Parser<TToken, T> parser, Parser<TToken, U> separator) : base(parser.Expected)
            {
                _parser = parser;
                _separator = separator;
            }

            internal override Result<TToken, IEnumerable<T>> Parse(IParseState<TToken> state)
            {
                var result = _parser.Parse(state);
                if (!result.Success)
                {
                    return Result.Failure<TToken, IEnumerable<T>>(result.Error, result.ConsumedInput);
                }
                var ts = new List<T> { result.Value };
                var consumedInput = result.ConsumedInput;

                while (true)
                {
                    var sepResult = _separator.Parse(state);
                    consumedInput = consumedInput || sepResult.ConsumedInput;
                    if (sepResult.ConsumedInput && !sepResult.Success)
                    {
                        return Result.Failure<TToken, IEnumerable<T>>(sepResult.Error, consumedInput);
                    }
                    if (!sepResult.ConsumedInput)
                    {
                        return Result.Success<TToken, IEnumerable<T>>(ts, consumedInput);
                    }

                    var itemResult = _parser.Parse(state);
                    consumedInput = consumedInput || itemResult.ConsumedInput;
                    if (itemResult.ConsumedInput && !itemResult.Success)
                    {
                        return Result.Failure<TToken, IEnumerable<T>>(sepResult.Error, consumedInput);
                    }
                    if (itemResult.Success)
                    {
                        ts.Add(itemResult.GetValueOrDefault());
                    }
                    if (!itemResult.ConsumedInput)
                    {
                        return Result.Success<TToken, IEnumerable<T>>(ts, consumedInput);
                    }
                }
            }
        }
    }
}