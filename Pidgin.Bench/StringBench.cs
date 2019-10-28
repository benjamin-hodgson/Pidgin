using BenchmarkDotNet.Attributes;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Pidgin.Bench
{
    [MemoryDiagnoser]
    public class StringBench
    {
        private static readonly string _input = new string('a', 65536);
        private static readonly Parser<char, string> _string = Parser.String(_input);
        private static readonly Parser<char, string> _cistring = Parser.CIString(_input);

        private static readonly string _whitespace = new string(' ', 65536);

        [Benchmark]
        public void String()
        {
            _string.ParseOrThrow(_input);
        }
        [Benchmark]
        public void CIString()
        {
            _cistring.ParseOrThrow(_input);
        }

        [Benchmark]
        public void SkipWhitespaces()
        {
            Parser.SkipWhitespaces.ParseOrThrow(_whitespace);
        }
    }
}