using System;
using System.Collections.Generic;
using System.Linq;
using Pidgin.ParseStates;

namespace Pidgin
{
    public static partial class Parser
    {
        /// <summary>
        /// Creates a parser which parses and returns one of the specified characters.
        /// </summary>
        /// <param name="chars">A sequence of characters to choose between</param>
        /// <returns>A parser which parses and returns one of the specified characters</returns>
        public static Parser<char, char> OneOf(params char[] chars)
            => OneOf(chars.AsEnumerable());

        /// <summary>
        /// Creates a parser which parses and returns one of the specified characters.
        /// </summary>
        /// <param name="chars">A sequence of characters to choose between</param>
        /// <returns>A parser which parses and returns one of the specified characters</returns>
        public static Parser<char, char> OneOf(IEnumerable<char> chars)
        {
            var cs = chars.ToArray();
            return Parser<char>
                .Token(c => Array.IndexOf(cs, c) != -1)
                .WithExpected(new SortedSet<Expected<char>>(cs.Select(c => new Expected<char>(new[] { c }))));
        }

        /// <summary>
        /// Creates a parser which parses and returns one of the specified characters, in a case insensitive manner.
        /// The parser returns the actual character parsed.
        /// </summary>
        /// <param name="chars">A sequence of characters to choose between</param>
        /// <returns>A parser which parses and returns one of the specified characters, in a case insensitive manner.</returns>
        public static Parser<char, char> CIOneOf(params char[] chars)
            => CIOneOf(chars.AsEnumerable());

        /// <summary>
        /// Creates a parser which parses and returns one of the specified characters, in a case insensitive manner.
        /// The parser returns the actual character parsed.
        /// </summary>
        /// <param name="chars">A sequence of characters to choose between</param>
        /// <returns>A parser which parses and returns one of the specified characters, in a case insensitive manner.</returns>
        public static Parser<char, char> CIOneOf(IEnumerable<char> chars)
        {
            var cs = chars.Select(char.ToLowerInvariant).ToArray();
            var expected = cs.Select(c => new Expected<char>(new[] { char.ToLowerInvariant(c) }))
                .Concat(cs.Select(c => new Expected<char>(new[] { char.ToUpperInvariant(c) })));
            return Parser<char>
                .Token(c => Array.IndexOf(cs, char.ToLowerInvariant(c)) != -1)
                .WithExpected(new SortedSet<Expected<char>>(expected));
        }

        /// <summary>
        /// Creates a parser which applies one of the specified parsers.
        /// The resulting parser fails if all of the input parsers fail without consuming input, or if one of them fails after consuming input
        /// </summary>
        /// <typeparam name="TToken">The type of tokens in the parsers' input stream</typeparam>
        /// <typeparam name="T">The return type of the parsers</typeparam>
        /// <param name="parsers">A sequence of parsers to choose between</param>
        /// <returns>A parser which applies one of the specified parsers</returns>
        public static Parser<TToken, T> OneOf<TToken, T>(params Parser<TToken, T>[] parsers)
            => OneOf(parsers.AsEnumerable());

        /// <summary>
        /// Creates a parser which applies one of the specified parsers.
        /// The resulting parser fails if all of the input parsers fail without consuming input, or if one of them fails after consuming input.
        /// The input enumerable is enumerated and copied to a list.
        /// </summary>
        /// <typeparam name="TToken">The type of tokens in the parsers' input stream</typeparam>
        /// <typeparam name="T">The return type of the parsers</typeparam>
        /// <param name="parsers">A sequence of parsers to choose between</param>
        /// <returns>A parser which applies one of the specified parsers</returns>
        public static Parser<TToken, T> OneOf<TToken, T>(IEnumerable<Parser<TToken, T>> parsers)
            => OneOfParser<TToken, T>.Create(parsers);


        private sealed class OneOfParser<TToken, T> : Parser<TToken, T>
        {
            private readonly Parser<TToken, T>[] _parsers;

            private OneOfParser(Parser<TToken, T>[] parsers)
                : base(ExpectedUtil.Union(parsers.Select(p => p.Expected)))
            {
                _parsers = parsers;
            }

            internal sealed override InternalResult<T> Parse(IParseState<TToken> state)
            {
                var err = new ParseError<TToken>(
                    Maybe.Nothing<TToken>(),
                    false,
                    Expected,
                    state.SourcePos,
                    "OneOf had no arguments"
                );
                InternalResult<T> failureResult = InternalResult.Failure<T>(false);
                foreach (var p in _parsers)
                {
                    var thisResult = p.Parse(state);
                    if (thisResult.Success || thisResult.ConsumedInput)
                    {
                        return thisResult;
                    }
                    if (state.Error.ErrorPos > err.ErrorPos)
                    {
                        failureResult = thisResult;
                        err = state.Error;
                    }
                }
                state.Error = err.WithExpected(Expected);
                return failureResult;
            }

            internal static OneOfParser<TToken, T> Create(IEnumerable<Parser<TToken, T>> parsers)
            {
                // if we know the length of the collection,
                // we know we're going to need at least that much room in the list
                var list = parsers is ICollection<Parser<TToken, T>> coll
                    ? new List<Parser<TToken, T>>(coll.Count)
                    : new List<Parser<TToken, T>>();
                
                foreach (var p in parsers)
                {
                    if (p is OneOfParser<TToken, T> o)
                    {
                        list.AddRange(o._parsers);
                    }
                    else
                    {
                        list.Add(p);
                    }
                }
                return new OneOfParser<TToken, T>(list.ToArray());
            }
        }
    }
}