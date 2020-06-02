using BenchmarkDotNet.Running;

namespace Pidgin.Bench
{
    public class Program
    {
        static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
