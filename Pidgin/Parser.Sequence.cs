using System.Collections.Generic;
using System.Linq;
using Pidgin.ParseStates;

namespace Pidgin
{
    public static partial class Parser<TToken>
    {
        /// <summary>
        /// Creates a parser that parses and returns a literal sequence of tokens
        /// </summary>
        /// <param name="tokens">A sequence of tokens</param>
        /// <returns>A parser that parses a literal sequence of tokens</returns>
        public static Parser<TToken, TToken[]> Sequence(params TToken[] tokens)
            => Sequence<TToken[]>(tokens);
        /// <summary>
        /// Creates a parser that parses and returns a literal sequence of tokens.
        /// The input enumerable is enumerated and copied to a list.
        /// </summary>
        /// <typeparam name="TEnumerable">The type of tokens to parse</typeparam>
        /// <param name="tokens">A sequence of tokens</param>
        /// <returns>A parser that parses a literal sequence of tokens</returns>
        public static Parser<TToken, TEnumerable> Sequence<TEnumerable>(TEnumerable tokens)
            where TEnumerable : IEnumerable<TToken>
            => new SequenceTokenParser<TEnumerable>(tokens);

        private sealed class SequenceTokenParser<TEnumerable> : Parser<TToken, TEnumerable>
            where TEnumerable : IEnumerable<TToken>
        {
            private readonly TEnumerable _value;
            private readonly TToken[] _valueTokens;

            public SequenceTokenParser(TEnumerable value)
                : base(new SortedSet<Expected<TToken>>{ new Expected<TToken>(value) })
            {
                _value = value;
                _valueTokens = value.ToArray();
            }

            internal sealed override Result<TToken, TEnumerable> Parse(IParseState<TToken> state)
            {
                var consumedInput = false;
                foreach (var x in _valueTokens)
                {
                    var result = state.Peek();
                    if (!result.HasValue)
                    {
                        return Result.Failure<TToken, TEnumerable>(
                            new ParseError<TToken>(
                                result,
                                true,
                                Expected,
                                state.SourcePos,
                                null
                            ),
                            consumedInput
                        );
                    }

                    TToken token = result.GetValueOrDefault();
                    if (!token.Equals(x))
                    {
                        return Result.Failure<TToken, TEnumerable>(
                            new ParseError<TToken>(
                                result,
                                false,
                                Expected,
                                state.SourcePos,
                                null
                            ),
                            consumedInput
                        );
                    }

                    consumedInput = true;
                    state.Advance();
                }
                return Result.Success<TToken, TEnumerable>(_value, consumedInput);
            }
        }

        /// <summary>
        /// Creates a parser that applies a sequence of parsers and collects the results.
        /// This parser fails if any of its constituent parsers fail
        /// </summary>
        /// <typeparam name="T">The return type of the parsers</typeparam>
        /// <param name="parsers">A sequence of parsers</param>
        /// <returns>A parser that applies a sequence of parsers and collects the results</returns>
        public static Parser<TToken, IEnumerable<T>> Sequence<T>(params Parser<TToken, T>[] parsers)
            => Sequence(parsers.AsEnumerable());

        /// <summary>
        /// Creates a parser that applies a sequence of parsers and collects the results.
        /// This parser fails if any of its constituent parsers fail
        /// </summary>
        /// <typeparam name="T">The return type of the parsers</typeparam>
        /// <param name="parsers">A sequence of parsers</param>
        /// <returns>A parser that applies a sequence of parsers and collects the results</returns>
        public static Parser<TToken, IEnumerable<T>> Sequence<T>(IEnumerable<Parser<TToken, T>> parsers)
            => new SequenceParser<T>(parsers);

        private sealed class SequenceParser<T> : Parser<TToken, IEnumerable<T>>
        {
            private readonly Parser<TToken, T>[] _parsers;

            public SequenceParser(IEnumerable<Parser<TToken, T>> parsers) : base(ExpectedUtil.Concat(parsers.Select(p => p.Expected)))
            {
                _parsers = parsers.ToArray();
            }

            internal sealed override Result<TToken, IEnumerable<T>> Parse(IParseState<TToken> state)
            {
                var consumedInput = false;
                var ts = new T[_parsers.Length];
                var i = 0;
                foreach (var p in _parsers)
                {
                    var result = p.Parse(state);
                    consumedInput = consumedInput || result.ConsumedInput;
                    if (!result.Success)
                    {
                        return Result.Failure<TToken, IEnumerable<T>>(
                            result.Error.WithExpected(Expected),
                            consumedInput
                        );
                    }
                    ts[i] = result.GetValueOrDefault();
                    i++;
                }
                return Result.Success<TToken, IEnumerable<T>>(ts, consumedInput);
            }
        }
    }
}