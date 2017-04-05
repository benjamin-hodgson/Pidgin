using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Pidgin.Bench
{
    public class Config : ManualConfig
    {
        public Config()
        {
            Add(new MemoryDiagnoser());
        }
    }
}