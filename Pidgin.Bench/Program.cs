using BenchmarkDotNet.Running;

namespace Pidgin.Bench
{
    public class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<JsonBench>();
        }
    }
}
