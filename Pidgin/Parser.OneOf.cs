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
            => OneOf((IEnumerable<char>)chars);

        /// <summary>
        /// Creates a parser which parses and returns one of the specified characters.
        /// </summary>
        /// <param name="chars">A sequence of characters to choose between</param>
        /// <returns>A parser which parses and returns one of the specified characters</returns>
        public static Parser<char, char> OneOf(IEnumerable<char> chars)
            => OneOf(chars.Select(c => Char(c)));

        /// <summary>
        /// Creates a parser which applies one of the specified parsers.
        /// The resulting parser fails if all of the input parsers fail without consuming input, or if one of them fails after consuming input
        /// </summary>
        /// <typeparam name="TToken">The type of tokens in the parsers' input stream</typeparam>
        /// <typeparam name="T">The return type of the parsers</typeparam>
        /// <param name="parsers">A sequence of parsers to choose between</param>
        /// <returns>A parser which applies one of the specified parsers</returns>
        public static Parser<TToken, T> OneOf<TToken, T>(params Parser<TToken, T>[] parsers)
            => OneOf((IEnumerable<Parser<TToken, T>>)parsers);

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
            => OneOfParser<TToken, T>.Create(parsers.ToList());


        private sealed class OneOfParser<TToken, T> : Parser<TToken, T>
        {
            private readonly List<Parser<TToken, T>> _parsers;

            private OneOfParser(List<Parser<TToken, T>> parsers)
                : base(ExpectedUtil.Union(parsers.Select(p => p.Expected)))
            {
                _parsers = parsers;
            }

            internal sealed override Result<TToken, T> Parse(IParseState<TToken> state)
            {
                var failureResult = Result.Failure<TToken, T>(
                    new ParseError<TToken>(Maybe.Nothing<TToken>(), false, Expected, state.SourcePos, "OneOf had no arguments"),
                    false
                );
                foreach (var p in _parsers)
                {
                    var thisResult = p.Parse(state);
                    if (thisResult.Success || thisResult.ConsumedInput)
                    {
                        return thisResult;
                    }
                    failureResult = LongestMatch(failureResult, thisResult);
                }
                return failureResult.WithExpected(Expected);
            }

            private static Result<TToken, T> LongestMatch(Result<TToken, T> result1, Result<TToken, T> result2)
                => result1.Error.ErrorPos > result2.Error.ErrorPos ? result1 : result2;

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
                return new OneOfParser<TToken, T>(list);
            }
        }
    }
}