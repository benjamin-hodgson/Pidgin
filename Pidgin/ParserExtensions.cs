using System;
using System.Collections.Generic;
using System.IO;
using Pidgin.ParseStates;

namespace Pidgin
{
    /// <summary>
    /// Extension methods for running parsers
    /// </summary>
    public static class ParserExtensions
    {
        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input string</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static Result<char, T> Parse<T>(this Parser<char, T> parser, string input, Func<char, SourcePos, SourcePos> calculatePos = null)
            => DoParse(parser, new StringParseState(input, calculatePos ?? Parser.DefaultCharPosCalculator));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input list</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static Result<TToken, T> Parse<TToken, T>(this Parser<TToken, T> parser, IList<TToken> input, Func<TToken, SourcePos, SourcePos> calculatePos = null)
            => DoParse(parser, new ListParseState<TToken>(input, calculatePos ?? Parser.GetDefaultPosCalculator<TToken>()));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input list</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static Result<TToken, T> ParseReadOnlyList<TToken, T>(this Parser<TToken, T> parser, IReadOnlyList<TToken> input, Func<TToken, SourcePos, SourcePos> calculatePos = null)
            => DoParse(parser, new ReadOnlyListParseState<TToken>(input, calculatePos ?? Parser.GetDefaultPosCalculator<TToken>()));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input enumerable</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static Result<TToken, T> Parse<TToken, T>(this Parser<TToken, T> parser, IEnumerable<TToken> input, Func<TToken, SourcePos, SourcePos> calculatePos = null)
        {
            using (var e = input.GetEnumerator())
            {
                return Parse(parser, e, calculatePos);
            }
        } 

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input enumerator</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static Result<TToken, T> Parse<TToken, T>(this Parser<TToken, T> parser, IEnumerator<TToken> input, Func<TToken, SourcePos, SourcePos> calculatePos = null)
            => DoParse(parser, new EnumeratorParseState<TToken>(input, calculatePos ?? Parser.GetDefaultPosCalculator<TToken>()));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input stream</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static Result<byte, T> Parse<T>(this Parser<byte, T> parser, Stream input, Func<byte, SourcePos, SourcePos> calculatePos = null)
            => DoParse(parser, new StreamParseState(input, calculatePos ?? Parser.DefaultBytePosCalculator));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input reader</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static Result<char, T> Parse<T>(this Parser<char, T> parser, TextReader input, Func<char, SourcePos, SourcePos> calculatePos = null)
            => DoParse(parser, new ReaderParseState(input, calculatePos ?? Parser.DefaultCharPosCalculator));
        
        private static Result<TToken, T> DoParse<TToken, T>(Parser<TToken, T> parser, IParseState<TToken> state)
        {
            using (state)  // ensure we return the state's buffer to the buffer pool
            {
                state.Advance();  // pull the first element from the input
                return parser.Parse(state);
            }
        }


        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input string</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static T ParseOrThrow<T>(this Parser<char, T> parser, string input, Func<char, SourcePos, SourcePos> calculatePos = null)
            => GetValueOrThrow(parser.Parse(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input list</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static T ParseOrThrow<TToken, T>(this Parser<TToken, T> parser, IList<TToken> input, Func<TToken, SourcePos, SourcePos> calculatePos = null)
            => GetValueOrThrow(parser.Parse(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input list</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static T ParseReadOnlyListOrThrow<TToken, T>(this Parser<TToken, T> parser, IReadOnlyList<TToken> input, Func<TToken, SourcePos, SourcePos> calculatePos = null)
            => GetValueOrThrow(parser.ParseReadOnlyList(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input enumerable</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static T ParseOrThrow<TToken, T>(this Parser<TToken, T> parser, IEnumerable<TToken> input, Func<TToken, SourcePos, SourcePos> calculatePos = null)
            => GetValueOrThrow(parser.Parse(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input enumerator</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static T ParseOrThrow<TToken, T>(this Parser<TToken, T> parser, IEnumerator<TToken> input, Func<TToken, SourcePos, SourcePos> calculatePos = null)
            => GetValueOrThrow(parser.Parse(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input stream</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static T ParseOrThrow<T>(this Parser<byte, T> parser, Stream input, Func<byte, SourcePos, SourcePos> calculatePos = null)
            => GetValueOrThrow(parser.Parse(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input reader</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static T ParseOrThrow<T>(this Parser<char, T> parser, TextReader input, Func<char, SourcePos, SourcePos> calculatePos = null)
            => GetValueOrThrow(parser.Parse(input, calculatePos));

        private static T GetValueOrThrow<TToken, T>(Result<TToken, T> result)
            => result.Success ? result.Value : throw new ParseException(result.Error.RenderErrorMessage());
    }
}