using System;
using System.Collections.Generic;
using System.Linq;

namespace Pidgin
{
    public static partial class Parser
    {
        /// <summary>
        /// Creates a parser that parses and returns a literal string
        /// </summary>
        /// <param name="str">The string to parse</param>
        /// <returns>A parser that parses and returns a literal string</returns>
        public static Parser<char, string> String(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }
            return Parser<char>.Sequence<string>(str);
        }

        /// <summary>
        /// Creates a parser that parses and returns a literal string, in a case insensitive manner.
        /// The parser returns the actual string parsed.
        /// </summary>
        /// <param name="str">The string to parse</param>
        /// <returns>A parser that parses and returns a literal string, in a case insensitive manner.</returns>
        public static Parser<char, string> CIString(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }
            return Parser<char>.Sequence(str.Select(CIChar))
                .Select(string.Concat)
                .WithExpected(new SortedSet<Expected<char>> { new Expected<char>(str.ToCharArray()) });
        }
    }
}