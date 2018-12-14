using BenchmarkDotNet.Running;

namespace Pidgin.Bench
{
    public class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run<NumberBench>();
            BenchmarkRunner.Run<JsonBench>();
            BenchmarkRunner.Run<ExpressionBench>();
        }
    }
}
