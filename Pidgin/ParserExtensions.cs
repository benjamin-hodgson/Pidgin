using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Pidgin.TokenStreams;

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
        public static Result<char, T> Parse<T>(this Parser<char, T> parser, string input, Func<char, SourcePos, SourcePos>? calculatePos = null)
            => Parse(parser, input.AsSpan(), calculatePos ?? Parser.DefaultCharPosCalculator);

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input list</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static Result<TToken, T> Parse<TToken, T>(this Parser<TToken, T> parser, IList<TToken> input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
            => DoParse(parser, new ListTokenStream<TToken>(input), calculatePos ?? Parser.GetDefaultPosCalculator<TToken>());

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input list</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static Result<TToken, T> ParseReadOnlyList<TToken, T>(this Parser<TToken, T> parser, IReadOnlyList<TToken> input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
            => DoParse(parser, new ReadOnlyListTokenStream<TToken>(input), calculatePos ?? Parser.GetDefaultPosCalculator<TToken>());

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input enumerable</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static Result<TToken, T> Parse<TToken, T>(this Parser<TToken, T> parser, IEnumerable<TToken> input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
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
        public static Result<TToken, T> Parse<TToken, T>(this Parser<TToken, T> parser, IEnumerator<TToken> input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
            => DoParse(parser, new EnumeratorTokenStream<TToken>(input), calculatePos ?? Parser.GetDefaultPosCalculator<TToken>());

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>.
        /// Note that more characters may be consumed from <paramref name="input"/> than were required for parsing.
        /// You may need to manually rewind <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input stream</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static Result<byte, T> Parse<T>(this Parser<byte, T> parser, Stream input, Func<byte, SourcePos, SourcePos>? calculatePos = null)
            => DoParse(parser, new StreamTokenStream(input), calculatePos ?? Parser.DefaultBytePosCalculator);

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input reader</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static Result<char, T> Parse<T>(this Parser<char, T> parser, TextReader input, Func<char, SourcePos, SourcePos>? calculatePos = null)
            => DoParse(parser, new ReaderTokenStream(input), calculatePos ?? Parser.DefaultCharPosCalculator);

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input array</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static Result<TToken, T> Parse<TToken, T>(this Parser<TToken, T> parser, TToken[] input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
            => parser.Parse(input.AsSpan(), calculatePos);
        
        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input span</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static Result<TToken, T> Parse<TToken, T>(this Parser<TToken, T> parser, ReadOnlySpan<TToken> input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
        {
            var state = new ParseState<TToken>(calculatePos ?? Parser.GetDefaultPosCalculator<TToken>(), input);
            var result = DoParse(parser, ref state);
            KeepAlive(ref input);
            return result;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void KeepAlive<TToken>(ref ReadOnlySpan<TToken> span) {}

        private static Result<TToken, T> DoParse<TToken, T>(Parser<TToken, T> parser, ITokenStream<TToken> stream, Func<TToken, SourcePos, SourcePos> calculatePos)
        {
            var state = new ParseState<TToken>(calculatePos, stream);
            return DoParse(parser, ref state);
        }
        private static Result<TToken, T> DoParse<TToken, T>(Parser<TToken, T> parser, ref ParseState<TToken> state)
        {
            var startingLoc = state.Location;
            var expecteds = new ExpectedCollector<TToken>();

            var result1 = parser.TryParse(ref state, ref expecteds, out var result)
                ? new Result<TToken, T>(state.Location > startingLoc, result)
                : new Result<TToken, T>(state.Location > startingLoc, state.BuildError(ref expecteds));

            expecteds.Dispose();
            state.Dispose();  // ensure we return the state's buffers to the buffer pool

            return result1;
        }


        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input string</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static T ParseOrThrow<T>(this Parser<char, T> parser, string input, Func<char, SourcePos, SourcePos>? calculatePos = null)
            => GetValueOrThrow(parser.Parse(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input list</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static T ParseOrThrow<TToken, T>(this Parser<TToken, T> parser, IList<TToken> input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
            => GetValueOrThrow(parser.Parse(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input list</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static T ParseReadOnlyListOrThrow<TToken, T>(this Parser<TToken, T> parser, IReadOnlyList<TToken> input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
            => GetValueOrThrow(parser.ParseReadOnlyList(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input enumerable</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static T ParseOrThrow<TToken, T>(this Parser<TToken, T> parser, IEnumerable<TToken> input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
            => GetValueOrThrow(parser.Parse(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input enumerator</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static T ParseOrThrow<TToken, T>(this Parser<TToken, T> parser, IEnumerator<TToken> input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
            => GetValueOrThrow(parser.Parse(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input stream</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static T ParseOrThrow<T>(this Parser<byte, T> parser, Stream input, Func<byte, SourcePos, SourcePos>? calculatePos = null)
            => GetValueOrThrow(parser.Parse(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input reader</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static T ParseOrThrow<T>(this Parser<char, T> parser, TextReader input, Func<char, SourcePos, SourcePos>? calculatePos = null)
            => GetValueOrThrow(parser.Parse(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input array</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static T ParseOrThrow<TToken, T>(this Parser<TToken, T> parser, TToken[] input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
            => GetValueOrThrow(parser.Parse(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input span</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static T ParseOrThrow<TToken, T>(this Parser<TToken, T> parser, ReadOnlySpan<TToken> input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
            => GetValueOrThrow(parser.Parse(input, calculatePos));

        private static T GetValueOrThrow<TToken, T>(Result<TToken, T> result)
            => result.Success ? result.Value : throw new ParseException(result.Error!.RenderErrorMessage());
    }
}