using System.Collections.Generic;
using static Pidgin.Parser<char>;

namespace Pidgin
{
    public static partial class Parser
    {
        /// <summary>
        /// A parser that parses and returns a single whitespace character
        /// </summary>
        /// <returns>A parser that parses and returns a single whitespace character</returns>
        public static Parser<char, char> Whitespace { get; }
            = Token(char.IsWhiteSpace).Labelled("whitespace");
        /// <summary>
        /// A parser that parses and returns a sequence of whitespace characters
        /// </summary>
        /// <returns>A parser that parses and returns a sequence of whitespace characters</returns>
        public static Parser<char, IEnumerable<char>> Whitespaces { get; }
            = Whitespace.Many().Labelled("whitespace");
        /// <summary>
        /// A parser that parses and returns a sequence of whitespace characters packed into a string
        /// </summary>
        /// <returns>A parser that parses and returns a sequence of whitespace characters packed into a string</returns>
        public static Parser<char, string> WhitespaceString { get; }
            = Whitespace.ManyString().Labelled("whitespace");
        /// <summary>
        /// A parser that discards a sequence of whitespace characters
        /// </summary>
        /// <returns>A parser that discards a sequence of whitespace characters</returns>
        public static Parser<char, Unit> SkipWhitespaces { get; }
            = Whitespace.SkipMany().Labelled("whitespace");
    }
}