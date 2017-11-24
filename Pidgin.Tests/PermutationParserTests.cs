using Xunit;
using Pidgin;
using Pidgin.Permutation;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;
using System.Linq;

namespace Pidgin.Tests
{
    public class PermutationParserTests
    {
        [Fact]
        public void TestSimplePermutation()
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

            var results = new[] { "abc", "bac", "bca", "cba" }.Select(x => parser.ParseOrThrow(x));

            Assert.All(results, x => Assert.Equal("abc", x));
        }

        [Fact]
        public void TestOptionalPermutation()
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

            var results1 = new[] { "abc", "bac", "bca", "cba" }.Select(x => parser.ParseOrThrow(x));
            Assert.All(results1, x => Assert.Equal("abc", x));

            var results2 = new[] { "ac", "ca" }.Select(x => parser.ParseOrThrow(x));
            Assert.All(results2, x => Assert.Equal("a_c", x));
        }
    }
}