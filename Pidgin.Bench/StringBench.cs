using BenchmarkDotNet.Attributes;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Pidgin.Bench
{
    public class StringBench
    {
        private static readonly string _input = new string('a', 65536);
        private static readonly Parser<char, string> _parser = String(_input);

        [Benchmark(Baseline = true)]
        public void Run()
        {
            _parser.ParseOrThrow(_input);
        }
    }
}