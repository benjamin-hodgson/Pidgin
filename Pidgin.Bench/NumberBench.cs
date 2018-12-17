using BenchmarkDotNet.Attributes;
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Pidgin.Bench
{
    public class NumberBench
    {
        private static string _input = int.MaxValue.ToString();

        [Benchmark]
        public int Pidgin()
        {
            return Parser.Num.ParseOrThrow(_input);
        }

        [Benchmark(Baseline = true)]
        public int BCL()
        {
            return int.Parse(_input);
        }
    }
}