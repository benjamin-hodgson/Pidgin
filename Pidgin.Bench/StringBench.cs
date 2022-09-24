using System.Diagnostics.CodeAnalysis;

using BenchmarkDotNet.Attributes;

namespace Pidgin.Bench;

[MemoryDiagnoser]
public class StringBench
{
    private static readonly string _input = new('a', 65536);
    private static readonly Parser<char, string> _string = Parser.String(_input);
    private static readonly Parser<char, string> _cistring = Parser.CIString(_input);

    private static readonly string _whitespace = new(' ', 65536);

    [Benchmark]
    [SuppressMessage("design", "CA1720:Identifier contains type name", Justification = "Doesn't matter for test code")]
    [SuppressMessage("performance", "CA1822:Make member static", Justification = "Must be non-static for BenchmarkDotNet")]
    public void String()
    {
        _string.ParseOrThrow(_input);
    }

    [Benchmark]
    [SuppressMessage("performance", "CA1822:Make member static", Justification = "Must be non-static for BenchmarkDotNet")]
    public void CIString()
    {
        _cistring.ParseOrThrow(_input);
    }

    [Benchmark]
    [SuppressMessage("performance", "CA1822:Make member static", Justification = "Must be non-static for BenchmarkDotNet")]
    public void SkipWhitespaces()
    {
        Parser.SkipWhitespaces.ParseOrThrow(_whitespace);
    }
}
