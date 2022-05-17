using BenchmarkDotNet.Attributes;

namespace Pidgin.Bench
{
    public class NumberBench
    {
        private static readonly string _input = int.MaxValue.ToString();

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
