using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

using BenchmarkDotNet.Attributes;

using static Pidgin.Parser;

namespace Pidgin.Bench;

[MemoryDiagnoser]
public class OneOfBench
{
    private static readonly string[] _keywords = Enumerable.Range(0, 32).Select(i => "keyword" + i.ToString("D2", CultureInfo.InvariantCulture)).ToArray();

    // Alternatives built from predicate parsers don't report any expecteds when they fail
    private static readonly Parser<string, Unit> _tokenKeywords =
        OneOf(_keywords.Select(k => Parser<string>.Token(t => t == k))).SkipMany().Then(Parser<string>.End);

    private static readonly Parser<char, Unit> _stringKeywords =
        OneOf(_keywords.Select(k => Try(String(k)))).SkipMany().Then(Parser<char>.End);

    // Every token matches the last alternative, so each OneOf call fails 31 times before succeeding
    private static readonly string[] _tokenInput = Enumerable.Repeat(_keywords[^1], 10000).ToArray();
    private static readonly string _stringInput = string.Concat(Enumerable.Repeat(_keywords[^1], 10000));

    [Benchmark]
    [SuppressMessage("performance", "CA1822:Make member static", Justification = "Must be non-static for BenchmarkDotNet")]
    public void OneOf_TokenPredicates()
    {
        _tokenKeywords.ParseOrThrow(_tokenInput);
    }

    [Benchmark]
    [SuppressMessage("performance", "CA1822:Make member static", Justification = "Must be non-static for BenchmarkDotNet")]
    public void OneOf_Strings()
    {
        _stringKeywords.ParseOrThrow(_stringInput);
    }
}
