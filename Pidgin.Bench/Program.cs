using BenchmarkDotNet.Running;

namespace Pidgin.Bench
{
    public class Program
    {
        static void Main()
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAll();
        }
    }
}
