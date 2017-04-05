using static Pidgin.Parser<char>;

namespace Pidgin
{
    public static partial class Parser
    {
        /// <summary>
        /// Creates a parser which parses and returns a single character.
        /// </summary>
        /// <param name="character">The character to parse</param>
        /// <returns>A parser which parses and returns a single character</returns>
        public static Parser<char, char> Char(char character) => Token(character);

        /// <summary>
        /// A parser that parses and returns a single digit character (0-9)
        /// </summary>
        /// <returns>A parser that parses and returns a single digit character</returns>
        public static Parser<char, char> Digit { get; } = Token(c => char.IsDigit(c)).Labelled("digit");

        /// <summary>
        /// A parser that parses and returns a single letter character
        /// </summary>
        /// <returns>A parser that parses and returns a single letter character</returns>
        public static Parser<char, char> Letter { get; } = Token(c => char.IsLetter(c)).Labelled("letter");

        /// <summary>
        /// A parser that parses and returns a single letter or digit character
        /// </summary>
        /// <returns>A parser that parses and returns a single letter or digit character</returns>
        public static Parser<char, char> LetterOrDigit { get; } = Token(c => char.IsLetterOrDigit(c)).Labelled("letter or digit");

        /// <summary>
        /// A parser that parses and returns a single lowercase letter character
        /// </summary>
        /// <returns>A parser that parses and returns a single lowercase letter character</returns>
        public static Parser<char, char> Lowercase { get; } = Token(c => char.IsLower(c)).Labelled("lowercase letter");

        /// <summary>
        /// A parser that parses and returns a single uppercase letter character
        /// </summary>
        /// <returns>A parser that parses and returns a single uppercase letter character</returns>
        public static Parser<char, char> Uppercase { get; } = Token(c => char.IsUpper(c)).Labelled("uppercase letter");

        /// <summary>
        /// A parser that parses and returns a single Unicode punctuation character
        /// </summary>
        /// <returns>A parser that parses and returns a single Unicode punctuation character</returns>
        public static Parser<char, char> Punctuation { get; } = Token(c => char.IsPunctuation(c)).Labelled("punctuation");

        /// <summary>
        /// A parser that parses and returns a single Unicode symbol character
        /// </summary>
        /// <returns>A parser that parses and returns a single Unicode symbol character</returns>
        public static Parser<char, char> Symbol { get; } = Token(c => char.IsSymbol(c)).Labelled("symbol");

        /// <summary>
        /// A parser that parses and returns a single Unicode separator character
        /// </summary>
        /// <returns>A parser that parses and returns a single Unicode separator character</returns>
        public static Parser<char, char> Separator { get; } = Token(c => char.IsSeparator(c)).Labelled("separator");
    }
}