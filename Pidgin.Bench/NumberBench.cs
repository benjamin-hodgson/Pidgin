using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;

namespace Pidgin.Bench;

public class NumberBench
{
    private static readonly string _input = int.MaxValue.ToString(null as IFormatProvider);

    [Benchmark]
    [SuppressMessage("performance", "CA1822:Make member static", Justification = "Must be non-static for BenchmarkDotNet")]
    public int Pidgin()
    {
        return Parser.Num.ParseOrThrow(_input);
    }

    [Benchmark(Baseline = true)]
    [SuppressMessage("performance", "CA1822:Make member static", Justification = "Must be non-static for BenchmarkDotNet")]
    public int BCL()
    {
        return int.Parse(_input, null);
    }
}
