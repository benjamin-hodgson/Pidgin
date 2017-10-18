using System;
using System.Collections.Generic;
using System.Linq;
using Pidgin.ParseStates;

namespace Pidgin
{
    public static partial class Parser
    {
        /// <summary>
        /// Creates a parser which applies <paramref name="parser"/> <paramref name="count"/> times,
        /// packing the resulting <see cref="char"/>s into a <see cref="string"/>.
        /// 
        /// <para>
        /// Equivalent to <c>parser.Repeat(count).Select(string.Concat)</c>.
        /// </para>
        /// </summary>
        /// <typeparam name="TToken">The type of tokens in the parser's input stream</typeparam>
        /// <param name="parser">The parser</param>
        /// <param name="count">The number of times to apply the parser</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> was less than 0</exception>
        /// <returns>
        /// A parser which applies <paramref name="parser"/> <paramref name="count"/> times,
        /// packing the resulting <see cref="char"/>s into a <see cref="string"/>.
        /// </returns>
        public static Parser<TToken, string> RepeatString<TToken>(this Parser<TToken, char> parser, int count)
        {
            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");
            }
            return new RepeatStringParser<TToken>(parser, count);
        }

        private sealed class RepeatStringParser<TToken> : Parser<TToken, string>
        {
            private readonly Parser<TToken, char> _parser;
            private readonly int _count;

            public RepeatStringParser(Parser<TToken, char> parser, int count) : base(ExpectedUtil.Concat(Enumerable.Repeat(parser.Expected, count)))
            {
                _parser = parser;
                _count = count;
            }

            internal override InternalResult<string> Parse(IParseState<TToken> state)
            {
                var consumedInput = false;
                var builder = new PooledStringBuilder(_count);

                for (var _ = 0; _ < _count; _++)
                {
                    var result = _parser.Parse(state);
                    consumedInput = consumedInput || result.ConsumedInput;

                    if (!result.Success)
                    {
                        state.Error = state.Error.WithExpected(Expected);
                        return InternalResult.Failure<string>(consumedInput);
                    }

                    builder.Append(result.Value);
                }

                return InternalResult.Success(builder.GetStringAndClear(), consumedInput);
            }
        }
    }

    public abstract partial class Parser<TToken, T>
    {
        /// <summary>
        /// Creates a parser which applies the current parser <paramref name="count"/> times.
        /// </summary>
        /// <param name="count">The number of times to apply the current parser</param>
        /// <exception cref="System.InvalidOperationException"><paramref name="count"/> is less than 0</exception>
        /// <returns>A parser which applies the current parser <paramref name="count"/> times.</returns>
        public Parser<TToken, IEnumerable<T>> Repeat(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");
            }
            return Parser<TToken>.Sequence(Enumerable.Repeat(this, count));
        }
    }
}