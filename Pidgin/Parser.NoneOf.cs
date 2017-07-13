using System.Collections.Generic;
using System.Linq;
using Pidgin.ParseStates;

namespace Pidgin
{
    public static partial class Parser
    {
        /// <summary>
        /// Creates a parser which parses and returns a character as long as it does not match one of the specified characters.
        /// </summary>
        /// <param name="chars">A sequence of characters that should not be matched</param>
        /// <returns>A parser which parses and returns a character that does not match one of the specified characters</returns>
        public static Parser<char, char> NoneOf(params char[] chars)
            => Not(OneOf((IEnumerable<char>)chars)).Then(Parser<char>.Token(c => true));

        /// <summary>
        /// Creates a parser which parses and returns a character as long as it does not match one of the specified characters.
        /// </summary>
        /// <param name="chars">A sequence of characters that should not be matched</param>
        /// <returns>A parser which parses and returns a character that does not match one of the specified characters</returns>
        public static Parser<char, char> NoneOf(IEnumerable<char> chars)
            => Not(OneOf(chars.Select(c => Char(c)))).Then(Parser<char>.Token(c => true));
    }
}