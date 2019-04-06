using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using LExpression = System.Linq.Expressions.Expression;

namespace Pidgin
{
    public static partial class Parser
    {
        internal sealed class SequenceTokenParserFast<TToken, TEnumerable> : Parser<TToken, TEnumerable>
            where TToken : struct, IEquatable<TToken>
            where TEnumerable : IEnumerable<TToken>
        {
            private readonly TEnumerable _value;
            private readonly ImmutableArray<TToken> _valueTokens;

            public SequenceTokenParserFast(TEnumerable value)
            {
                _value = value;
                _valueTokens = value.ToImmutableArray();
            }

            internal sealed override InternalResult<TEnumerable> Parse(ref ParseState<TToken> state)
            {
                var span = state.Peek(_valueTokens.Length);  // span.Length <= _valueTokens.Length
                
                var errorPos = -1;
                for (var i = 0; i < span.Length; i++)
                {
                    if (!span[i].Equals(_valueTokens[i]))
                    {
                        errorPos = i;
                        break;
                    }
                }

                if (errorPos != -1)
                {
                    // strings didn't match
                    state.Advance(errorPos);
                    state.Error = new InternalError<TToken>(
                        Maybe.Just(span[errorPos]),
                        false,
                        state.SourcePos,
                        null
                    );
                    state.AddExpected(new Expected<TToken>(_valueTokens));
                    return InternalResult.Failure<TEnumerable>(errorPos > 0);
                }

                if (span.Length < _valueTokens.Length)
                {
                    // strings matched but reached EOF
                    state.Advance(span.Length);
                    state.Error = new InternalError<TToken>(
                        Maybe.Nothing<TToken>(),
                        true,
                        state.SourcePos,
                        null
                    );
                    state.AddExpected(new Expected<TToken>(_valueTokens));
                    return InternalResult.Failure<TEnumerable>(span.Length > 0);
                }

                // OK
                state.Advance(_valueTokens.Length);
                return InternalResult.Success<TEnumerable>(_value, _valueTokens.Length > 0);
            }
        }

        internal sealed class SequenceTokenParserSlow<TToken, TEnumerable> : Parser<TToken, TEnumerable>
            where TEnumerable : IEnumerable<TToken>
        {
            private readonly TEnumerable _value;
            private readonly ImmutableArray<TToken> _valueTokens;

            public SequenceTokenParserSlow(TEnumerable value)
            {
                _value = value;
                _valueTokens = value.ToImmutableArray();
            }

            internal sealed override InternalResult<TEnumerable> Parse(ref ParseState<TToken> state)
            {
                var span = state.Peek(_valueTokens.Length);  // span.Length <= _valueTokens.Length
                
                var errorPos = -1;
                for (var i = 0; i < span.Length; i++)
                {
                    if (!EqualityComparer<TToken>.Default.Equals(span[i], _valueTokens[i]))
                    {
                        errorPos = i;
                        break;
                    }
                }

                if (errorPos != -1)
                {
                    // strings didn't match
                    state.Advance(errorPos);
                    state.Error = new InternalError<TToken>(
                        Maybe.Just(span[errorPos]),
                        false,
                        state.SourcePos,
                        null
                    );
                    state.AddExpected(new Expected<TToken>(_valueTokens));
                    return InternalResult.Failure<TEnumerable>(errorPos > 0);
                }

                if (span.Length < _valueTokens.Length)
                {
                    // strings matched but reached EOF
                    state.Advance(span.Length);
                    state.Error = new InternalError<TToken>(
                        Maybe.Nothing<TToken>(),
                        true,
                        state.SourcePos,
                        null
                    );
                    state.AddExpected(new Expected<TToken>(_valueTokens));
                    return InternalResult.Failure<TEnumerable>(span.Length > 0);
                }

                // OK
                state.Advance(_valueTokens.Length);
                return InternalResult.Success<TEnumerable>(_value, _valueTokens.Length > 0);
            }
        }
    }

    public static partial class Parser<TToken>
    {
        /// <summary>
        /// Creates a parser that parses and returns a literal sequence of tokens
        /// </summary>
        /// <param name="tokens">A sequence of tokens</param>
        /// <returns>A parser that parses a literal sequence of tokens</returns>
        public static Parser<TToken, TToken[]> Sequence(params TToken[] tokens)
        {
            if (tokens == null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }
            return Sequence<TToken[]>(tokens);
        }
        /// <summary>
        /// Creates a parser that parses and returns a literal sequence of tokens.
        /// The input enumerable is enumerated and copied to a list.
        /// </summary>
        /// <typeparam name="TEnumerable">The type of tokens to parse</typeparam>
        /// <param name="tokens">A sequence of tokens</param>
        /// <returns>A parser that parses a literal sequence of tokens</returns>
        public static Parser<TToken, TEnumerable> Sequence<TEnumerable>(TEnumerable tokens)
            where TEnumerable : IEnumerable<TToken>
        {
            if (tokens == null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }
            return FastSequenceParser<TEnumerable>.Create(tokens);
        }

        private static class FastSequenceParser<TEnumerable>
            where TEnumerable : IEnumerable<TToken>
        {
            private static readonly Func<TEnumerable, Parser<TToken, TEnumerable>> _createParser;

            public static Parser<TToken, TEnumerable> Create(TEnumerable tokens)
            {
                if (_createParser != null)
                {
                    return _createParser(tokens);
                }
                return new Parser.SequenceTokenParserSlow<TToken, TEnumerable>(tokens);
            }

            static FastSequenceParser()
            {
                var ttoken = typeof(TToken).GetTypeInfo();
                var comparable = typeof(IComparable<TToken>).GetTypeInfo();
                if (ttoken.IsValueType && comparable.IsAssignableFrom(ttoken))
                {
                    var ctor = typeof(Parser.SequenceTokenParserFast<,>)
                        .MakeGenericType(typeof(TToken), typeof(TEnumerable))
                        .GetTypeInfo()
                        .DeclaredConstructors
                        .Single();
                    var param = LExpression.Parameter(typeof(TEnumerable));
                    var create = LExpression.New(ctor, param);
                    _createParser = LExpression.Lambda<Func<TEnumerable, Parser<TToken, TEnumerable>>>(create, param).Compile();
                }
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
        {
            return Sequence(parsers.AsEnumerable());
        }

        /// <summary>
        /// Creates a parser that applies a sequence of parsers and collects the results.
        /// This parser fails if any of its constituent parsers fail
        /// </summary>
        /// <typeparam name="T">The return type of the parsers</typeparam>
        /// <param name="parsers">A sequence of parsers</param>
        /// <returns>A parser that applies a sequence of parsers and collects the results</returns>
        public static Parser<TToken, IEnumerable<T>> Sequence<T>(IEnumerable<Parser<TToken, T>> parsers)
        {
            if (parsers == null)
            {
                throw new ArgumentNullException(nameof(parsers));
            }
            var parsersArray = parsers.ToArray();
            if (parsersArray.Length == 1)
            {
                return parsersArray[0].Select(x => new[] { x }.AsEnumerable());
            }
            return new SequenceParser<T>(parsersArray);
        }

        private sealed class SequenceParser<T> : Parser<TToken, IEnumerable<T>>
        {
            private readonly Parser<TToken, T>[] _parsers;

            public SequenceParser(Parser<TToken, T>[] parsers)
            {
                _parsers = parsers;
            }

            internal sealed override InternalResult<IEnumerable<T>> Parse(ref ParseState<TToken> state)
            {
                var consumedInput = false;
                var ts = new T[_parsers.Length];
                
                for (var i = 0; i < _parsers.Length; i++)
                {
                    var p = _parsers[i];
                
                    var result = p.Parse(ref state);
                    consumedInput = consumedInput || result.ConsumedInput;
                
                    if (!result.Success)
                    {
                        return InternalResult.Failure<IEnumerable<T>>(consumedInput);
                    }
                
                    ts[i] = result.Value;
                }

                return InternalResult.Success<IEnumerable<T>>(ts, consumedInput);
            }
        }
    }
}