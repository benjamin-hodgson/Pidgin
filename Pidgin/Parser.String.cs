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
            => Parser<char>.Sequence<string>(str);
    }
}