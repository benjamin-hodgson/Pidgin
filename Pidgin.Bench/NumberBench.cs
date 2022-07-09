using System;
using System.Diagnostics.CodeAnalysis;

using BenchmarkDotNet.Attributes;

namespace Pidgin.Bench;

public class NumberBench
{
    private static readonly string _input = int.MaxValue.ToString(null as IFormatProvider);

    [Benchmark]
    [SuppressMessage("performance", "CA1822")]  // Member does not access instance data and can be marked as static
    public int Pidgin()
    {
        return Parser.Num.ParseOrThrow(_input);
    }

    [Benchmark(Baseline = true)]
    [SuppressMessage("performance", "CA1822")]  // Member does not access instance data and can be marked as static
    public int BCL()
    {
        return int.Parse(_input, null);
    }
}
