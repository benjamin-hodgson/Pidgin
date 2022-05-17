using System.Diagnostics.CodeAnalysis;

namespace Pidgin
{
    public static partial class Parser<TToken>
    {
        /// <summary>
        /// Creates a parser that parses any single character
        /// </summary>
        /// <returns>A parser that parses any single character</returns>
        [SuppressMessage("design", "CA1000")]  // "Do not declare static members on generic types"
        public static Parser<TToken, TToken> Any { get; }
            = Token(_ => true).Labelled("any character");
    }
}
