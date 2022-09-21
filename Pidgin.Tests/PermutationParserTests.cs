using Pidgin.Permutation;

using Xunit;

using static Pidgin.Parser;

namespace Pidgin.Tests;

public class PermutationParserTests : ParserTestBase
{
    [Theory]
    [InlineData("abc")]
    [InlineData("bac")]
    [InlineData("bca")]
    [InlineData("cba")]
    [InlineData("cab")]
    public void TestSimplePermutation(string value)
    {
        var parser = PermutationParser
            .Create<char>()
            .Add(Char('a'))
            .Add(Char('b'))
            .Add(Char('c'))
            .Build()
            .Select(
                tup =>
                {
                    var (((_, a), b), c) = tup;
                    return string.Concat(a, b, c);
                }
            );

        AssertFullParse(parser, value, "abc");
    }

    [Theory]
    [InlineData("abc", "abc")]
    [InlineData("bac", "abc")]
    [InlineData("bca", "abc")]
    [InlineData("cba", "abc")]
    [InlineData("cab", "abc")]
    [InlineData("ac", "a_c")]
    [InlineData("ca", "a_c")]
    public void TestOptionalPermutation(string input, string expected)
    {
        var parser = PermutationParser
            .Create<char>()
            .Add(Char('a'))
            .AddOptional(Char('b'), '_')
            .Add(Char('c'))
            .Build()
            .Select(
                tup =>
                {
                    var (((_, a), b), c) = tup;
                    return string.Concat(a, b, c);
                }
            );

        AssertFullParse(parser, input, expected);
    }
}
